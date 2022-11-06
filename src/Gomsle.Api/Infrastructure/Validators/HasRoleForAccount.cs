using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Gomsle.Api.Features.Account;
using Gomsle.Api.Infrastructure.Extensions;

namespace Gomsle.Api.Infrastructure.Validators;

public static class HasRoleForAccount
{
    public static async Task<bool> Validate(
        IServiceProvider services,
        string? accountId,
        IEnumerable<AccountRole> allowedRoles,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(accountId))
        {
            return false;
        }

        var database = services.GetRequiredService<IAmazonDynamoDB>();
        var httpContextAccessor = services.GetRequiredService<IHttpContextAccessor>();

        var context = new DynamoDBContext(database);
        var account = await context.LoadAsync<AccountModel>(
            accountId, cancellationToken);

        if (account == default)
        {
            return false;
        }

        var userId = httpContextAccessor.HttpContext!.User.GetUserId();
        if (account.Members.TryGetValue(userId!, out var role))
        {
            return allowedRoles.Contains(role);
        }

        return false;
    }
}