using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using AspNetCore.Identity.AmazonDynamoDB;
using FluentValidation;
using Gomsle.Api.Features.Email;
using Gomsle.Api.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Gomsle.Api.Features.Account;

public class InviteCommand
{
    public class Command : IRequest<IResponse>
    {
        public string? AccountName { get; set; }
        public string? Email { get; set; }
        public AccountRole? Role { get; set; }
        public string? InvitationUrl { get; set; }
    }

    public class CommandValidator : AbstractValidator<Command>
    {
        public CommandValidator(IAmazonDynamoDB database)
        {
            RuleFor(x => x.AccountName)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .MustAsync(async (accountName, cancellationToken) =>
                {
                    var context = new DynamoDBContext(database);
                    var account = await context.LoadAsync<AccountModel>(
                        accountName!.UrlFriendly(), cancellationToken);

                    return account != default;
                })
                .WithErrorCode("AccountNotFound")
                .WithMessage("No account with that name found");

            RuleFor(x => x.Email)
                .NotEmpty()
                .EmailAddress();

            RuleFor(x => x.Role)
                .NotEmpty()
                .NotEqual(AccountRole.Owner)
                .WithErrorCode("OnlyOneOwner")
                .WithMessage("There can be only one owner");

            RuleFor(x => x.InvitationUrl)
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
            var accountInvitation = new AccountInvitationModel
            {
                AccountName = request.AccountName,
                Email = request.Email,
                Role = request.Role!.Value,
            };
            await _dbContext.SaveAsync(accountInvitation);
            await SendInvitationEmail(request, accountInvitation.Id, cancellationToken);

            return Response();
        }

        private async Task SendInvitationEmail(Command request, string token, CancellationToken cancellationToken)
        {
            var url = $"{request.InvitationUrl}?token={token}";
            var account = await _dbContext.LoadAsync<AccountModel>(
                request.AccountName!.UrlFriendly(), cancellationToken);

            var body = $"You've been invited to join the Gömsle account {account.Name}, follow the link below to become a member:<br /><a href=\"{url}\">{url}</a>";

            await _emailSender.Send(request.Email!, $"Invited to join {account.Name}", body, cancellationToken);
        }
    }
}