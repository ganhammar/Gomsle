using Amazon.DynamoDBv2.DataModel;

namespace Gomsle.Api.Features.Application;

[DynamoDBTable(ApplicationSetup.ApplicationConfigurationsTableName)]
public class ApplicationConfigurationModel
{
    public string? ApplicationId { get; set; }
    public string? AccountId { get; set; }
    public bool AutoProvision { get; set; }
    public bool EnableProvision { get; set; }
    public List<string> ConnectedOidcProviders { get; set; } = new();
}