using FluentValidation;
using Gomsle.Api.Infrastructure.Validators;

namespace Gomsle.Api.Features.Application;

public class ApplicationBaseInputValidator : AbstractValidator<ApplicationBaseInput>
{
    public ApplicationBaseInputValidator(IServiceProvider services)
    {
        RuleFor(x => x.DisplayName)
            .NotEmpty();

        RuleFor(x => x.AutoProvision)
            .NotEmpty();

        RuleFor(x => x.EnableProvision)
            .NotEmpty();

        RuleForEach(x => x.RedirectUris)
            .IsUri();

        RuleForEach(x => x.PostLogoutRedirectUris)
            .IsUri();

        RuleFor(x => x.DefaultOrigin)
            .IsUri()
            .When(x => string.IsNullOrEmpty(x.DefaultOrigin) == false);

        When(x => x.DefaultOrigin == default, () =>
        {
            RuleFor(x => x.Origins)
                .Empty();
        });

        When(x => x.Origins?.Any() == true, () =>
        {
            RuleForEach(x => x.Origins)
                .IsUri();

            RuleFor(x => x.Origins)
                .Must((command, origins) => origins.Contains(command.DefaultOrigin!) == false)
                .WithErrorCode(nameof(ErrorCodes.DuplicateOrigin))
                .WithMessage(ErrorCodes.DuplicateOrigin);
        });
    }
}