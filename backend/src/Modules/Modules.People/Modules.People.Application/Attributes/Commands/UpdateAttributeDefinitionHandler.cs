using BuildingBlocks.Abstractions.Common;
using BuildingBlocks.Abstractions.Persistence;
using MediatR;
using Modules.People.Application.Abstractions;

namespace Modules.People.Application.Attributes.Commands;

internal sealed class UpdateAttributeDefinitionHandler : IRequestHandler<UpdateAttributeDefinitionCommand, Result>
{
    private readonly IAttributeDefinitionRepository _defs;
    private readonly IAttributeCatalogCache _cache;
    private readonly IUnitOfWork _uow;

    public UpdateAttributeDefinitionHandler(
        IAttributeDefinitionRepository defs,
        IAttributeCatalogCache cache,
        IUnitOfWork uow)
    {
        _defs = defs;
        _cache = cache;
        _uow = uow;
    }

    public async Task<Result> Handle(UpdateAttributeDefinitionCommand req, CancellationToken ct)
    {
        var def = await _defs.GetByIdAsync(req.Id, ct);
        if (def is null) return Result.Fail("attributes.not_found", "Definición no encontrada");

        def.Update(req.DisplayName, req.IsFilterable, req.ValidationRulesJson, req.IsActive);

        _defs.Update(def);
        await _uow.SaveChangesAsync(ct);
        await _cache.InvalidateAsync(ct);
        return Result.Ok();
    }
}
