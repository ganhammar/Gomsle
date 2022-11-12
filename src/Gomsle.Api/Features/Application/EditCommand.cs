using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using FluentValidation;
using Gomsle.Api.Features.Account;
using Gomsle.Api.Infrastructure;
using Gomsle.Api.Infrastructure.Validators;
using MediatR;
using OpenIddict.Abstractions;
using OpenIddict.AmazonDynamoDB;

namespace Gomsle.Api.Features.Application;

public class EditCommand
{
    public class Command : ApplicationBaseInput, IRequest<IResponse<ApplicationDto>>
    {
        public string? Id { get; set; }
    }

    public class CommandValidator : AbstractValidator<Command>
    {
        public CommandValidator(IServiceProvider services)
        {
            RuleFor(x => x.Id)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .MustAsync(async (command, id, cancellationToken) =>
                {
                    var dbContext = new DynamoDBContext(
                        services.GetRequiredService<IAmazonDynamoDB>());
                    var applicationConfiguration = await dbContext.LoadAsync<ApplicationConfigurationModel>(
                        id, cancellationToken);

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
                .WithErrorCode(nameof(ErrorCodes.NotAuthorized))
                .WithMessage(ErrorCodes.NotAuthorized);

            RuleFor(x => x)
                .SetValidator(new ApplicationBaseInputValidator(services));
        }
    }

    public class CommandHandler : Handler<Command, IResponse<ApplicationDto>>
    {
        private readonly DynamoDBContext _dbContext;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IOpenIddictApplicationManager _applicationManager;

        public CommandHandler(
            IAmazonDynamoDB database,
            IHttpContextAccessor httpContextAccessor,
            IOpenIddictApplicationManager applicationManager)
        {
            _dbContext = new DynamoDBContext(database);
            _httpContextAccessor = httpContextAccessor;
            _applicationManager = applicationManager;
        }

        public override async Task<IResponse<ApplicationDto>> Handle(
            Command request, CancellationToken cancellationToken)
        {
            var application = (OpenIddictDynamoDbApplication)(await _applicationManager.FindByIdAsync(request.Id!))!;
            var applicationConfiguration = await _dbContext.LoadAsync<ApplicationConfigurationModel>(
                request.Id!, cancellationToken);

            application.DisplayName = request.DisplayName;
            application.RedirectUris = request.RedirectUris;
            application.PostLogoutRedirectUris = request.PostLogoutRedirectUris;

            applicationConfiguration.AutoProvision = request.AutoProvision!.Value;
            applicationConfiguration.EnableProvision = request.EnableProvision!.Value;
            applicationConfiguration.ConnectedOidcProviders = request.ConnectedOidcProviders;

            var origins = await SaveOrigins(request, cancellationToken);
            await _dbContext.SaveAsync(application);
            await _dbContext.SaveAsync(applicationConfiguration);

            return Response(ApplicationDtoMapper.ToDto(application, applicationConfiguration, origins));
        }

        public async Task<List<ApplicationOriginModel>> GetOrigins(
            string applicationId, CancellationToken cancellationToken)
        {
            var search = _dbContext.FromQueryAsync<ApplicationOriginModel>(new QueryOperationConfig
            {
                IndexName = "ApplicationId-index",
                KeyExpression = new Expression
                {
                    ExpressionStatement = "ApplicationId = :applicationId",
                    ExpressionAttributeValues = new Dictionary<string, DynamoDBEntry>
                {
                    { ":applicationId", applicationId },
                },
                },
            });
            return await search.GetRemainingAsync(cancellationToken);
        }

        public async Task RemoveDeletedLogins(
            List<ApplicationOriginModel> origins,
            string applicationId,
            CancellationToken cancellationToken)
        {
            var persistedOrigins = await GetOrigins(applicationId, cancellationToken);

            var toBeDeleted = persistedOrigins.Except(origins);

            if (toBeDeleted.Any())
            {
                var batch = _dbContext.CreateBatchWrite<ApplicationOriginModel>();

                foreach (var login in toBeDeleted)
                {
                    batch.AddDeleteItem(login);
                }

                await batch.ExecuteAsync();
            }
        }

        public async Task<List<ApplicationOriginModel>> SaveOrigins(
            Command request, CancellationToken cancellationToken)
        {
            var origins = request.Origins;
            if (string.IsNullOrEmpty(request.DefaultOrigin) == false)
            {
                origins.Add(request.DefaultOrigin);
            }

            var originModels = origins
                .Select(x => new ApplicationOriginModel
                {
                    ApplicationId = request.Id!,
                    IsDefault = request.DefaultOrigin == x,
                    Origin = x,
                })
                .ToList();

            await RemoveDeletedLogins(originModels, request.Id!, cancellationToken);

            if (originModels.Any() == false)
            {
                return originModels;
            }

            var batch = _dbContext.CreateBatchWrite<ApplicationOriginModel>();

            foreach (var model in originModels)
            {
                batch.AddPutItem(model);
            }

            await batch.ExecuteAsync(cancellationToken);

            return originModels;
        }
    }
}