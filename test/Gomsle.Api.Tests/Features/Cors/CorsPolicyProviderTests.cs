using Amazon.DynamoDBv2;
using AspNetCore.Identity.AmazonDynamoDB;
using Gomsle.Api.Features.Account;
using Gomsle.Api.Features.Cors;
using Gomsle.Api.Infrastructure;
using Gomsle.Api.Tests.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.AmazonDynamoDB;
using Xunit;
using CreateCommand = Gomsle.Api.Features.Application.CreateCommand;

namespace Gomsle.Api.Tests.Features.Cors;

[Collection("Sequential")]
public class CorsPolicyProviderTests : TestBase
{
    private async Task<IServiceProvider> Setup(IAmazonDynamoDB database, string origin)
    {
        var serviceProvider = GetServiceProvider(services =>
            services.AddSingleton<IAmazonDynamoDB>(database));

        AspNetCoreIdentityDynamoDbSetup.EnsureInitialized(serviceProvider);
        OpenIddictDynamoDbSetup.EnsureInitialized(serviceProvider);
        DynamoDbSetup.EnsureInitialized(serviceProvider);

        var user = await CreateAndLoginValidUser(serviceProvider);
        var account = await CreateAccount(serviceProvider, new()
        {
            { user.Id, AccountRole.Owner },
        });

        var mediator = serviceProvider.GetRequiredService<IMediator>();
        await mediator.Send(new CreateCommand.Command
        {
            AccountId = account.Id,
            AutoProvision = true,
            EnableProvision = true,
            DefaultOrigin = origin,
            DisplayName = "Microsoft",
        });

        return serviceProvider;
    }

    [Fact]
    public async Task Should_AllowOrigin_When_ItExists()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var origin = "http://microsoft.com";
            await Setup(database.Client, origin);

            var request = GetMock<HttpRequest>();
            request!.Setup(x => x.Headers).Returns(new HeaderDictionary
            {
                { "Origin", origin },
            });
            var httpContext = GetMock<HttpContext>();
            var corsPolicyProvider = new CorsPolicyProvider();

            // Act
            var policy = await corsPolicyProvider.GetPolicyAsync(httpContext!.Object, default);

            // Assert
            Assert.NotNull(policy);
            Assert.True(policy!.AllowAnyHeader);
            Assert.True(policy!.AllowAnyMethod);
            Assert.True(policy!.Origins.Contains(origin));
        }
    }

    [Fact]
    public async Task Should_NotAllowOrigin_When_ItDoesntExist()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            await Setup(database.Client, "http://microsoft.se");

            var request = GetMock<HttpRequest>();
            request!.Setup(x => x.Headers).Returns(new HeaderDictionary
            {
                { "Origin", "http://microsoft.com" },
            });
            var httpContext = GetMock<HttpContext>();
            var corsPolicyProvider = new CorsPolicyProvider();

            // Act
            var policy = await corsPolicyProvider.GetPolicyAsync(httpContext!.Object, default);

            // Assert
            Assert.Null(policy);
        }
    }
}