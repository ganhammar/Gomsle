using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using FluentValidation;
using Gomsle.Api.Features.Account;
using Gomsle.Api.Infrastructure;
using Gomsle.Api.Infrastructure.Validators;
using MediatR;

namespace Gomsle.Api.Features.Application.Oidc;

public class CreateCommand
{
    public class Command : OidcProviderBaseInput, IRequest<IResponse<OidcProviderModel>>
    {
        public string? AccountId { get; set; }
    }

    public class CommandValidator : AbstractValidator<Command>
    {
        public CommandValidator(IServiceProvider services)
        {
            RuleFor(x => x)
                .SetValidator(new OidcProviderBaseInputValidator(services));

            RuleFor(x => x.AccountId)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .IsAuthenticated(services)
                .HasRoleForAccount(services, AccountRole.Administrator, AccountRole.Owner);

            RuleFor(x => x.ClientSecret)
                .NotEmpty();
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
            var model = new OidcProviderModel
            {
                AccountId = request.AccountId,
                AuthorityUrl = request.AuthorityUrl,
                ClientId = request.ClientId,
                ClientSecret = request.ClientSecret,
                IsDefault = request.IsDefault!.Value,
                IsVisible = request.IsVisible!.Value,
                Name = request.Name,
                RequiredDomains = request.RequiredDomains,
                ResponseType = request.ResponseType,
                Scopes = request.Scopes,
            };

            await _dbContext.SaveAsync(model, cancellationToken);

            return Response(model);
        }
    }
}