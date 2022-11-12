using Amazon.DynamoDBv2;
using AspNetCore.Identity.AmazonDynamoDB;
using Gomsle.Api.Features.Account;
using Gomsle.Api.Features.Application;
using Gomsle.Api.Features.OidcProvider;
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
            ApplicationSetup.EnsureInitializedAsync(database),
            OidcProviderSetup.EnsureInitializedAsync(database),
        };

        Task.WhenAll(promises).GetAwaiter().GetResult();
    }
}