using Gomsle.Api.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gomsle.Api.Features.OidcProvider;

[Authorize(Constants.LocalApiPolicy)]
public class OidcProviderController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public OidcProviderController(IMediator mediator)
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
}