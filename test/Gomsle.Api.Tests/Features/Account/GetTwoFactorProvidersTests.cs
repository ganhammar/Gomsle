using AspNetCore.Identity.AmazonDynamoDB;
using Gomsle.Api.Features.Account;
using Gomsle.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Gomsle.Api.Tests.Features.Account;

[Collection("Sequential")]
public class GetTwoFactorProvidersTests : TestBase
{
    [Fact]
    public async Task Should_ReturnListOfTwoFactorProviders_When_CommandIsValid() =>
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
                TwoFactorEnabled = true,
                LockoutEnabled = false,
            };
            await userManager.CreateAsync(user, password);

            var command = new Login.Command
            {
                Email = email,
                Password = password,
                RememberMe = false,
            };
            var query = new GetTwoFactorProviders.Query();
            await mediator.Send(command);

            // Act
            var providers = await mediator.Send(query);

            // Assert
            Assert.True(providers.IsValid);
            Assert.True(providers.Result!.Any());
        });

    [Fact]
    public async Task Should_NotBeValid_When_ThereIsNoLoginInProgress() =>
        await MediatorTest(async (mediator, services) =>
        {
            // Arrange
            var userManager = services.GetRequiredService<UserManager<DynamoDbUser>>();
            var signInManager = services.GetRequiredService<SignInManager<DynamoDbUser>>();
            var validator = new GetTwoFactorProviders.QueryValidator(signInManager);
            var query = new GetTwoFactorProviders.Query();

            // Act
            var response = await validator.ValidateAsync(query);

            // Assert
            Assert.False(response.IsValid);
            Assert.Contains(response.Errors, error =>
                error.ErrorCode == "NoLoginAttemptInProgress");
        });
}