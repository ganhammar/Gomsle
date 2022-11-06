using Amazon.DynamoDBv2.DataModel;

namespace Gomsle.Api.Features.Application;

[DynamoDBTable(ApplicationSetup.ApplicationOriginsTableName)]
public class ApplicationOriginModel
{
    public string? ApplicationId { get; set; }
    public string? Origin { get; set; }
    public bool IsDefault { get; set; }
}