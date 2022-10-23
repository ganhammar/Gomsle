using AspNetCore.Identity.AmazonDynamoDB;
using Gomsle.Api.Features.User;
using Gomsle.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Gomsle.Api.Tests.Features.User;

[Collection("Sequential")]
public class VerifyCodeCommandTests : TestBase
{
    [Fact]
    public async Task Should_LogUserIn_When_CommandIsValid() =>
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
            };
            await userManager.CreateAsync(user, password);
            await mediator.Send(new LoginCommand.Command
            {
                Email = email,
                Password = password,
                RememberMe = false,
            });
            var code = await userManager.GenerateTwoFactorTokenAsync(user, "Email");
            var command = new VerifyCodeCommand.Command
            {
                Code = code,
                Provider = "Email",
            };

            // Act
            var result = await mediator.Send(command);

            // Assert
            Assert.True(result.IsValid);
            Assert.True(result.Result!.Succeeded);
        });

    [Fact]
    public async Task Should_NotBeValid_When_ProviderIsNotSet() =>
        await MediatorTest(async (mediator, services) =>
        {
            // Arrange
            var userManager = services.GetRequiredService<UserManager<DynamoDbUser>>();
            var signInManager = services.GetRequiredService<SignInManager<DynamoDbUser>>();
            var command = new VerifyCodeCommand.Command
            {
                Code = "test",
            };
            var validator = new VerifyCodeCommand.CommandValidator(signInManager, userManager);

            // Act
            var response = await mediator.Send(command);

            // Assert
            Assert.False(response.IsValid);
            Assert.Contains(response.Errors, error =>
                error.ErrorCode == "NotEmptyValidator" && error.PropertyName == "Provider");
        });

    [Fact]
    public async Task Should_NotBeValid_When_CodeIsNotSet() =>
        await MediatorTest(async (mediator, services) =>
        {
            // Arrange
            var userManager = services.GetRequiredService<UserManager<DynamoDbUser>>();
            var signInManager = services.GetRequiredService<SignInManager<DynamoDbUser>>();
            var command = new VerifyCodeCommand.Command
            {
                Provider = "test",
            };
            var validator = new VerifyCodeCommand.CommandValidator(signInManager, userManager);

            // Act
            var response = await mediator.Send(command);

            // Assert
            Assert.False(response.IsValid);
            Assert.Contains(response.Errors, error =>
                error.ErrorCode == "NotEmptyValidator" && error.PropertyName == "Code");
        });

    [Fact]
    public async Task Should_NotBeValid_When_NoLoginAttempIsInProgress() =>
        await MediatorTest(async (mediator, services) =>
        {
            // Arrange
            var userManager = services.GetRequiredService<UserManager<DynamoDbUser>>();
            var command = new VerifyCodeCommand.Command
            {
                Provider = "Email",
                Code = "test",
            };

            // Act
            var response = await mediator.Send(command);

            // Assert
            Assert.False(response.IsValid);
            Assert.Contains(response.Errors, error =>
                error.ErrorCode == "NoLoginAttemptInProgress");
        });

    [Fact]
    public async Task Should_NotBeValid_When_ProviderIsAllowed() =>
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
            };
            await userManager.CreateAsync(user, password);
            await mediator.Send(new LoginCommand.Command
            {
                Email = email,
                Password = password,
                RememberMe = false,
            });
            var command = new VerifyCodeCommand.Command
            {
                Provider = "Phone",
                Code = "test",
            };

            // Act
            var response = await mediator.Send(command);

            // Assert
            Assert.False(response.IsValid);
            Assert.Contains(response.Errors, error =>
                error.ErrorCode == "TwoFactorProviderNotValid" && error.PropertyName == "Provider");
        });
}