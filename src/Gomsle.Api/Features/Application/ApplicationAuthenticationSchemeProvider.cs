using Gomsle.Api.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Options;

namespace Gomsle.Api.Features.Application;

public class ApplicationAuthenticationSchemeProvider : AuthenticationSchemeProvider
{
    private readonly Type _oidcHandlerType;

    public ApplicationAuthenticationSchemeProvider(
            IOptions<AuthenticationOptions> options)
        : base(options)
    {
        this._oidcHandlerType = typeof(OpenIdConnectHandler);
    }

    public override async Task<IEnumerable<AuthenticationScheme>> GetRequestHandlerSchemesAsync()
    {
        return await GetAllSchemesAsync();
    }

    public override async Task<AuthenticationScheme?> GetSchemeAsync(string name)
    {
        return await base.GetSchemeAsync(name);
    }

    public override async Task<IEnumerable<AuthenticationScheme>> GetAllSchemesAsync()
    {
        var allSchemes = new List<AuthenticationScheme>();
        var localSchemes = await base.GetAllSchemesAsync();

        if (localSchemes != null)
        {
            allSchemes.AddRange(localSchemes.Except(
                localSchemes.Where(x => x.Name == Constants.FakeOidcHandler)));
        }

        return allSchemes;
    }
}