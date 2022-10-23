using AspNetCore.Identity.AmazonDynamoDB;
using FluentValidation;
using Gomsle.Api.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Gomsle.Api.Features.User;

public class VerifyCodeCommand
{
    public class Command : IRequest<IResponse<SignInResult>>
    {
        public string? Provider { get; set; }
        public string? Code { get; set; }
        public bool RememberBrowser { get; set; }
        public bool RememberMe { get; set; }
    }

    public class CommandValidator : AbstractValidator<Command>
    {
        public CommandValidator(
            SignInManager<DynamoDbUser> signInManager,
            UserManager<DynamoDbUser> userManager)
        {
            RuleFor(x => x)
                .MustAsync(async (query, cancellationToken) =>
                {
                    var user = await signInManager.GetTwoFactorAuthenticationUserAsync();

                    return user != default;
                })
                .WithErrorCode("NoLoginAttemptInProgress")
                .WithMessage("No login request is in progress");

            RuleFor(x => x.Provider)
                .NotEmpty()
                .MustAsync(async (provider, cancellationToken) =>
                {
                    var user = await signInManager.GetTwoFactorAuthenticationUserAsync();
                    if (user == default)
                    {
                        return false;
                    }

                    var providers = await userManager.GetValidTwoFactorProvidersAsync(user);

                    return providers.Contains(provider);
                })
                .WithErrorCode("TwoFactorProviderNotValid")
                .WithMessage("The selected two factor provider is not valid in the current context");

            RuleFor(x => x.Code)
                .NotEmpty();
        }
    }

    public class CommandHandler : Handler<Command, IResponse<SignInResult>>
    {
        private readonly UserManager<DynamoDbUser> _userManager;
        private readonly SignInManager<DynamoDbUser> _signInManager;

        public CommandHandler(
            UserManager<DynamoDbUser> userManager,
            SignInManager<DynamoDbUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public override async Task<IResponse<SignInResult>> Handle(
            Command request, CancellationToken cancellationToken)
        {
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            var result = await _signInManager
                .TwoFactorSignInAsync(request.Provider, request.Code, request.RememberMe, request.RememberBrowser);

            return Response(result);
        }
    }
}
