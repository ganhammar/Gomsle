using System.Web;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using AspNetCore.Identity.AmazonDynamoDB;
using FluentValidation;
using Gomsle.Api.Features.Email;
using Gomsle.Api.Infrastructure;
using Gomsle.Api.Infrastructure.Validators;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Gomsle.Api.Features.Account;

public class InviteCommand
{
    public class Command : IRequest<IResponse>
    {
        public string? AccountId { get; set; }
        public string? Email { get; set; }
        public AccountRole? Role { get; set; }
        public string? InvitationUrl { get; set; }
        public string? SuccessUrl { get; set; }
    }

    public class CommandValidator : AbstractValidator<Command>
    {
        public CommandValidator(IServiceProvider services)
        {
            RuleFor(x => x.AccountId)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .IsAuthenticated(services)
                .HasRoleForAccount(services, AccountRole.Administrator, AccountRole.Owner);

            RuleFor(x => x.Email)
                .NotEmpty()
                .EmailAddress();

            RuleFor(x => x.Role)
                .NotEmpty()
                .NotEqual(AccountRole.Owner)
                .WithErrorCode(nameof(ErrorCodes.OnlyOneOwner))
                .WithMessage(ErrorCodes.OnlyOneOwner);

            RuleFor(x => x.InvitationUrl)
                .NotEmpty();

            RuleFor(x => x.SuccessUrl)
                .NotEmpty();
        }
    }

    public class CommandHandler : Handler<Command, IResponse>
    {
        private readonly DynamoDBContext _dbContext;
        private readonly UserManager<DynamoDbUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CommandHandler(
            IAmazonDynamoDB database,
            UserManager<DynamoDbUser> userManager,
            IEmailSender emailSender,
            IHttpContextAccessor httpContextAccessor)
        {
            _dbContext = new DynamoDBContext(database);
            _userManager = userManager;
            _emailSender = emailSender;
            _httpContextAccessor = httpContextAccessor;
        }

        public override async Task<IResponse> Handle(
            Command request, CancellationToken cancellationToken)
        {
            var account = await _dbContext.LoadAsync<AccountModel>(
                request.AccountId, cancellationToken);
            var accountInvitation = new AccountInvitationModel
            {
                AccountId = account.Id,
                Email = request.Email,
                Role = request.Role!.Value,
                SuccessUrl = request.SuccessUrl,
            };
            await _dbContext.SaveAsync(accountInvitation);
            await SendInvitationEmail(request, account, accountInvitation.Id, cancellationToken);

            return Response();
        }

        private async Task SendInvitationEmail(
            Command request, AccountModel account, string token, CancellationToken cancellationToken)
        {
            var url = await GetUrl(request.Email!, request.InvitationUrl!, token);
            var body = $"You've been invited to join the Gömsle account {account.Name}, follow the link below to become a member:<br /><a href=\"{url}\">{url}</a>";

            await _emailSender.Send(request.Email!, $"Invited to join {account.Name}", body, cancellationToken);
        }

        private async Task<string> GetUrl(string email, string invitationUrl, string token)
        {
            var user = await _userManager.FindByEmailAsync(email);

            if (user != default)
            {
                var request = _httpContextAccessor.HttpContext!.Request;
                return $"{request.Scheme}://{request.Host}/account/acceptinvitation?token={HttpUtility.UrlEncode(token)}";
            }

            return $"{invitationUrl}?token={token}";
        }
    }
}