using Amazon.DynamoDBv2.DataModel;

namespace Gomsle.Api.Features.Account;

[DynamoDBTable(AccountSetup.AccountInvitationsTableName)]
public class AccountInvitationModel
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string? AccountName { get; set; }
    public string? Email { get; set; }
    public AccountRole Role { get; set; }
}