using Gomsle.Api.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Authorization;
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
    [AllowAnonymous]
    public async Task<IActionResult> Register(RegisterAccount.Command command)
        => Respond(await _mediator.Send(command));

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Confirm(string userId, string token, string returnUrl)
    {
        var result = await _mediator.Send(new ConfirmAccount.Command
        {
            UserId = userId,
            Token = token,
        });

        if (result.IsValid) {
            return Redirect(returnUrl);
        }

        return Forbid();
    }
}