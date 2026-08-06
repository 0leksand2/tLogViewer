using Microsoft.AspNetCore.Mvc;
using tLogViewer.Core.Models;
using tLogViewer.Services.Interfaces;

namespace TLogViewer.Web.Controllers;

[ApiController]
[Route("api/tlog")]
public class TlogController : ControllerBase
{
    private readonly ITlogProcessingService _processingService;
    private readonly ITlogSessionStore _sessionStore;
    private readonly IFlightAnalysisCache _analysisCache;

    public TlogController(
        ITlogProcessingService processingService,
        ITlogSessionStore sessionStore,
        IFlightAnalysisCache analysisCache)
    {
        _processingService = processingService;
        _sessionStore = sessionStore;
        _analysisCache = analysisCache;
    }

    /// <summary>
    /// Parses the uploaded TLog (or reuses a 1-hour analysis cache hit) and returns flight summaries.
    /// Cache identity = file name + vehicle system id + size (+ split flag).
    /// Fetch each flight via GET sessions/{sessionId}/flights/{flightId}.
    /// </summary>
    [HttpPost("upload")]
    [RequestSizeLimit(512_000_000)]
    [RequestFormLimits(MultipartBodyLengthLimit = 512_000_000)]
    public ActionResult<TlogUploadResponse> Upload(
        IFormFile file,
        [FromForm] bool splitIntoFlights = true)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { message = "No file was uploaded." });
        }

        var extension = Path.GetExtension(file.FileName);
        if (!string.Equals(extension, ".tlog", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "Only .tlog files are accepted." });
        }

        TlogParseResult processResult;
        var fromCache = false;
        string cacheKey;

        try
        {
            using var uploadStream = file.OpenReadStream();
            using var buffer = new MemoryStream();
            uploadStream.CopyTo(buffer);
            buffer.Position = 0;

            var peekedSysId = _processingService.PeekSystemId(buffer);
            buffer.Position = 0;

            cacheKey = _analysisCache.BuildKey(
                file.FileName,
                peekedSysId,
                file.Length,
                splitIntoFlights);

            if (_analysisCache.TryGet(cacheKey, out var cached) && cached is not null)
            {
                processResult = cached;
                fromCache = true;
            }
            else
            {
                processResult = _processingService.Process(buffer, splitIntoFlights);

                var resolvedSysId = processResult.SystemId != 0
                    ? processResult.SystemId
                    : peekedSysId;

                if (resolvedSysId != peekedSysId)
                {
                    var resolvedKey = _analysisCache.BuildKey(
                        file.FileName,
                        resolvedSysId,
                        file.Length,
                        splitIntoFlights);

                    if (_analysisCache.TryGet(resolvedKey, out cached) && cached is not null)
                    {
                        processResult = cached;
                        fromCache = true;
                        cacheKey = resolvedKey;
                    }
                    else
                    {
                        cacheKey = resolvedKey;
                        if (processResult.SystemId == 0 && resolvedSysId != 0)
                        {
                            processResult = CloneWithSystemId(processResult, resolvedSysId);
                        }

                        _analysisCache.Set(cacheKey, processResult);
                    }
                }
                else
                {
                    if (processResult.SystemId == 0 && peekedSysId != 0)
                    {
                        processResult = CloneWithSystemId(processResult, peekedSysId);
                    }

                    _analysisCache.Set(cacheKey, processResult);
                }
            }
        }
        catch (InvalidDataException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception)
        {
            return BadRequest(new { message = "Failed to parse the TLog file." });
        }

        var sessionId = _sessionStore.Store(file.FileName, file.Length, processResult);
        var snapshot = _sessionStore.GetSnapshot(sessionId)!;

        return Ok(new TlogUploadResponse
        {
            SessionId = sessionId,
            FileName = snapshot.FileName,
            Size = snapshot.Size,
            TotalRecords = snapshot.TotalRecords,
            ParsedCount = snapshot.ParsedCount,
            FlightCount = snapshot.Flights.Count,
            Flights = snapshot.Flights,
            SystemId = processResult.SystemId,
            FromCache = fromCache,
            CacheKey = cacheKey
        });
    }

    /// <summary>
    /// Downloads one flight's messages. Session and analysis cache entries expire after 1 hour.
    /// </summary>
    [HttpGet("sessions/{sessionId}/flights/{flightId:guid}")]
    public ActionResult<TlogFlightResponse> GetFlight(string sessionId, Guid flightId)
    {
        if (!_sessionStore.TryTakeFlight(sessionId, flightId, out var flight, out var sessionReleased)
            || flight is null)
        {
            return NotFound(new { message = $"Flight '{flightId}' was not found for session '{sessionId}'." });
        }

        return Ok(new TlogFlightResponse
        {
            SessionId = sessionId,
            Flight = flight,
            SessionReleased = sessionReleased
        });
    }

    private static TlogParseResult CloneWithSystemId(TlogParseResult source, byte systemId) =>
        new()
        {
            TotalRecords = source.TotalRecords,
            ParsedCount = source.ParsedCount,
            SystemId = systemId,
            Flights = source.Flights
        };
}
