using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Modules.AI.Application.Abstractions;
using Modules.AI.Infrastructure.OpenAI;
using System.Net.Http.Headers;
using Modules.AI.Infrastructure.Catalog;

namespace Modules.AI.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAiModule(this IServiceCollection services, IConfiguration cfg)
    {
        services.Configure<OpenAiOptions>(cfg.GetSection("OpenAI"));

        // OpenAI HttpClient
        services.AddHttpClient<OpenAiClient>((sp, http) =>
        {
            var opt = sp.GetRequiredService<IOptions<OpenAiOptions>>().Value;

            http.BaseAddress = new Uri(opt.BaseUrl.TrimEnd('/') + "/");
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", opt.ApiKey);
            http.Timeout = TimeSpan.FromSeconds(20);
        });

        services.AddScoped<IAttributeCatalogProvider, PeopleAttributeCatalogProvider>();

        services.AddScoped<IConditionNormalizer, OpenAiConditionNormalizer>();

        return services;
    }
}
