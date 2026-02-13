using MediatR;
using Microsoft.AspNetCore.Mvc;
using Modules.People.Application.Attributes.Commands;
using Modules.People.Application.People.Commands;
using Modules.People.Application.People.Queries;
using Modules.People.Contracts.Dtos;
using BuildingBlocks.Abstractions.Common;
using Modules.People.Application.Attributes.Queries;
using Microsoft.AspNetCore.Authorization;

namespace Api.Host.Controllers;

[Authorize(Policy = "People.Read")]
[ApiController]
[Route("api/v1/people")]
public sealed class PeopleController : ControllerBase
{
    [Authorize(Policy = "People.Write")]
    [HttpPost]
    public async Task<ActionResult<PersonDto>> Create(
        [FromBody] CreatePersonCommand cmd,
        [FromServices] IMediator mediator)
    {
        var res = await mediator.Send(cmd);
        return res.IsSuccess ? Ok(res.Value) : BadRequest(res.Error);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdatePersonCommand cmd,
        [FromServices] IMediator mediator)
    {
        if (id != cmd.Id) return BadRequest("Id mismatch");

        var res = await mediator.Send(cmd);
        return res.IsSuccess ? NoContent() : NotFound(res.Error);
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> SetStatus(
        Guid id,
        [FromBody] SetPersonStatusCommand cmd,
        [FromServices] IMediator mediator)
    {
        if (id != cmd.Id) return BadRequest("Id mismatch");

        var res = await mediator.Send(cmd);
        return res.IsSuccess ? NoContent() : NotFound(res.Error);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PersonDto>> GetById(Guid id, [FromServices] IMediator mediator)
    {
        var res = await mediator.Send(new GetPersonByIdQuery(id));
        return res.IsSuccess ? Ok(res.Value) : NotFound(res.Error);
    }

    // filtros dinámicos por querystring: attr.diabetic=true
    [HttpGet]
    public async Task<ActionResult<PagedResult<PersonDto>>> Search(
        [FromQuery] string? name,
        [FromQuery] string? identificationNumber,
        [FromQuery] bool? isActive,
        [FromQuery] int? minAge,
        [FromQuery] int? maxAge,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromServices] IMediator mediator = default!)
    {

        // Soportar 2 formatos:
        // 1) attr[disease_text]=hipertension (Swagger-friendly)
        // 2) attr.disease_text=hipertension (legacy)
        // Construimos dinámicos SOLO desde claves attr.* / attr[...]
        // para evitar que page/pageSize/otros query params entren como filtros.
        var dyn = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var kv in Request.Query.Where(k => k.Key.StartsWith("attr.", StringComparison.OrdinalIgnoreCase)))
        {
            var key = kv.Key["attr.".Length..];
            dyn[key] = kv.Value.ToString();
        }

        foreach (var kv in Request.Query.Where(k =>
                     k.Key.StartsWith("attr[", StringComparison.OrdinalIgnoreCase) &&
                     k.Key.EndsWith("]", StringComparison.Ordinal)))
        {
            var key = kv.Key["attr[".Length..^1];
            if (!string.IsNullOrWhiteSpace(key))
                dyn[key] = kv.Value.ToString();
        }

        var req = new SearchPeopleRequest(name, identificationNumber, isActive, minAge, maxAge, dyn, page, pageSize);
        var res = await mediator.Send(new SearchPeopleQuery(req));
        return res.IsSuccess ? Ok(res.Value) : BadRequest(res.Error);
    }

    [HttpPut("{personId:guid}/attributes")]
    public async Task<IActionResult> UpsertAttributes(
        Guid personId,
        [FromBody] List<UpsertAttributeValueDto> values,
        [FromServices] IMediator mediator)
    {
        var res = await mediator.Send(new UpsertPersonAttributesCommand(personId, values));
        return res.IsSuccess ? NoContent() : BadRequest(res.Error);
    }

    [HttpGet("{personId:guid}/attributes")]
    public async Task<ActionResult<IReadOnlyList<PersonAttributeDto>>> GetAttributes(
        Guid personId,
        [FromServices] IMediator mediator)
    {
        var res = await mediator.Send(new GetPersonAttributesQuery(personId));
        return res.IsSuccess ? Ok(res.Value) : NotFound(res.Error);
    }

    [HttpGet("{personId:guid}/attributes/form")]
    public async Task<ActionResult<IReadOnlyList<PersonAttributeFormItemDto>>> GetAttributeForm(
        Guid personId,
        [FromQuery] bool onlyActive = true,
        [FromServices] IMediator mediator = default!)
    {
        var res = await mediator.Send(new GetPersonAttributeFormQuery(personId, onlyActive));
        return res.IsSuccess ? Ok(res.Value) : NotFound(res.Error);
    }

}
