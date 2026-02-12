using MediatR;
using Microsoft.AspNetCore.Mvc;
using Modules.People.Application.Attributes.Commands;
using Modules.People.Application.Attributes.Queries;
using Modules.People.Contracts.Dtos;

namespace Api.Host.Controllers;

[ApiController]
[Route("api/v1/attributes")]
public sealed class AttributesController : ControllerBase
{
    [HttpGet("definitions")]
    public async Task<ActionResult<IReadOnlyList<AttributeDefinitionDto>>> GetDefinitions(
        [FromQuery] bool onlyActive = true,
        [FromServices] IMediator mediator = default!)
    {
        var res = await mediator.Send(new GetAttributeDefinitionsQuery(onlyActive));
        return res.IsSuccess ? Ok(res.Value) : BadRequest(res.Error);
    }

    [HttpPost("definitions")]
    public async Task<ActionResult<AttributeDefinitionDto>> CreateDefinition(
        [FromBody] CreateAttributeDefinitionCommand cmd,
        [FromServices] IMediator mediator)
    {
        var res = await mediator.Send(cmd);
        return res.IsSuccess ? Ok(res.Value) : BadRequest(res.Error);
    }

    [HttpPut("definitions/{id:guid}")]
    public async Task<IActionResult> UpdateDefinition(
        Guid id,
        [FromBody] UpdateAttributeDefinitionCommand cmd,
        [FromServices] IMediator mediator)
    {
        if (id != cmd.Id) return BadRequest("Id mismatch");
        var res = await mediator.Send(cmd);
        return res.IsSuccess ? NoContent() : NotFound(res.Error);
    }
}
