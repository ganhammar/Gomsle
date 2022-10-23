using AspNetCore.Identity.AmazonDynamoDB;
using FluentValidation;
using Gomsle.Api.Features.Email;
using Gomsle.Api.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Gomsle.Api.Features.User;

public class ForgotPasswordCommand
{
    public class Command : IRequest<IResponse>
    {
        public string? Email { get; set; }
        public string? ResetUrl { get; set; }
    }

    public class CommandValidator : AbstractValidator<Command>
    {
        public CommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .EmailAddress();

            RuleFor(x => x.ResetUrl)
                .NotEmpty();
        }
    }

    public class CommandHandler : Handler<Command, IResponse>
    {
        private readonly UserManager<DynamoDbUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CommandHandler(
            UserManager<DynamoDbUser> userManager,
            IEmailSender emailSender,
            IHttpContextAccessor httpContextAccessor)
        {
            _userManager = userManager;
            _emailSender = emailSender;
            _httpContextAccessor = httpContextAccessor;
        }

        public override async Task<IResponse> Handle(
            Command request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);

            if (user != default)
            {
                await SendResetEmail(user, request.ResetUrl);
            }

            return Response();
        }

        private async Task SendResetEmail(DynamoDbUser user, string? returnUrl)
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var request = _httpContextAccessor.HttpContext!.Request;
            var url = $"{request.Protocol}://{request.Host}/account/reset"
                + $"?UserId={user.Id}&Token={token}&ReturnUrl={returnUrl}";

            var body = $"Follow the link below to reset your Gömsle account password:<br /><a href=\"{url}\">{url}</a>";

            await _emailSender.Send(user.Email, "Reset Password", body);
        }
    }
}