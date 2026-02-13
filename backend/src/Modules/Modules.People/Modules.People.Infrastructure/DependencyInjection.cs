using BuildingBlocks.Abstractions.Persistence;
using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Modules.People.Application.Abstractions;
using Modules.People.Infrastructure.Caching;
using Modules.People.Infrastructure.Persistence;
using Modules.People.Infrastructure.Persistence.Repositories;

namespace Modules.People.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddPeopleModule(this IServiceCollection services, IConfiguration cfg)
    {
        services.AddDbContext<PeopleDbContext>(opt =>
        {
            opt.UseSqlServer(cfg.GetConnectionString("PeopleDb"));
            opt.EnableSensitiveDataLogging(false);
            opt.EnableDetailedErrors(false);
        });

        services.AddScoped<IPersonRepository, EfPersonRepository>();
        services.AddScoped<IAttributeDefinitionRepository, EfAttributeDefinitionRepository>();
        services.AddScoped<IPersonAttributeRepository, EfPersonAttributeRepository>();

        // UoW basado en DbContext del módulo
        services.AddScoped<IUnitOfWork>(sp =>
            new EfUnitOfWork<PeopleDbContext>(sp.GetRequiredService<PeopleDbContext>()));

        // Cache (si tenés Redis en Api.Host)
        services.AddScoped<IAttributeCatalogCache, AttributeCatalogCache>();

        return services;
    }
}
