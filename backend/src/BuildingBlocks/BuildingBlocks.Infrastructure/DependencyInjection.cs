using BuildingBlocks.Abstractions.Contracts;
using BuildingBlocks.Abstractions.Security;
using BuildingBlocks.Infrastructure.Clock;
using BuildingBlocks.Infrastructure.Logging;
using BuildingBlocks.Infrastructure.MediatR;
using BuildingBlocks.Infrastructure.Middleware;
using BuildingBlocks.Infrastructure.Security;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddBuildingBlocks(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();

        services.AddScoped<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddScoped<IUserContext, HttpUserContext>();

        services.AddTransient<ExceptionMiddleware>();
        services.AddTransient<RequestLoggingMiddleware>();

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }
}
