using Microsoft.Extensions.Logging;
using MediatR;

namespace BuildingBlocks.Infrastructure.MediatR;

public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var responseName = typeof(TResponse).Name;
        var sw = System.Diagnostics.Stopwatch.StartNew();

        using var scope = _logger.BeginScope(new Dictionary<string, object?>
        {
            ["Component"] = "CQRS",
            ["CqrsRequest"] = requestName,
            ["CqrsResponse"] = responseName
        });

        _logger.LogDebug(
            "CQRS START {RequestName} -> {ResponseName} | Payload: {@Request}",
            requestName,
            responseName,
            request);

        try
        {
            var response = await next();
            sw.Stop();

            _logger.LogInformation(
                "CQRS OK {RequestName} in {ElapsedMs:0.0000} ms | Response: {@Response}",
                requestName,
                sw.Elapsed.TotalMilliseconds,
                response);

            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(
                ex,
                "CQRS FAIL {RequestName} after {ElapsedMs:0.0000} ms | Payload: {@Request}",
                requestName,
                sw.Elapsed.TotalMilliseconds,
                request);
            throw;
        }
    }
}
