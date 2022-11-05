using Amazon.DynamoDBv2.DataModel;

namespace Gomsle.Api.Features.Account;

[DynamoDBTable(AccountSetup.AccountsTableName)]
public class AccountModel
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string? NormalizedName { get; set; }
    public string? Name { get; set; }
    public Dictionary<string, AccountRole> Members { get; set; }
        = new Dictionary<string, AccountRole>();
}