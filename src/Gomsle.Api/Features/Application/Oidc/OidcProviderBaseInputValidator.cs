using FluentValidation;
using Gomsle.Api.Infrastructure.Validators;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Gomsle.Api.Features.Application.Oidc;

public class OidcProviderBaseInputValidator : AbstractValidator<OidcProviderBaseInput>
{
    public OidcProviderBaseInputValidator(IServiceProvider services)
    {
        RuleFor(x => x.ClientId)
            .NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty();

        RuleFor(x => x.AuthorityUrl)
            .NotEmpty()
            .IsUri();

        RuleFor(x => x.ResponseType)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Must((responseType) => new[]
                {
                    ResponseTypes.Code,
                    ResponseTypes.IdToken,
                    ResponseTypes.None,
                    ResponseTypes.Token
                }.Contains(responseType))
            .WithErrorCode(nameof(ErrorCodes.ResponseTypeIsInvalid))
            .WithMessage(ErrorCodes.ResponseTypeIsInvalid);

        RuleFor(x => x.IsDefault)
            .NotEmpty();

        RuleFor(x => x.IsVisible)
            .NotEmpty();
    }
}