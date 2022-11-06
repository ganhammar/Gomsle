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
    private readonly AccountRole[] _roles;

    public HasRoleForAccountValidator(IServiceProvider services, params AccountRole[] roles)
    {
        _services = services;
        _roles = roles;
    }

    public override string Name => nameof(ErrorCodes.MisingRoleForAccount);

    public override async Task<bool> IsValidAsync(
        ValidationContext<T> validationContext,
        string? accountId,
        CancellationToken cancellationToken)
    {
        return await HasRoleForAccount.Validate(_services, accountId, _roles, cancellationToken);
    }

    protected override string GetDefaultMessageTemplate(string errorCode)
        => ErrorCodes.MisingRoleForAccount;
}