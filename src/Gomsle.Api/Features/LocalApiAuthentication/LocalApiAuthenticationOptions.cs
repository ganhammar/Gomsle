using Gomsle.Api.Infrastructure;
using Microsoft.AspNetCore.Authentication;

namespace Gomsle.Api.Features.LocalApiAuthentication;

public class LocalApiAuthenticationOptions : AuthenticationSchemeOptions
{
    public string ExpectedScope { get; set; } = Constants.LocalApiAuthenticationScheme;
    public bool SaveToken { get; set; } = true;
    public new LocalApiAuthenticationEvents? Events
    {
        get { return (LocalApiAuthenticationEvents)base.Events!; }
        set { base.Events = value; }
    }
}