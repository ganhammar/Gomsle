using AspNetCore.Identity.AmazonDynamoDB;
using Gomsle.Api.Features.User;
using Gomsle.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Gomsle.Api.Tests.Features.User;

[Collection("Sequential")]
public class LoginCommandTests : TestBase
{
    [Fact]
    public async Task Should_LoginUser_When_CommandIsValid() =>
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
                LockoutEnabled = false,
            };
            await userManager.CreateAsync(user, password);

            var command = new LoginCommand.Command
            {
                Email = email,
                Password = password,
                RememberMe = false,
            };

            // Act
            var response = await mediator.Send(command);

            // Assert
            Assert.True(response.IsValid);
            Assert.True(response.Result!.Succeeded);
        });

    [Fact]
    public async Task Should_NotBeValid_When_EmailAndUserNameIsNotSet() =>
        await MediatorTest(async (mediator, services) =>
        {
            // Arrange
            var userManager = services.GetRequiredService<UserManager<DynamoDbUser>>();
            var command = new LoginCommand.Command
            {
                Password = "itsnotaseasyas123",
            };
            var validator = new LoginCommand.CommandValidator(userManager);

            // Act
            var response = await mediator.Send(command);

            // Assert
            Assert.False(response.IsValid);
            Assert.Contains(response.Errors, error =>
                error.ErrorCode == "NotEmptyValidator" && error.PropertyName == "Email");
            Assert.Contains(response.Errors, error =>
                error.ErrorCode == "NotEmptyValidator" && error.PropertyName == "UserName");
        });

    [Fact]
    public async Task Should_NotBeValid_When_PasswordIsNotSet() =>
        await MediatorTest(async (mediator, services) =>
        {
            // Arrange
            var userManager = services.GetRequiredService<UserManager<DynamoDbUser>>();
            var command = new LoginCommand.Command
            {
                Email = "valid@gomsle.com",
            };
            var validator = new LoginCommand.CommandValidator(userManager);

            // Act
            var response = await mediator.Send(command);

            // Assert
            Assert.False(response.IsValid);
            Assert.Contains(response.Errors, error =>
                error.ErrorCode == "NotEmptyValidator" && error.PropertyName == "Password");
        });

    [Fact]
    public async Task Should_NotBeValid_When_BothUserNameAndEmailIsSet() =>
        await MediatorTest(async (mediator, services) =>
        {
            // Arrange
            var userManager = services.GetRequiredService<UserManager<DynamoDbUser>>();
            var command = new LoginCommand.Command
            {
                Email = "valid@gomsle.com",
                UserName = "valid",
                Password = "itsnotaseasyas123",
            };
            var validator = new LoginCommand.CommandValidator(userManager);

            // Act
            var response = await mediator.Send(command);

            // Assert
            Assert.False(response.IsValid);
            Assert.Contains(response.Errors, error =>
                error.ErrorCode == "EmptyValidator" && error.PropertyName == "UserName");
        });
}