using MediatR;
using Microsoft.AspNetCore.Mvc;
using Modules.People.Application.Attributes.Commands;
using Modules.People.Application.People.Commands;
using Modules.People.Application.People.Queries;
using Modules.People.Contracts.Dtos;
using BuildingBlocks.Abstractions.Common;

namespace Api.Host.Controllers;

[ApiController]
[Route("api/v1/people")]
public sealed class PeopleController : ControllerBase
{
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
        var dyn = Request.Query
            .Where(kv => kv.Key.StartsWith("attr.", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(kv => kv.Key["attr.".Length..], kv => kv.Value.ToString());

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
}
