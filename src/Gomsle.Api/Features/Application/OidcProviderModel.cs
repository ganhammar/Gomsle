namespace Gomsle.Api.Features.Application;

public class OidcProviderModel
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string? Name { get; set; }
    public string? AuthorityUrl { get; set; }
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string? ResponseType { get; set; }
    public bool IsDefault { get; set; }
    public bool IsVisible { get; set; }
    public List<string> Scopes { get; set; } = new();
    public List<string> RequiredDomains { get; set; } = new();
}