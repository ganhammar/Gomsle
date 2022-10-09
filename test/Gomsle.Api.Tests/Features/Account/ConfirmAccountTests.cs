using AspNetCore.Identity.AmazonDynamoDB;
using Gomsle.Api.Features.Account;
using Gomsle.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Gomsle.Api.Tests.Features.Account;

[Collection("Sequential")]
public class ConfirmAccountTests : TestBase
{
    [Fact]
    public async Task Should_ConfirmAccount_When_CommandIsValid() =>
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
            var command = new ConfirmAccount.Command
            {
                UserId = user.Id,
                Token = token,
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
            var command = new ConfirmAccount.Command
            {
                Token = token,
            };
            var validator = new ConfirmAccount.CommandValidator(userManager);

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
            var command = new ConfirmAccount.Command
            {
                UserId = Guid.NewGuid().ToString(),
                Token = token,
            };
            var validator = new ConfirmAccount.CommandValidator(userManager);

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
            var command = new ConfirmAccount.Command
            {
                UserId = user.Id,
            };
            var validator = new ConfirmAccount.CommandValidator(userManager);

            // Act
            var response = await validator.ValidateAsync(command);

            // Assert
            Assert.False(response.IsValid);
            Assert.Contains(response.Errors, error =>
                error.ErrorCode == "NotEmptyValidator" && error.PropertyName == "Token");
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
            var command = new ConfirmAccount.Command
            {
                UserId = user.Id,
                Token = Guid.NewGuid().ToString(),
            };

            // Act
            var response = await mediator.Send(command);

            // Assert
            Assert.False(response.IsValid);
            Assert.Contains(response.Errors, error =>
                error.ErrorCode == "InvalidToken");
        });
}