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
    public class Command : IRequest<IResponse<ApplicationDto>>
    {
        public string? AccountId { get; set; }
        public string? DisplayName { get; set; }
        public bool? AutoProvision { get; set; }
        public bool? EnableProvision { get; set; }
        public List<string> RedirectUris { get; set; } = new();
        public List<string> PostLogoutRedirectUris { get; set; } = new();
        public string? DefaultOrigin { get; set; }
        public List<string> Origins { get; set; } = new();
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

            RuleFor(x => x.DisplayName)
                .NotEmpty();

            RuleFor(x => x.AutoProvision)
                .NotEmpty();

            RuleFor(x => x.EnableProvision)
                .NotEmpty();

            RuleForEach(x => x.RedirectUris)
                .IsUri();

            RuleForEach(x => x.PostLogoutRedirectUris)
                .IsUri();

            RuleFor(x => x.DefaultOrigin)
                .IsUri()
                .When(x => string.IsNullOrEmpty(x.DefaultOrigin) == false);

            When(x => x.DefaultOrigin == default, () =>
            {
                RuleFor(x => x.Origins)
                    .Empty();
            });

            When(x => x.Origins?.Any() == true, () =>
            {
                RuleForEach(x => x.Origins)
                    .IsUri();
                
                RuleFor(x => x.Origins)
                    .Must((command, origins) => origins.Contains(command.DefaultOrigin!) == false)
                    .WithErrorCode(nameof(ErrorCodes.DuplicateOrigin))
                    .WithMessage(ErrorCodes.DuplicateOrigin);
            });
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