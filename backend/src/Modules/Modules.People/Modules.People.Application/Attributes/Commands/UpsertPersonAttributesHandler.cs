using BuildingBlocks.Abstractions.Common;
using BuildingBlocks.Abstractions.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Modules.People.Application.Abstractions;
using Modules.People.Application.Attributes.Validation;
using Modules.People.Domain.Entities;
using Modules.People.Domain.Enums;

namespace Modules.People.Application.Attributes.Commands;

internal sealed class UpsertPersonAttributesHandler : IRequestHandler<UpsertPersonAttributesCommand, Result>
{
    private readonly IPersonRepository _people;
    private readonly IAttributeDefinitionRepository _defs;
    private readonly IPersonAttributeRepository _vals;
    private readonly IUnitOfWork _uow;

    public UpsertPersonAttributesHandler(
        IPersonRepository people,
        IAttributeDefinitionRepository defs,
        IPersonAttributeRepository vals,
        IUnitOfWork uow)
    {
        _people = people;
        _defs = defs;
        _vals = vals;
        _uow = uow;
    }

    public async Task<Result> Handle(UpsertPersonAttributesCommand req, CancellationToken ct)
    {
        var person = await _people.GetByIdAsync(req.PersonId, ct);
        if (person is null) return Result.Fail(PeopleErrors.NotFound, "Persona no encontrada");

        // Normalize + distinct keys
        var keys = req.Values
            .Select(v => AttributeDefinition.NormalizeKey(v.Key))
            .Distinct()
            .ToArray();

        // Load only active definitions for keys
        var definitions = await _defs.Query()
            .AsNoTracking()
            .Where(d => keys.Contains(d.Key) && d.IsActive)
            .ToListAsync(ct);

        var defMap = definitions.ToDictionary(d => d.Key, d => d);

        foreach (var inputRaw in req.Values)
        {
            var key = AttributeDefinition.NormalizeKey(inputRaw.Key);

            if (!defMap.TryGetValue(key, out var def))
                return Result.Fail(PeopleErrors.AttributeKeyInvalid, $"Atributo desconocido o inactivo: {key}");

            // --- 1) Parse rules json (optional)
            var rules = ValidationRulesParser.TryParse(def.ValidationRulesJson);

            // --- 2) Validate shape (only one value field filled, and matches datatype)
            var shape = AttributeValueShapeValidator.ValidateShape(
                key: def.Key,
                dataType: def.DataType,
                boolValue: inputRaw.BoolValue,
                stringValue: inputRaw.StringValue,
                numberValue: inputRaw.NumberValue,
                dateValue: inputRaw.DateValue);

            if (!shape.ok)
                return Result.Fail("attributes.invalid_shape", shape.error!);

            // --- 3) Validate business/data rules from ValidationRulesJson
            var (ok, error) = AttributeRulesValidator.Validate(
                key: def.Key,
                dataType: def.DataType,
                rules: rules,
                boolValue: inputRaw.BoolValue,
                stringValue: inputRaw.StringValue,
                numberValue: inputRaw.NumberValue,
                dateValue: inputRaw.DateValue);

            if (!ok)
                return Result.Fail("attributes.rule_failed", error!);

            // --- 4) Normalize values (important for Enum equality filters)
            // If DTO is a record => "with" works. If it's a class, just use local normalized variables instead.
            var input = inputRaw;

            if (def.DataType == AttributeDataType.Enum && input.StringValue is not null)
            {
                input = input with { StringValue = input.StringValue.Trim().ToLowerInvariant() };
            }

            // --- 5) Upsert
            var existing = await _vals.GetAsync(req.PersonId, def.Id, ct);
            var entity = existing ?? new PersonAttributeValue(req.PersonId, def.Id);

            switch (def.DataType)
            {
                case AttributeDataType.Boolean:
                    entity.SetBool(input.BoolValue);
                    break;

                case AttributeDataType.String:
                    entity.SetString(input.StringValue);
                    break;

                case AttributeDataType.Enum:
                    entity.SetString(input.StringValue);
                    break;

                case AttributeDataType.Number:
                    entity.SetNumber(input.NumberValue);
                    break;

                case AttributeDataType.Date:
                    entity.SetDate(input.DateValue);
                    break;

                default:
                    return Result.Fail(PeopleErrors.UnsupportedType, $"Tipo no soportado: {key}");
            }

            if (existing is null) await _vals.AddAsync(entity, ct);
            else _vals.Update(entity);
        }

        await _uow.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
