using Amazon.DynamoDBv2;
using AspNetCore.Identity.AmazonDynamoDB;
using Gomsle.Api.Features.Account;
using OpenIddict.AmazonDynamoDB;

namespace Gomsle.Api.Infrastructure;

public static class DynamoDbSetup
{
    public static void EnsureInitialized(IServiceProvider services)
    {
        var database = services.GetService<IAmazonDynamoDB>();

        ArgumentNullException.ThrowIfNull(database);

        AspNetCoreIdentityDynamoDbSetup.EnsureInitialized(services);
        OpenIddictDynamoDbSetup.EnsureInitialized(services);

        var promises = new[]
        {
            AccountSetup.EnsureInitializedAsync(database),
        };

        Task.WhenAll(promises).GetAwaiter().GetResult();
    }
}