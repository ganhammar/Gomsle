using System.Security.Claims;

namespace Gomsle.Api.Features.LocalApiAuthentication;

public class LocalApiAuthenticationEvents
{
    public Func<ClaimsTransformationContext, Task> OnClaimsTransformation { get; set; } = context => Task.CompletedTask;
    public virtual Task ClaimsTransformation(ClaimsTransformationContext context) => OnClaimsTransformation(context);
}

public class ClaimsTransformationContext
{
    public ClaimsPrincipal? Principal { get; set; }
    public HttpContext? HttpContext { get; internal set; }
}