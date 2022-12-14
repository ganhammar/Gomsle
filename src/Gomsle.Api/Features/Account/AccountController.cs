using Gomsle.Api.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gomsle.Api.Features.Account;

[Authorize(Constants.LocalApiPolicy)]
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

    [HttpPost]
    public async Task<IActionResult> Invite(InviteCommand.Command command)
        => Respond(await _mediator.Send(command));

    [HttpPost, HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> AcceptInvitation(AcceptInvitationCommand.Command command)
        => Respond(await _mediator.Send(command));
}