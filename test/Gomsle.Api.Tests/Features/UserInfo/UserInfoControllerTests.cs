using AspNetCore.Identity.AmazonDynamoDB;
using Gomsle.Api.Features.Account;
using Gomsle.Api.Features.UserInfo;
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

namespace Gomsle.Api.Tests.Features.UserInfo;

[Collection("Sequential")]
public class UserInfoControllerTests : TestBase
{
    private Func<IServiceProvider, object[]> ConfigureController = (services) =>
    {
        var mediator = services.GetRequiredService<IMediator>();

        return new object[] { mediator };
    };

    [Fact]
    public async Task Should_ReturnOk_When_UserInfoRequestIsValid() => await ControllerTest<UserInfoController>(
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
            var result = await controller.UserInfo(new());

            // Assert
            Assert.NotNull(result);

            var okObjectResult = result as OkObjectResult;
            Assert.NotNull(okObjectResult);
        });

    [Fact]
    public async Task Should_ReturnChallenge_When_UserIsntAuthenticated() => await ControllerTest<UserInfoController>(
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
            var result = await controller.UserInfo(new());

            // Assert
            Assert.NotNull(result);

            var challengeResult = result as ChallengeResult;
            Assert.NotNull(challengeResult);
        });
}