namespace Gomsle.Api.Features.Application.Oidc;

public abstract class OidcProviderBaseInput
{
    public string? Name { get; set; }
    public string? AuthorityUrl { get; set; }
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string? ResponseType { get; set; }
    public bool? IsDefault { get; set; }
    public bool? IsVisible { get; set; }
    public List<string> Scopes { get; set; } = new();
    public List<string> RequiredDomains { get; set; } = new();
}