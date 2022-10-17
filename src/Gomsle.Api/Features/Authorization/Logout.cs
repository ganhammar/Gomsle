using AspNetCore.Identity.AmazonDynamoDB;
using FluentValidation;
using Gomsle.Api.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Gomsle.Api.Features.Authorization;

public class Logout
{
    public class Command : IRequest<IResponse>
    {
    }

    public class CommandValidator : AbstractValidator<Command>
    {
        public CommandValidator()
        {
        }
    }

    public class CommandHandler : Handler<Command, IResponse>
    {
        private readonly SignInManager<DynamoDbUser> _signInManager;

        public CommandHandler(SignInManager<DynamoDbUser> signInManager)
        {
            _signInManager = signInManager;
        }

        public override async Task<IResponse> Handle(
            Command request, CancellationToken cancellationToken)
        {
            await _signInManager.SignOutAsync();

            return Response();
        }
    }
}