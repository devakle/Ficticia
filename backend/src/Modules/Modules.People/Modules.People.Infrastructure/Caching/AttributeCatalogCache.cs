using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Modules.People.Application.Abstractions;
using Modules.People.Contracts.Dtos;
using Modules.People.Infrastructure.Persistence;
using System.Text.Json;

namespace Modules.People.Infrastructure.Caching;

public sealed class AttributeCatalogCache : IAttributeCatalogCache
{
    private const string CacheKey = "ficticia:people:attribute-definitions:v1";

    private readonly IDistributedCache? _cache;
    private readonly PeopleDbContext _db;

    public AttributeCatalogCache(PeopleDbContext db, IDistributedCache? cache = null)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<IReadOnlyList<AttributeDefinitionDto>> GetAsync(bool onlyActive, CancellationToken ct)
    {
        var key = $"{CacheKey}:{onlyActive}";
        if (_cache is not null)
        {
            try
            {
                var cached = await _cache.GetStringAsync(key, ct);
                if (cached is not null)
                    return JsonSerializer.Deserialize<List<AttributeDefinitionDto>>(cached)!;
            }
            catch (Exception)
            {
                // If distributed cache is down/unreachable, fallback to DB instead of failing request.
            }
        }

        var q = _db.AttributeDefinitions.AsNoTracking();
        if (onlyActive) q = q.Where(x => x.IsActive);

        var items = await q.OrderBy(x => x.Key)
            .Select(x => new AttributeDefinitionDto(
                x.Id, x.Key, x.DisplayName, (int)x.DataType, x.IsFilterable, x.IsActive, x.ValidationRulesJson
            ))
            .ToListAsync(ct);

        if (_cache is not null)
        {
            try
            {
                await _cache.SetStringAsync(
                    key,
                    JsonSerializer.Serialize(items),
                    new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30) },
                    ct
                );
            }
            catch (Exception)
            {
                // Ignore cache write failures; source of truth is DB.
            }
        }

        return items;
    }

    public Task InvalidateAsync(CancellationToken ct)
    {
        if (_cache is null) return Task.CompletedTask;
        return InvalidateSafeAsync(ct);
    }

    private async Task InvalidateSafeAsync(CancellationToken ct)
    {
        try
        {
            await Task.WhenAll(
                _cache!.RemoveAsync($"{CacheKey}:True", ct),
                _cache.RemoveAsync($"{CacheKey}:False", ct));
        }
        catch (Exception)
        {
            // Ignore cache invalidation failures; source of truth is DB.
        }
    }
}
