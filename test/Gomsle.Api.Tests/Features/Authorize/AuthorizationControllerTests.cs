using AspNetCore.Identity.AmazonDynamoDB;
using Gomsle.Api.Features.Account;
using Gomsle.Api.Features.Authorization;
using Gomsle.Api.Tests.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using OpenIddict.Server.AspNetCore;
using Xunit;
using static OpenIddict.Abstractions.OpenIddictConstants;
using SignInResult = Microsoft.AspNetCore.Mvc.SignInResult;

namespace Gomsle.Api.Tests.Features.Authorize;

[Collection("Sequential")]
public class AuthorizationControllerTests : TestBase
{
    private Func<IServiceProvider, object[]> ConfigureController = (services) =>
    {
        var mediator = services.GetRequiredService<IMediator>();

        return new object[] { mediator };
    };

    [Fact]
    public async Task Should_RegisterUser_When_RequestIsValid() => await ControllerTest<AuthorizationController>(
        // Arrange
        ConfigureController,
        // Act & Assert
        async (controller, services) =>
        {
            // Arrange
            var mediator = services.GetRequiredService<IMediator>();
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
            await mediator.Send(new Login.Command
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

            // Act
            var result = await controller.Authorize(new());

            // Assert
            Assert.NotNull(result);

            var signInResult = result as SignInResult;
            Assert.NotNull(signInResult);
        });

    [Fact]
    public async Task Should_ReturnChallenge_When_UserIsntAuthenticated() => await ControllerTest<AuthorizationController>(
        // Arrange
        ConfigureController,
        // Act & Assert
        async (controller, services) =>
        {
            // Arrange
            var mediator = services.GetRequiredService<IMediator>();
            var httpContext = GetMock<HttpContext>();
            var featureCollection = new FeatureCollection();
            featureCollection.Set(new OpenIddictServerAspNetCoreFeature
            {
                Transaction = new OpenIddictServerTransaction
                {
                    Request = new OpenIddictRequest(),
                },
            });
            httpContext!.Setup(x => x.Features).Returns(featureCollection);

            // Act
            var result = await controller.Authorize(new());

            // Assert
            Assert.NotNull(result);

            var challengeResult = result as ChallengeResult;
            Assert.NotNull(challengeResult);
        });

    [Fact]
    public async Task Should_ReturnForbid_When_RequestIsntValid() => await ControllerTest<AuthorizationController>(
        // Arrange
        ConfigureController,
        // Act & Assert
        async (controller, services) =>
        {
            // Arrange
            var mediator = services.GetRequiredService<IMediator>();
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

            // Act
            var result = await controller.Authorize(new());

            // Assert
            Assert.NotNull(result);

            var forbidResult = result as ForbidResult;
            Assert.NotNull(forbidResult);
        });

    [Fact]
    public async Task Should_Return_When_LoggingOut() => await ControllerTest<AuthorizationController>(
        // Arrange
        ConfigureController,
        // Act & Assert
        async (controller, services) =>
        {
            // Act
            var result = await controller.Logout(new());

            // Assert
            Assert.NotNull(result);

            var signOutResult = result as SignOutResult;
            Assert.NotNull(signOutResult);
        });
}