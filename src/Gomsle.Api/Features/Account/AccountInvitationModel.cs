using Amazon.DynamoDBv2.DataModel;

namespace Gomsle.Api.Features.Account;

[DynamoDBTable(AccountSetup.AccountInvitationsTableName)]
public class AccountInvitationModel
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string? NormalizedAccountName { get; set; }
    public string? Email { get; set; }
    public AccountRole Role { get; set; }
    public string? SuccessUrl { get; set; }
}