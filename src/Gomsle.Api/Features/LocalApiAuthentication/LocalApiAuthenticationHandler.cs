using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using OpenIddict.Validation;

namespace Gomsle.Api.Features.LocalApiAuthentication;

public class LocalApiAuthenticationHandler : AuthenticationHandler<LocalApiAuthenticationOptions>
{
    private readonly OpenIddictValidationService _tokenValidator;

    public LocalApiAuthenticationHandler(
            IOptionsMonitor<LocalApiAuthenticationOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock,
            OpenIddictValidationService tokenValidator)
        : base(options, logger, encoder, clock)
    {
        _tokenValidator = tokenValidator;
    }

    protected new LocalApiAuthenticationEvents Events
    {
        get => (LocalApiAuthenticationEvents)base.Events!;
        set => base.Events = value;
    }

    protected override Task<object> CreateEventsAsync()
        => Task.FromResult<object>(new LocalApiAuthenticationEvents());

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        string? token = null;
        string authorization = Request.Headers["Authorization"];

        if (string.IsNullOrEmpty(authorization))
        {
            return AuthenticateResult.NoResult();
        }

        if (authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            token = authorization.Substring("Bearer ".Length).Trim();
        }

        if (string.IsNullOrEmpty(token))
        {
            return AuthenticateResult.Fail("No Access Token is sent");
        }

        ClaimsPrincipal claimsPrincipal;
        try
        {
            claimsPrincipal = await _tokenValidator.ValidateAccessTokenAsync(token);
        }
        catch (Exception exception)
        {
            return AuthenticateResult.Fail(exception.Message);
        }

        if (claimsPrincipal.HasClaim(OpenIddictConstants.Claims.Scope, Options.ExpectedScope) == false)
        {
            return AuthenticateResult.Fail($"Missing scope '{Options.ExpectedScope}");
        }

        var authenticationProperties = new AuthenticationProperties();
        if (Options.SaveToken)
        {
            authenticationProperties.StoreTokens(new[]
            {
                    new AuthenticationToken { Name = "access_token", Value = token }
                });
        }

        var claimsTransformationContext = new ClaimsTransformationContext
        {
            Principal = claimsPrincipal,
            HttpContext = Context
        };

        await Events.ClaimsTransformation(claimsTransformationContext);

        AuthenticationTicket authenticationTicket = new AuthenticationTicket(
            claimsTransformationContext.Principal, authenticationProperties, Scheme.Name);
        return AuthenticateResult.Success(authenticationTicket);
    }
}