using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Modules.AI.Application.Conditions.Commands;
using Modules.AI.Contracts.Dtos;

namespace Api.Host.Controllers;

[ApiController]
[Route("api/v1/ai")]
public sealed class AiController : ControllerBase
{
    // Podés protegerlo con una policy tipo People.Read (o una nueva AI.Use)
    [Authorize(Policy = "People.Read")]
    [HttpPost("conditions/normalize")]
    public async Task<ActionResult<NormalizeConditionResponseDto>> NormalizeCondition(
        [FromBody] NormalizeConditionRequestDto req,
        [FromServices] IMediator mediator)
    {
        var res = await mediator.Send(new NormalizeConditionCommand(req.Text));
        return res.IsSuccess ? Ok(res.Value) : BadRequest(res.Error);
    }
}
