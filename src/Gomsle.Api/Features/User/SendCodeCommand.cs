using AspNetCore.Identity.AmazonDynamoDB;
using FluentValidation;
using Gomsle.Api.Features.Email;
using Gomsle.Api.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Gomsle.Api.Features.User;

public class SendCodeCommand
{
    public class Command : IRequest<IResponse>
    {
        public string? Provider { get; set; }
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
        }
    }

    public class CommandHandler : Handler<Command, IResponse>
    {
        private readonly UserManager<DynamoDbUser> _userManager;
        private readonly SignInManager<DynamoDbUser> _signInManager;
        private readonly IEmailSender _emailSender;

        public CommandHandler(
            UserManager<DynamoDbUser> userManager,
            SignInManager<DynamoDbUser> signInManager,
            IEmailSender emailSender)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
        }

        public override async Task<IResponse> Handle(
            Command request, CancellationToken cancellationToken)
        {
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            var code = await _userManager.GenerateTwoFactorTokenAsync(user, request.Provider);

            var message = $"Your security code is: {code}";

            switch(request.Provider)
            {
                case "Email":
                    await _emailSender.Send(await _userManager.GetEmailAsync(user), "Security Code - Gömsle", message);
                    break;
            }

            return Response();
        }
    }
}
