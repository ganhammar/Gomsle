using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using FluentValidation;
using Gomsle.Api.Features.Account;
using Gomsle.Api.Infrastructure;
using Gomsle.Api.Infrastructure.Validators;
using MediatR;

namespace Gomsle.Api.Features.Application.Oidc;

public class EditCommand
{
    public class Command : OidcProviderBaseInput, IRequest<IResponse<OidcProviderModel>>
    {
        public string? Id { get; set; }
    }

    public class CommandValidator : AbstractValidator<Command>
    {
        public CommandValidator(IServiceProvider services)
        {
            RuleFor(x => x)
                .SetValidator(new OidcProviderBaseInputValidator(services));

            RuleFor(x => x.Id)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .MustAsync(async (command, id, cancellationToken) =>
                {
                    var dbContext = new DynamoDBContext(
                        services.GetRequiredService<IAmazonDynamoDB>());
                    var oidcProvider = await dbContext.LoadAsync<OidcProviderModel>(id, cancellationToken);

                    if (oidcProvider == default)
                    {
                        return false;
                    }

                    return await HasRoleForAccount.Validate(
                        services,
                        oidcProvider.AccountId,
                        new[] { AccountRole.Administrator, AccountRole.Owner },
                        cancellationToken);
                })
                .WithErrorCode(nameof(ErrorCodes.NotAuthorized))
                .WithMessage(ErrorCodes.NotAuthorized);
        }
    }

    public class CommandHandler : Handler<Command, IResponse<OidcProviderModel>>
    {
        private readonly DynamoDBContext _dbContext;

        public CommandHandler(IAmazonDynamoDB database)
        {
            _dbContext = new DynamoDBContext(database);
        }

        public override async Task<IResponse<OidcProviderModel>> Handle(
            Command request, CancellationToken cancellationToken)
        {
            var oidcProvider = await _dbContext.LoadAsync<OidcProviderModel>(request.Id);

            oidcProvider.AuthorityUrl = request.AuthorityUrl;
            oidcProvider.ClientId = request.ClientId;
            oidcProvider.IsDefault = request.IsDefault!.Value;
            oidcProvider.IsVisible = request.IsVisible!.Value;
            oidcProvider.Name = request.Name;
            oidcProvider.RequiredDomains = request.RequiredDomains;
            oidcProvider.ResponseType = request.ResponseType;
            oidcProvider.Scopes = request.Scopes;

            if (request.ClientSecret != default)
            {
                oidcProvider.ClientSecret = request.ClientSecret;
            }

            await _dbContext.SaveAsync(oidcProvider);

            return Response(oidcProvider);
        }
    }
}