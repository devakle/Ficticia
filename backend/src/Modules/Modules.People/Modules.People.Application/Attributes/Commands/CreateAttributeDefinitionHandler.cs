using BuildingBlocks.Abstractions.Common;
using BuildingBlocks.Abstractions.Persistence;
using MediatR;
using Modules.People.Application.Abstractions;
using Modules.People.Contracts.Dtos;
using Modules.People.Domain.Entities;
using Modules.People.Domain.Enums;

namespace Modules.People.Application.Attributes.Commands;

internal sealed class CreateAttributeDefinitionHandler : IRequestHandler<CreateAttributeDefinitionCommand, Result<AttributeDefinitionDto>>
{
    private readonly IAttributeDefinitionRepository _defs;
    private readonly IAttributeCatalogCache _cache;
    private readonly IUnitOfWork _uow;

    public CreateAttributeDefinitionHandler(
        IAttributeDefinitionRepository defs,
        IAttributeCatalogCache cache,
        IUnitOfWork uow)
    {
        _defs = defs;
        _cache = cache;
        _uow = uow;
    }

    public async Task<Result<AttributeDefinitionDto>> Handle(CreateAttributeDefinitionCommand req, CancellationToken ct)
    {
        var key = AttributeDefinition.NormalizeKey(req.Key);

        var existing = await _defs.GetByKeyAsync(key, ct);
        if (existing is not null)
            return Result<AttributeDefinitionDto>.Fail("attributes.duplicate", $"Ya existe el atributo '{key}'");

        var def = new AttributeDefinition(
            key,
            req.DisplayName,
            (AttributeDataType)req.DataType,
            req.IsFilterable,
            req.ValidationRulesJson
        );

        await _defs.AddAsync(def, ct);
        await _uow.SaveChangesAsync(ct);
        await _cache.InvalidateAsync(ct);

        return Result<AttributeDefinitionDto>.Ok(new AttributeDefinitionDto(
            def.Id, def.Key, def.DisplayName, (int)def.DataType, def.IsFilterable, def.IsActive, def.ValidationRulesJson
        ));
    }
}
