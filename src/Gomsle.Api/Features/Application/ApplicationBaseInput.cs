namespace Gomsle.Api.Features.Application;

public abstract class ApplicationBaseInput
{
    public string? DisplayName { get; set; }
    public bool? AutoProvision { get; set; }
    public bool? EnableProvision { get; set; }
    public List<string> RedirectUris { get; set; } = new();
    public List<string> PostLogoutRedirectUris { get; set; } = new();
    public string? DefaultOrigin { get; set; }
    public List<string> Origins { get; set; } = new();
    public List<string> ConnectedOidcProviders { get; set; } = new();
}