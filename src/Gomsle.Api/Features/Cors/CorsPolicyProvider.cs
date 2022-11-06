using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Gomsle.Api.Features.Application;
using Microsoft.AspNetCore.Cors.Infrastructure;

namespace Gomsle.Api.Features.Cors;

public class CorsPolicyProvider : ICorsPolicyProvider
{
    public async Task<CorsPolicy?> GetPolicyAsync(HttpContext context, string? policyName)
    {
        var origin = context.Request.GetCorsOrigin();

        if (origin == default)
        {
            return null;
        }

        var database = context.RequestServices.GetRequiredService<IAmazonDynamoDB>();
        var dbContext = new DynamoDBContext(database);
        var search = dbContext.FromQueryAsync<ApplicationOriginModel>(new QueryOperationConfig
        {
            KeyExpression = new Expression
            {
                ExpressionStatement = "Origin = :origin",
                ExpressionAttributeValues = new Dictionary<string, DynamoDBEntry>
                {
                    { ":origin", origin },
                }
            },
            Limit = 1,
        });
        var origins = await search.GetNextSetAsync();

        if (origins.Any())
        {
            return Allow(origin);
        }

        return null;
    }

    private CorsPolicy Allow(string origin)
    {
        var policyBuilder = new CorsPolicyBuilder()
            .WithOrigins(origin)
            .AllowAnyHeader()
            .AllowAnyMethod();

        policyBuilder.SetPreflightMaxAge(TimeSpan.FromHours(1));

        return policyBuilder.Build();
    }
}