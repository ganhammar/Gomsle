using AspNetCore.Identity.AmazonDynamoDB;
using FluentValidation;
using FluentValidation.Results;
using Gomsle.Api.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Gomsle.Api.Features.User;

public class ConfirmAccountCommand
{
    public class Command : IRequest<IResponse>
    {
        public string? UserId { get; set; }
        public string? Token { get; set; }
        public string? ReturnUrl { get; set; }
    }

    public class CommandValidator : AbstractValidator<Command>
    {
        public CommandValidator(UserManager<DynamoDbUser> userManager)
        {
            RuleFor(x => x.UserId)
                .NotEmpty();

            When(x => string.IsNullOrEmpty(x.UserId) == false, () =>
            {
                RuleFor(x => x.UserId)
                    .MustAsync(async (userId, cancellationToken) => 
                    {
                        var user = await userManager.FindByIdAsync(userId);
                        return user != default;
                    })
                    .WithErrorCode("UserIdInvalid")
                    .WithMessage("Invalid user");
            });

            RuleFor(x => x.Token)
                .NotEmpty();

            RuleFor(x => x.ReturnUrl)
                .NotEmpty();
        }
    }

    public class CommandHandler : Handler<Command, IResponse>
    {
        private readonly UserManager<DynamoDbUser> _userManager;

        public CommandHandler(UserManager<DynamoDbUser> userManager)
        {
            _userManager = userManager;
        }

        public override async Task<IResponse> Handle(
            Command request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(request.UserId);
            var result = await _userManager.ConfirmEmailAsync(user, request.Token);

            if (!result.Succeeded)
            {
                return Response(result.Errors.Select(x => new ValidationFailure
                {
                    ErrorCode = x.Code,
                    ErrorMessage = x.Description,
                }));
            }

            return Response();
        }
    }
}