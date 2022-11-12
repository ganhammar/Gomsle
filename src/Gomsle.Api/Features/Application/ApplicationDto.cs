using Gomsle.Api.Features.Application.Oidc;

namespace Gomsle.Api.Features.Application;

public class ApplicationDto
{
    public string? Id { get; set; }
    public string? ClientId { get; set; }
    public string? DisplayName { get; set; }
    public List<string>? PostLogoutRedirectUris { get; set; }
    public List<string>? RedirectUris { get; set; }
    public string? AccountId { get; set; }
    public bool AutoProvision { get; set; }
    public bool EnableProvision { get; set; }
    public string? DefaultOrigin { get; set; }
    public List<string> Origins { get; set; } = new();
}