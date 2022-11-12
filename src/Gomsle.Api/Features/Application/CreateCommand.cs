using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using FluentValidation;
using Gomsle.Api.Features.Account;
using Gomsle.Api.Infrastructure;
using Gomsle.Api.Infrastructure.Validators;
using MediatR;
using OpenIddict.Abstractions;
using OpenIddict.AmazonDynamoDB;

namespace Gomsle.Api.Features.Application;

public class CreateCommand
{
    public class Command : ApplicationBaseInput, IRequest<IResponse<ApplicationDto>>
    {
        public string? AccountId { get; set; }
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
            var applicationDescriptor = new OpenIddictApplicationDescriptor
            {
                DisplayName = request.DisplayName,
                ClientId = Guid.NewGuid().ToString(),
                Permissions =
                {
                    "ept:authorization",
                    "ept:logout",
                    "gt:implicit",
                    "gt:refresh_token",
                    "rst:id_token",
                    "rst:token",
                    "scp:email",
                    "scp:profile",
                    "scp:roles",
                    "scp:gomsle_api",
                },
            };

            if (request.RedirectUris != default)
            {
                foreach(var redirectUri in request.RedirectUris)
                {
                    applicationDescriptor.RedirectUris
                        .Add(new Uri(redirectUri));
                }
            }

            if (request.PostLogoutRedirectUris != default)
            {
                foreach(var postLogoutRedirectUri in request.PostLogoutRedirectUris)
                {
                    applicationDescriptor.PostLogoutRedirectUris
                        .Add(new Uri(postLogoutRedirectUri));
                }
            }

            var application = (OpenIddictDynamoDbApplication)await _applicationManager
                .CreateAsync(applicationDescriptor, cancellationToken);

            var applicationConfiguration = new ApplicationConfigurationModel
            {
                AccountId = request.AccountId,
                EnableProvision = request.EnableProvision!.Value,
                AutoProvision = request.AutoProvision!.Value,
                ApplicationId = application.Id,
            };
            await _dbContext.SaveAsync(applicationConfiguration);
            var origins = await SaveOrigins(request, application.Id, cancellationToken);

            return Response(ApplicationDtoMapper.ToDto(application, applicationConfiguration, origins));
        }

        private async Task<List<ApplicationOriginModel>> SaveOrigins(Command request, string applicationId, CancellationToken cancellationToken)
        {
            var result = new List<ApplicationOriginModel>();
            var origins = request.Origins;
            if (string.IsNullOrEmpty(request.DefaultOrigin) == false)
            {
                origins.Add(request.DefaultOrigin);
            }

            if (origins.Any() == false)
            {
                return result;
            }

            var batch = _dbContext.CreateBatchWrite<ApplicationOriginModel>();

            foreach (var origin in origins)
            {
                var model = new ApplicationOriginModel
                {
                    ApplicationId = applicationId,
                    IsDefault = origin == request.DefaultOrigin,
                    Origin = origin,
                };
                batch.AddPutItem(model);
                result.Add(model);
            }

            await batch.ExecuteAsync(cancellationToken);

            return result;
        }
    }
}