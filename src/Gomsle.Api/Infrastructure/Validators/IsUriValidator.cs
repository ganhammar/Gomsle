using FluentValidation;
using FluentValidation.Validators;

namespace Gomsle.Api.Infrastructure.Validators;

public class IsUriValidator<T> : PropertyValidator<T, string?>
{
    public IsUriValidator()
    {
    }

    public override string Name => nameof(ErrorCodes.InvalidUri);

    public override bool IsValid(ValidationContext<T> context, string? value)
        => value != default && Uri.TryCreate(value, UriKind.Absolute, out _);

    protected override string GetDefaultMessageTemplate(string errorCode)
        => ErrorCodes.InvalidUri;
}