using Amazon.DynamoDBv2.DataModel;

namespace Gomsle.Api.Features.OidcProvider;

[DynamoDBTable(OidcProviderSetup.OidcProviderRequiredDomainsTableName)]
public class OidcProviderRequiredDomainModel
{
    public string? RequiredDomain { get; set; }
    public string? OidcProviderId { get; set; }
}