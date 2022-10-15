using AspNetCore.Identity.AmazonDynamoDB;
using FluentValidation;
using Gomsle.Api.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Gomsle.Api.Features.Account;

public class GetTwoFactorProviders
{
    public class Query : IRequest<IResponse<List<string>>>
    {
    }
    
    public class QueryValidator : AbstractValidator<Query>
    {
        public QueryValidator(SignInManager<DynamoDbUser> signInManager)
        {
            RuleFor(x => x)
                .MustAsync(async (query, cancellationToken) =>
                {
                    var user = await signInManager.GetTwoFactorAuthenticationUserAsync();

                    return user != default;
                })
                .WithErrorCode("NoLoginAttemptInProgress")
                .WithMessage("No login request is in progress");
        }
    }

    public class CommandHandler : Handler<Query, IResponse<List<string>>>
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

        public override async Task<IResponse<List<string>>> Handle(
            Query request, CancellationToken cancellationToken)
        {
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            var providers = await _userManager.GetValidTwoFactorProvidersAsync(user);

            return Response(providers.ToList());
        }
    }
}