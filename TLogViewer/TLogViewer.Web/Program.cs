using Microsoft.AspNetCore.Http.Features;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSingleton<tLogViewer.Services.ILogAnalyticsService, tLogViewer.Services.LogAnalyticsService>();
builder.Services.AddSingleton<tLogViewer.Services.ITlogProcessingService, tLogViewer.Services.TlogProcessingService>();
builder.Services.AddSingleton<tLogViewer.Services.ITlogSessionStore, tLogViewer.Services.TlogSessionStore>();
builder.Services.AddHostedService<TLogViewer.Web.TlogSessionCleanupService>();
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 512_000_000;
});
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 512_000_000;
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularDev", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();
app.UseDefaultFiles();
app.UseStaticFiles();

if (app.Environment.IsDevelopment())
{
    app.UseCors("AllowAngularDev");
}

//app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

// Unknown /api routes must return JSON 404 (do not fall through to the SPA).
app.Map("/api/{**slug}", (HttpContext context) =>
    Results.Json(
        new { message = $"No API endpoint matches '{context.Request.Path}'." },
        statusCode: StatusCodes.Status404NotFound));

app.MapFallbackToFile("index.html");

app.Run();
