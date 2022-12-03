using Gomsle.Api.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gomsle.Api.Features.Application;

[Authorize(Constants.LocalApiPolicy)]
public class ApplicationController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public ApplicationController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateCommand.Command command)
        => Respond(await _mediator.Send(command));

    [HttpPut]
    public async Task<IActionResult> Edit(EditCommand.Command command)
        => Respond(await _mediator.Send(command));

    [HttpDelete]
    public async Task<IActionResult> Delete(DeleteCommand.Command command)
        => Respond(await _mediator.Send(command));

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> DomainRequirements([FromQuery] DomainRequirements.Query query)
        => Respond(await _mediator.Send(query));
}