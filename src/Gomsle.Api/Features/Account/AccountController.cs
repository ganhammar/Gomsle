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
    public async Task<IActionResult> Confirm(ConfirmAccount.Command command)
    {
        var result = await _mediator.Send(command);

        if (result.IsValid) {
            return Redirect(command.ReturnUrl!);
        }

        return Forbid();
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Forgot(ForgotPassword.Command command)
        => Respond(await _mediator.Send(command));

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Reset(ResetPassword.Command command)
    {
        var result = await _mediator.Send(command);

        if (result.IsValid) {
            return Redirect(command.ReturnUrl!);
        }

        return Forbid();
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Login(Login.Command command)
        => Respond(await _mediator.Send(command));
    
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetTwoFactorProviders(GetTwoFactorProviders.Query query)
        => Respond(await _mediator.Send(query));
}