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
        var method = context.Request.Method;
        var path = context.Request.Path.Value ?? "/";
        var host = context.Request.Host.Value;
        var scheme = context.Request.Scheme;
        var traceId = context.TraceIdentifier;
        var sw = System.Diagnostics.Stopwatch.StartNew();
        using var scope = _logger.BeginScope(new Dictionary<string, object?>
        {
            ["Component"] = "HTTP",
            ["TraceId"] = traceId,
            ["RequestPath"] = path,
            ["RequestMethod"] = method
        });

        try
        {
            await next(context);
            sw.Stop();

            _logger.Log(
                GetLevel(context.Response.StatusCode),
                "HTTP {Method} {Path} responded {StatusCode} in {Elapsed:0.0000} ms (Host: {RequestHost}, Scheme: {RequestScheme}, TraceId: {TraceId})",
                method,
                path,
                context.Response.StatusCode,
                sw.Elapsed.TotalMilliseconds,
                host,
                scheme,
                traceId);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(
                ex,
                "HTTP {Method} {Path} failed in {Elapsed:0.0000} ms (Host: {RequestHost}, Scheme: {RequestScheme}, TraceId: {TraceId})",
                method,
                path,
                sw.Elapsed.TotalMilliseconds,
                host,
                scheme,
                traceId);
            throw;
        }
    }

    private static LogLevel GetLevel(int statusCode)
    {
        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            return LogLevel.Error;
        }

        if (statusCode >= StatusCodes.Status400BadRequest)
        {
            return LogLevel.Warning;
        }

        return LogLevel.Information;
    }
}
