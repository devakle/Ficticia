using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Infrastructure.Middleware;

public sealed class ExceptionMiddleware : IMiddleware
{
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(ILogger<ExceptionMiddleware> logger)
    {
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var method = context.Request.Method;
        var path = context.Request.Path.Value ?? "/";
        var traceId = context.TraceIdentifier;

        try
        {
            await next(context);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(
                ex,
                "Validation error for HTTP {Method} {Path}. TraceId: {TraceId}",
                method,
                path,
                traceId);

            context.Response.StatusCode = 400;
            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Title = "Validation error",
                Status = 400,
                Detail = string.Join("; ", ex.Errors.Select(e => e.ErrorMessage))
            });
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            _logger.LogWarning(
                ex,
                "Database unique constraint violation for HTTP {Method} {Path}. TraceId: {TraceId}",
                method,
                path,
                traceId);

            context.Response.StatusCode = 409;
            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Title = "Conflict",
                Status = 409,
                Detail = "A record with the same unique value already exists."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unhandled exception for HTTP {Method} {Path}. TraceId: {TraceId}",
                method,
                path,
                traceId);

            context.Response.StatusCode = 500;
            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Title = "Internal server error",
                Status = 500
            });
        }
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        var message = ex.InnerException?.Message ?? ex.Message;
        return message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase)
               || message.Contains("duplicate", StringComparison.OrdinalIgnoreCase)
               || message.Contains("2627", StringComparison.OrdinalIgnoreCase)
               || message.Contains("2601", StringComparison.OrdinalIgnoreCase);
    }
}
