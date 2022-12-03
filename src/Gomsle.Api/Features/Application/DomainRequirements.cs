using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using FluentValidation;
using Gomsle.Api.Features.OidcProvider;
using Gomsle.Api.Infrastructure;
using Gomsle.Api.Infrastructure.Extensions;
using MediatR;

namespace Gomsle.Api.Features.Application;

public class DomainRequirements
{
    public class Query : ApplicationBaseInput, IRequest<IResponse<QueryResult>>
    {
        public string? Domain { get; set; }
    }

    public class QueryValidator : AbstractValidator<Query>
    {
        public QueryValidator(IServiceProvider services)
        {
            RuleFor(x => x.Domain)
                .NotEmpty();

            RuleFor(x => x)
                .MustAsync(async (x, cancellationToken) =>
                {
                    var httpContextAccessor = services.GetRequiredService<IHttpContextAccessor>();

                    if (httpContextAccessor.HttpContext == default)
                    {
                        return false;
                    }

                    var applicationId = await httpContextAccessor.HttpContext
                        .GetCurrentApplicationId(cancellationToken);
                    return applicationId != default;
                });
        }
    }

    public class QueryResult
    {
        public string? RequiredOidcProviderId { get; set; }
    }

    public class QueryHandler : Handler<Query, IResponse<QueryResult>>
    {
        private readonly DynamoDBContext _dbContext;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public QueryHandler(
            IAmazonDynamoDB database,
            IHttpContextAccessor httpContextAccessor)
        {
            _dbContext = new DynamoDBContext(database);
            _httpContextAccessor = httpContextAccessor;
        }

        public override async Task<IResponse<QueryResult>> Handle(
            Query request, CancellationToken cancellationToken)
        {
            var applicationId = await _httpContextAccessor.HttpContext!
                .GetCurrentApplicationId(cancellationToken);
            var applicationConfiguration = await _dbContext.LoadAsync<ApplicationConfigurationModel>(applicationId);
            var search = _dbContext.FromQueryAsync<OidcProviderRequiredDomainModel>(new QueryOperationConfig
            {
                KeyExpression = new Expression
                {
                    ExpressionStatement = "RequiredDomain = :domain",
                    ExpressionAttributeValues = new Dictionary<string, DynamoDBEntry>
                    {
                        { ":domain", request.Domain },
                    },
                },
                Limit = 1,
            });
            var domains = await search.GetRemainingAsync(cancellationToken);
            var domain = domains
                .Where(x => x.OidcProviderId != default)
                .Where(x => applicationConfiguration.ConnectedOidcProviders.Contains(x.OidcProviderId!))
                .FirstOrDefault();

            return Response(new QueryResult()
            {
                RequiredOidcProviderId = domain?.OidcProviderId,
            });
        }
    }
}