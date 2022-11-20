using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Gomsle.Api.Infrastructure.Extensions;

namespace Gomsle.Api.Features.Account;

public static class AccountHelpers
{
    public static async Task<AccountModel?> FindByName(
        string name,
        DynamoDBContext context,
        CancellationToken cancellationToken = default)
    {
        var search = context.FromQueryAsync<AccountModel>(new QueryOperationConfig
        {
            IndexName = "NormalizedName-index",
            KeyExpression = new Expression
            {
                ExpressionStatement = "NormalizedName = :normalizedName",
                ExpressionAttributeValues = new Dictionary<string, DynamoDBEntry>
                {
                    { ":normalizedName", name!.UrlFriendly() },
                }
            },
            Limit = 1,
        });
        var accounts = await search.GetRemainingAsync(cancellationToken);

        return accounts.FirstOrDefault();
    }
}