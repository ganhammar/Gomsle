using AspNetCore.Identity.AmazonDynamoDB;
using Gomsle.Api.Features.User;
using Gomsle.Api.Features.Authorization;
using Gomsle.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using OpenIddict.Server.AspNetCore;
using Xunit;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Gomsle.Api.Tests.Features.Authorize;

[Collection("Sequential")]
public class AuthorizeCommandTests : TestBase
{
    [Fact]
    public async Task Should_ReturnPrincipal_When_UserIsAuthenticated() =>
        await MediatorTest(async (mediator, services) =>
        {
            // Arrange
            var userManager = services.GetRequiredService<UserManager<DynamoDbUser>>();
            var email = "valid@gomsle.com";
            var password = "itsaseasyas123";
            var user = new DynamoDbUser
            {
                Email = email,
                UserName = email,
                EmailConfirmed = true,
                TwoFactorEnabled = false,
            };
            await userManager.CreateAsync(user, password);
            await mediator.Send(new LoginCommand.Command
            {
                Email = email,
                Password = password,
                RememberMe = false,
            });
            var httpContext = GetMock<HttpContext>();
            var featureCollection = new FeatureCollection();
            featureCollection.Set(new OpenIddictServerAspNetCoreFeature
            {
                Transaction = new OpenIddictServerTransaction
                {
                    Request = new OpenIddictRequest
                    {
                        Scope = "test",
                    },
                },
            });
            httpContext!.Setup(x => x.Features).Returns(featureCollection);
            var command = new AuthorizeCommand.Command();

            // Act
            var result = await mediator.Send(command);

            // Assert
            Assert.True(result.IsValid);
            Assert.NotNull(result.Result!.Identity);
            Assert.True(result.Result!.Identity!.IsAuthenticated);
        });

    [Fact]
    public async Task Should_NotBeValid_When_AuthorizationRequestIsNotSet() =>
        await MediatorTest(async (mediator, services) =>
        {
            // Arrange
            var httpContext = GetMock<HttpContext>();
            var featureCollection = new FeatureCollection();
            featureCollection.Set(new OpenIddictServerAspNetCoreFeature
            {
                Transaction = new OpenIddictServerTransaction(),
            });
            httpContext!.Setup(x => x.Features).Returns(featureCollection);
            var command = new AuthorizeCommand.Command();

            // Act
            var response = await mediator.Send(command);

            // Assert
            Assert.False(response.IsValid);
            Assert.Contains(response.Errors, error =>
                error.ErrorCode == "NoAuthorizationRequestInProgress");
        });

    [Fact]
    public async Task Should_RequireLogin_When_UserIsntAuthenticated() =>
        await MediatorTest(async (mediator, services) =>
        {
            // Arrange
            var httpContext = GetMock<HttpContext>();
            var featureCollection = new FeatureCollection();
            featureCollection.Set(new OpenIddictServerAspNetCoreFeature
            {
                Transaction = new OpenIddictServerTransaction
                {
                    Request = new OpenIddictRequest
                    {
                        Prompt = Prompts.None,
                    },
                },
            });
            httpContext!.Setup(x => x.Features).Returns(featureCollection);
            var command = new AuthorizeCommand.Command();

            // Act
            var response = await mediator.Send(command);

            // Assert
            Assert.False(response.IsValid);
            Assert.Contains(response.Errors, error =>
                error.ErrorCode == Errors.LoginRequired);
        });
}