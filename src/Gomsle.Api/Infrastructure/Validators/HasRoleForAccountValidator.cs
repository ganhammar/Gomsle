using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using FluentValidation;
using FluentValidation.Validators;
using Gomsle.Api.Features.Account;
using Gomsle.Api.Infrastructure.Extensions;

namespace Gomsle.Api.Infrastructure.Validators;

public class HasRoleForAccountValidator<T> : AsyncPropertyValidator<T, string?>
{
    private readonly IServiceProvider _services;

    public HasRoleForAccountValidator(IServiceProvider services, params AccountRole[] roles)
    {
        _services = services;
    }

    public override string Name => nameof(ErrorCodes.MisingRoleForAccount);

    public override async Task<bool> IsValidAsync(
        ValidationContext<T> validationContext,
        string? accountId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(accountId))
        {
            return false;
        }

        var database = _services.GetRequiredService<IAmazonDynamoDB>();
        var httpContextAccessor = _services.GetRequiredService<IHttpContextAccessor>();

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
            return new[] { AccountRole.Administrator, AccountRole.Owner }.Contains(role);
        }

        return false;
    }

    protected override string GetDefaultMessageTemplate(string errorCode)
        => ErrorCodes.MisingRoleForAccount;
}