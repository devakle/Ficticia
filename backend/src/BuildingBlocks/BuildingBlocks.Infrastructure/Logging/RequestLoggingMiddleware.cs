using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;


namespace BuildingBlocks.Infrastructure.Logging;

public sealed class RequestLoggingMiddleware : IMiddleware
{
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(ILogger<RequestLoggingMiddleware> logger)
    {
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        await next(context);
        sw.Stop();

        _logger.LogInformation(
            "HTTP {Method} {Path} responded {StatusCode} in {Elapsed} ms",
            context.Request.Method,
            context.Request.Path,
            context.Response.StatusCode,
            sw.ElapsedMilliseconds
        );
    }
}
