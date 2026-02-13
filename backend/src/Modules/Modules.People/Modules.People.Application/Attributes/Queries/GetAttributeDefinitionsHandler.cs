using BuildingBlocks.Abstractions.Common;
using MediatR;
using Modules.People.Application.Abstractions;
using Modules.People.Contracts.Dtos;

namespace Modules.People.Application.Attributes.Queries;

internal sealed class GetAttributeDefinitionsHandler : IRequestHandler<GetAttributeDefinitionsQuery, Result<IReadOnlyList<AttributeDefinitionDto>>>
{
    private readonly IAttributeCatalogCache _cache;

    public GetAttributeDefinitionsHandler(IAttributeCatalogCache cache) => _cache = cache;

    public async Task<Result<IReadOnlyList<AttributeDefinitionDto>>> Handle(GetAttributeDefinitionsQuery req, CancellationToken ct)
    {
        var items = await _cache.GetAsync(req.OnlyActive, ct);
        return Result<IReadOnlyList<AttributeDefinitionDto>>.Ok(items);
    }
}
