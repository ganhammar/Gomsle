using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using FluentValidation;
using Gomsle.Api.Features.Account;
using Gomsle.Api.Infrastructure;
using Gomsle.Api.Infrastructure.Validators;
using MediatR;
using OpenIddict.Abstractions;
using OpenIddict.AmazonDynamoDB;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Gomsle.Api.Features.Application.Oidc;

public class CreateCommand
{
    public class Command : IRequest<IResponse<OidcProviderModel>>
    {
        public string? ApplicationId { get; set; }
        public string? Name { get; set; }
        public string? AuthorityUrl { get; set; }
        public string? ClientId { get; set; }
        public string? ClientSecret { get; set; }
        public string? ResponseType { get; set; }
        public bool? IsDefault { get; set; }
        public bool? IsVisible { get; set; }
        public List<string> Scopes { get; set; } = new();
        public List<string> RequiredDomains { get; set; } = new();
    }

    public class CommandValidator : AbstractValidator<Command>
    {
        public CommandValidator(IServiceProvider services)
        {
            RuleFor(x => x.ApplicationId)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .IsAuthenticated(services)
                .MustAsync(async (command, applicationId, cancellationToken) =>
                {
                    var applicationManager = services.GetRequiredService<IOpenIddictApplicationManager>();
                    var application = (OpenIddictDynamoDbApplication?)await applicationManager
                        .FindByIdAsync(applicationId!, cancellationToken);
                    
                    if (application == default)
                    {
                        return false;
                    }

                    var dbContext = new DynamoDBContext(services.GetRequiredService<IAmazonDynamoDB>());
                    var applicationConfiguration = await dbContext.LoadAsync<ApplicationConfigurationModel>(
                        application.Id, cancellationToken);

                    if (applicationConfiguration == default)
                    {
                        return false;
                    }

                    return await HasRoleForAccount.Validate(
                        services,
                        applicationConfiguration.AccountId,
                        new[] { AccountRole.Administrator, AccountRole.Owner },
                        cancellationToken);
                })
                .WithErrorCode(nameof(ErrorCodes.MisingRoleForAccount))
                .WithMessage(ErrorCodes.MisingRoleForAccount);

            RuleFor(x => x.ClientId)
                .NotEmpty();

            RuleFor(x => x.ClientSecret)
                .NotEmpty();

            RuleFor(x => x.Name)
                .NotEmpty();

            RuleFor(x => x.AuthorityUrl)
                .NotEmpty()
                .IsUri();

            RuleFor(x => x.ResponseType)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .Must((responseType) => new[]
                    {
                        ResponseTypes.Code,
                        ResponseTypes.IdToken,
                        ResponseTypes.None,
                        ResponseTypes.Token
                    }.Contains(responseType))
                .WithErrorCode(nameof(ErrorCodes.ResponseTypeIsInvalid))
                .WithMessage(ErrorCodes.ResponseTypeIsInvalid);

            RuleFor(x => x.IsDefault)
                .NotEmpty();

            RuleFor(x => x.IsVisible)
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
                ApplicationId = request.ApplicationId,
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