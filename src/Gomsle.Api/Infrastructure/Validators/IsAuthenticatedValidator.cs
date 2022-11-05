using FluentValidation;
using FluentValidation.Validators;

namespace Gomsle.Api.Infrastructure.Validators;

public class IsAuthenticatedValidator<T> : PropertyValidator<T, object?>
{
    private readonly IServiceProvider _services;

    public IsAuthenticatedValidator(IServiceProvider services)
    {
        _services = services;
    }

    public override string Name => nameof(ErrorCodes.NotAuthenticated);

    public override bool IsValid(ValidationContext<T> context, object? value)
    {
        var httpContextAccessor = _services
            .GetRequiredService<IHttpContextAccessor>();
        return httpContextAccessor?.HttpContext
            ?.User?.Identity?.IsAuthenticated == true;
    }

    protected override string GetDefaultMessageTemplate(string errorCode)
        => ErrorCodes.NotAuthenticated;
}