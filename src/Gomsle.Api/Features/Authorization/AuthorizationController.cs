using Gomsle.Api.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Server.AspNetCore;

namespace Gomsle.Api.Features.Authorization;

public class AuthorizationController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public AuthorizationController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("~/connect/authorize")]
    public async Task<IActionResult> Authorize(AuthorizeRequest.Command command)
    {
        var result = await _mediator.Send(command);

        if (result.IsValid == false)
        {
            return Forbid(new AuthenticationProperties(
                result.Errors.ToDictionary(x => x.ErrorCode, x => x.ErrorMessage)!),
                OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        if (result.Result?.Identity?.IsAuthenticated != true)
        {
            return Challenge();
        }

        return SignIn(
            result.Result!,
            OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }
}