using AspNetCore.Identity.AmazonDynamoDB;
using Gomsle.Api.Features.User;
using Gomsle.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Gomsle.Api.Tests.Features.User;

[Collection("Sequential")]
public class ConfirmEmailCommandTests : TestBase
{
    [Fact]
    public async Task Should_ConfirmEmailCommand_When_CommandIsValid() =>
        await MediatorTest(async (mediator, services) =>
        {
            // Arrange
            var userManager = services.GetRequiredService<UserManager<DynamoDbUser>>();
            var user = new DynamoDbUser
            {
                Email = "test@gomsle.com",
                UserName = "test@gomsle.com",
                EmailConfirmed = false,
            };
            await userManager.CreateAsync(user);
            var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
            var command = new ConfirmEmailCommand.Command
            {
                UserId = user.Id,
                Token = token,
                ReturnUrl = "https://gomsle.com",
            };

            // Act
            var response = await mediator.Send(command);

            // Assert
            Assert.True(response.IsValid);
        });

    [Fact]
    public async Task Should_NotBeValid_When_UserIdIsNotSet() =>
        await MediatorTest(async (mediator, services) =>
        {
            // Arrange
            var userManager = services.GetRequiredService<UserManager<DynamoDbUser>>();
            var user = new DynamoDbUser
            {
                Email = "test@gomsle.com",
                UserName = "test@gomsle.com",
                EmailConfirmed = false,
            };
            await userManager.CreateAsync(user);
            var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
            var command = new ConfirmEmailCommand.Command
            {
                Token = token,
                ReturnUrl = "https://gomsle.com",
            };
            var validator = new ConfirmEmailCommand.CommandValidator(userManager);

            // Act
            var response = await validator.ValidateAsync(command);

            // Assert
            Assert.False(response.IsValid);
            Assert.Contains(response.Errors, error =>
                error.ErrorCode == "NotEmptyValidator" && error.PropertyName == "UserId");
        });

    [Fact]
    public async Task Should_NotBeValid_When_UserDoesntExist() =>
        await MediatorTest(async (mediator, services) =>
        {
            // Arrange
            var userManager = services.GetRequiredService<UserManager<DynamoDbUser>>();
            var user = new DynamoDbUser
            {
                Email = "test@gomsle.com",
                UserName = "test@gomsle.com",
                EmailConfirmed = false,
            };
            await userManager.CreateAsync(user);
            var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
            var command = new ConfirmEmailCommand.Command
            {
                UserId = Guid.NewGuid().ToString(),
                Token = token,
                ReturnUrl = "https://gomsle.com",
            };
            var validator = new ConfirmEmailCommand.CommandValidator(userManager);

            // Act
            var response = await validator.ValidateAsync(command);

            // Assert
            Assert.False(response.IsValid);
            Assert.Contains(response.Errors, error =>
                error.ErrorCode == "UserIdInvalid" && error.PropertyName == "UserId");
        });

    [Fact]
    public async Task Should_NotBeValid_When_TokenIsNotSet() =>
        await MediatorTest(async (mediator, services) =>
        {
            // Arrange
            var userManager = services.GetRequiredService<UserManager<DynamoDbUser>>();
            var user = new DynamoDbUser
            {
                Email = "test@gomsle.com",
                UserName = "test@gomsle.com",
            };
            await userManager.CreateAsync(user);
            var command = new ConfirmEmailCommand.Command
            {
                UserId = user.Id,
                ReturnUrl = "https://gomsle.com",
            };
            var validator = new ConfirmEmailCommand.CommandValidator(userManager);

            // Act
            var response = await validator.ValidateAsync(command);

            // Assert
            Assert.False(response.IsValid);
            Assert.Contains(response.Errors, error =>
                error.ErrorCode == "NotEmptyValidator" && error.PropertyName == "Token");
        });

    [Fact]
    public async Task Should_NotBeValid_When_ReturnUrlIsNotSet() =>
        await MediatorTest(async (mediator, services) =>
        {
            // Arrange
            var userManager = services.GetRequiredService<UserManager<DynamoDbUser>>();
            var user = new DynamoDbUser
            {
                Email = "test@gomsle.com",
                UserName = "test@gomsle.com",
            };
            await userManager.CreateAsync(user);
            var command = new ConfirmEmailCommand.Command
            {
                UserId = user.Id,
                Token = "a-confirm-token",
            };
            var validator = new ConfirmEmailCommand.CommandValidator(userManager);

            // Act
            var response = await validator.ValidateAsync(command);

            // Assert
            Assert.False(response.IsValid);
            Assert.Contains(response.Errors, error =>
                error.ErrorCode == "NotEmptyValidator" && error.PropertyName == "ReturnUrl");
        });

    [Fact]
    public async Task Should_NotBeValid_When_WithInvalidToken() =>
        await MediatorTest(async (mediator, services) =>
        {
            // Arrange
            var userManager = services.GetRequiredService<UserManager<DynamoDbUser>>();
            var user = new DynamoDbUser
            {
                Email = "test@gomsle.com",
                UserName = "test@gomsle.com",
                EmailConfirmed = false,
            };
            await userManager.CreateAsync(user);
            var command = new ConfirmEmailCommand.Command
            {
                UserId = user.Id,
                Token = Guid.NewGuid().ToString(),
                ReturnUrl = "https://gomsle.com",
            };

            // Act
            var response = await mediator.Send(command);

            // Assert
            Assert.False(response.IsValid);
            Assert.Contains(response.Errors, error =>
                error.ErrorCode == "InvalidToken");
        });
}