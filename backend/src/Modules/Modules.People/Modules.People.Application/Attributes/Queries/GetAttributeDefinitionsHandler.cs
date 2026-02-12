using BuildingBlocks.Abstractions.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Modules.People.Application.Abstractions;
using Modules.People.Contracts.Dtos;

namespace Modules.People.Application.Attributes.Queries;

internal sealed class GetAttributeDefinitionsHandler : IRequestHandler<GetAttributeDefinitionsQuery, Result<IReadOnlyList<AttributeDefinitionDto>>>
{
    private readonly IAttributeDefinitionRepository _defs;

    public GetAttributeDefinitionsHandler(IAttributeDefinitionRepository defs) => _defs = defs;

    public async Task<Result<IReadOnlyList<AttributeDefinitionDto>>> Handle(GetAttributeDefinitionsQuery req, CancellationToken ct)
    {
        var q = _defs.Query().AsNoTracking();

        if (req.OnlyActive) q = q.Where(x => x.IsActive);

        var items = await q
            .OrderBy(x => x.Key)
            .Select(x => new AttributeDefinitionDto(x.Id, x.Key, x.DisplayName, (int)x.DataType, x.IsFilterable, x.IsActive, x.ValidationRulesJson))
            .ToListAsync(ct);

        return Result<IReadOnlyList<AttributeDefinitionDto>>.Ok(items);
    }
}
