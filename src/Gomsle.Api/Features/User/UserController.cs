using Gomsle.Api.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gomsle.Api.Features.User;

public class UserController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public UserController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterCommand.Command command)
        => Respond(await _mediator.Send(command));

    [HttpGet]
    [AllowAnonymous]
    [Route("[controller]/confirm")]
    public async Task<IActionResult> ConfirmEmail([FromQuery] ConfirmEmailCommand.Command command)
    {
        var result = await _mediator.Send(command);

        if (result.IsValid) {
            return Redirect(command.ReturnUrl!);
        }

        return Forbid();
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Forgot([FromBody] ForgotPasswordCommand.Command command)
        => Respond(await _mediator.Send(command));

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Reset([FromQuery] ResetPasswordCommand.Command command)
    {
        var result = await _mediator.Send(command);

        if (result.IsValid) {
            return Redirect(command.ReturnUrl!);
        }

        return Forbid();
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginCommand.Command command)
        => Respond(await _mediator.Send(command));
    
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetTwoFactorProvidersQuery(GetTwoFactorProvidersQuery.Query query)
        => Respond(await _mediator.Send(query));

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> SendCodeCommand([FromBody] SendCodeCommand.Command command)
        => Respond(await _mediator.Send(command));

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyCodeCommand([FromBody] VerifyCodeCommand.Command command)
        => Respond(await _mediator.Send(command));
}