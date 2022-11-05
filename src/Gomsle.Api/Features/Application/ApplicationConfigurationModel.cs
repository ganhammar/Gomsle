using Amazon.DynamoDBv2.DataModel;

namespace Gomsle.Api.Features.Application;

[DynamoDBTable(ApplicationSetup.ApplicationConfigurationsTableName)]
public class ApplicationConfigurationModel
{
    public string? ApplicationId { get; set; }
    public string? AccountId { get; set; }
    public bool AutoProvision { get; set; }
    public bool EnableProvision { get; set; }
    public string? DefaultOrigin { get; set; }
    public List<string> Origins { get; set; } = new();
    public List<OidcProviderModel> OidcProviders { get; set; } = new();
}