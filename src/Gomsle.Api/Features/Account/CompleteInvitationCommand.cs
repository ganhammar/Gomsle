using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using AspNetCore.Identity.AmazonDynamoDB;
using FluentValidation;
using FluentValidation.Results;
using Gomsle.Api.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Gomsle.Api.Features.Account;

public class CompleteInvitationCommand
{
    public class Command : IRequest<IResponse<DynamoDbUser>>
    {
        public string? Token { get; set; }
        public string? Password { get; set; }
        public string? UserName { get; set; }
    }

    public class CommandValidator : AbstractValidator<Command>
    {
        public CommandValidator(IAmazonDynamoDB database)
        {
            RuleFor(x => x.Token)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .MustAsync(async (token, cancellationToken) =>
                {
                    var context = new DynamoDBContext(database);
                    var invitation = await context.LoadAsync<AccountInvitationModel>(
                        token, cancellationToken);

                    return invitation != default;
                })
                .WithErrorCode("TokenNotValid")
                .WithMessage("The invitation token is not valid");

            RuleFor(x => x.Password)
                .NotEmpty();
        }
    }

    public class CommandHandler : Handler<Command, IResponse<DynamoDbUser>>
    {
        private readonly DynamoDBContext _dbContext;
        private readonly UserManager<DynamoDbUser> _userManager;

        public CommandHandler(
            IAmazonDynamoDB database,
            UserManager<DynamoDbUser> userManager)
        {
            _dbContext = new DynamoDBContext(database);
            _userManager = userManager;
        }

        public override async Task<IResponse<DynamoDbUser>> Handle(
            Command request, CancellationToken cancellationToken)
        {
            var invitation = await _dbContext.LoadAsync<AccountInvitationModel>(
                request.Token, cancellationToken);
            var account = await _dbContext.LoadAsync<AccountModel>(
                invitation.NormalizedAccountName, cancellationToken);
            var user = await _userManager.FindByEmailAsync(invitation.Email);

            if (user == default)
            {
                user = new DynamoDbUser
                {
                    UserName = request.UserName ?? invitation.Email,
                    Email = invitation.Email,
                    EmailConfirmed = true,
                    PhoneNumberConfirmed = false,
                };

                var result = await _userManager.CreateAsync(user, request.Password);

                if (result.Succeeded == false)
                {
                    return Response<DynamoDbUser>(new(), result.Errors.Select(x => new ValidationFailure
                    {
                        ErrorCode = x.Code,
                        ErrorMessage = x.Description,
                    }));
                }
            }

            account.Members.Add(user.Id, invitation.Role);
            await _dbContext.SaveAsync(account);

            return Response(user);
        }
    }
}