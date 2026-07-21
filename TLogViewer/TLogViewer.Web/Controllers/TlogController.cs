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

    public TlogController(ITlogProcessingService processingService, ITlogSessionStore sessionStore)
    {
        _processingService = processingService;
        _sessionStore = sessionStore;
    }

    /// <summary>
    /// Parses the uploaded TLog in memory and returns flight summaries only.
    /// Fetch each flight via GET sessions/{sessionId}/flights/{flightId}.
    /// </summary>
    [HttpPost("upload")]
    [RequestSizeLimit(512_000_000)]
    [RequestFormLimits(MultipartBodyLengthLimit = 512_000_000)]
    public ActionResult<TlogUploadResponse> Upload(IFormFile file)
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
        try
        {
            using var stream = file.OpenReadStream();
            processResult = _processingService.Process(stream);
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
            Flights = snapshot.Flights
        });
    }

    /// <summary>
    /// Downloads one flight's messages. Session memory is released after all flights
    /// are downloaded, or automatically after 30 minutes.
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
}
