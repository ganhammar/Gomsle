using FluentValidation;
using Gomsle.Api.Features.Account;

namespace Gomsle.Api.Infrastructure.Validators;

public static class CustomValidators
{
    public static IRuleBuilderOptions<T, string?> IsUri<T>(this IRuleBuilder<T, string?> ruleBuilder)
        => ruleBuilder.SetValidator(new IsUriValidator<T>());

    public static IRuleBuilderOptions<T, string?> IsAuthenticated<T>(this IRuleBuilder<T, string?> ruleBuilder, IServiceProvider services)
        => ruleBuilder.SetValidator(new IsAuthenticatedValidator<T>(services));

    public static IRuleBuilderOptions<T, string?> HasRoleForAccount<T>(
            this IRuleBuilder<T, string?> ruleBuilder, IServiceProvider services, params AccountRole[] roles)
        => ruleBuilder.SetAsyncValidator(new HasRoleForAccountValidator<T>(services, roles));
}