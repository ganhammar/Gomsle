using Amazon.DynamoDBv2.DataModel;

namespace Gomsle.Api.Features.Account;

[DynamoDBTable(AccountSetup.TableName)]
public class AccountModel
{
    public string? NormalizedName { get; set; }
    public string? Name { get; set; }
}