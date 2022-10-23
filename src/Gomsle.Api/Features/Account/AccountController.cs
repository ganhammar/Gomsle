using Gomsle.Api.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gomsle.Api.Features.Account;

public class AccountController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public AccountController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateCommand.Command command)
        => Respond(await _mediator.Send(command));
}