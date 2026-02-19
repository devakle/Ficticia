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

    private readonly IDistributedCache? _primaryCache;
    private readonly IDistributedCache? _secondaryCache;
    private readonly PeopleDbContext _db;

    public AttributeCatalogCache(PeopleDbContext db, IEnumerable<IDistributedCache>? caches = null)
    {
        _db = db;
        var allCaches = (caches ?? Array.Empty<IDistributedCache>()).Distinct().ToList();

        // En Development puede haber Redis + Memory; preferimos Redis como primary.
        _primaryCache = allCaches.FirstOrDefault(c => c.GetType().Name.Contains("Redis", StringComparison.OrdinalIgnoreCase))
                     ?? allCaches.FirstOrDefault();

        _secondaryCache = allCaches
            .FirstOrDefault(c => !ReferenceEquals(c, _primaryCache));
    }

    public async Task<IReadOnlyList<AttributeDefinitionDto>> GetAsync(bool onlyActive, CancellationToken ct)
    {
        var key = $"{CacheKey}:{onlyActive}";
        var cached = await TryGetCacheAsync(_primaryCache, key, ct)
                  ?? await TryGetCacheAsync(_secondaryCache, key, ct);
        if (cached is not null)
            return cached;

        var q = _db.AttributeDefinitions.AsNoTracking();
        if (onlyActive) q = q.Where(x => x.IsActive);

        var items = await q.OrderBy(x => x.Key)
            .Select(x => new AttributeDefinitionDto(
                x.Id, x.Key, x.DisplayName, (int)x.DataType, x.IsFilterable, x.IsActive, x.ValidationRulesJson
            ))
            .ToListAsync(ct);

        await TrySetCacheAsync(_primaryCache, key, items, ct);
        await TrySetCacheAsync(_secondaryCache, key, items, ct);

        return items;
    }

    public Task InvalidateAsync(CancellationToken ct)
    {
        if (_primaryCache is null && _secondaryCache is null) return Task.CompletedTask;
        return InvalidateSafeAsync(ct);
    }

    private async Task InvalidateSafeAsync(CancellationToken ct)
    {
        await TryRemoveCacheAsync(_primaryCache, ct);
        await TryRemoveCacheAsync(_secondaryCache, ct);
    }

    private static async Task<IReadOnlyList<AttributeDefinitionDto>?> TryGetCacheAsync(
        IDistributedCache? cache,
        string key,
        CancellationToken ct)
    {
        if (cache is null) return null;

        try
        {
            var cached = await cache.GetStringAsync(key, ct);
            if (cached is not null)
            {
                return JsonSerializer.Deserialize<List<AttributeDefinitionDto>>(cached)!;
            }
        }
        catch (Exception)
        {
            // If cache is down/unreachable, caller will try fallback cache or DB.
        }

        return null;
    }

    private static async Task TrySetCacheAsync(
        IDistributedCache? cache,
        string key,
        IReadOnlyList<AttributeDefinitionDto> items,
        CancellationToken ct)
    {
        if (cache is null) return;

        try
        {
            await cache.SetStringAsync(
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

    private static async Task TryRemoveCacheAsync(IDistributedCache? cache, CancellationToken ct)
    {
        if (cache is null) return;

        try
        {
            await Task.WhenAll(
                cache.RemoveAsync($"{CacheKey}:True", ct),
                cache.RemoveAsync($"{CacheKey}:False", ct));
        }
        catch (Exception)
        {
            // Ignore cache invalidation failures; source of truth is DB.
        }
    }
}
