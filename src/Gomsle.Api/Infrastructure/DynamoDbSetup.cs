using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using AspNetCore.Identity.AmazonDynamoDB;
using Gomsle.Api.Features.Account;
using Gomsle.Api.Features.Application;
using Gomsle.Api.Features.OidcProvider;
using Gomsle.Api.Infrastructure.Extensions;
using MediatR;
using OpenIddict.Abstractions;
using OpenIddict.AmazonDynamoDB;
using CreateCommand = Gomsle.Api.Features.Application.CreateCommand;

namespace Gomsle.Api.Infrastructure;

public static class DynamoDbSetup
{
    public static async void EnsureInitialized(IServiceProvider services)
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

        await EnsureInternalAccountsSetupAsync(services);
    }

    public static async Task EnsureInternalAccountsSetupAsync(IServiceProvider services)
    {
        using (var serviceScope = services.CreateScope())
        {
            var scopedServices = serviceScope.ServiceProvider;
            var internalOptions = scopedServices.GetRequiredService<InternalOptions>();
            var database = scopedServices.GetRequiredService<IAmazonDynamoDB>();
            var mediator = scopedServices.GetRequiredService<IMediator>();
            var applicationManager = scopedServices.GetRequiredService<IOpenIddictApplicationManager>();
            var context = new DynamoDBContext(database);

            foreach (var accountOptions in internalOptions.Accounts)
            {
                var account = await AccountHelpers.FindByName(accountOptions.Name!, context);

                if (account == default)
                {
                    account = new AccountModel
                    {
                        Name = accountOptions.Name,
                        NormalizedName = accountOptions.Name!.UrlFriendly(),
                    };
                    await context.SaveAsync(account);

                    if (accountOptions.InternalApplications?.Any() != true)
                    {
                        continue;
                    }

                    foreach (var applicationOption in accountOptions.InternalApplications)
                    {
                        applicationOption.AccountId = account.Id;
                        var commandHandler = new CreateCommand.CommandHandler(database, applicationManager);
                        await commandHandler.Handle(applicationOption, CancellationToken.None);
                    }
                }
            }
        }
    }
}