using AspNetCore.Identity.AmazonDynamoDB;
using Gomsle.Api.Features.User;
using Gomsle.Api.Features.Email;
using Gomsle.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Gomsle.Api.Tests.Features.User;

[Collection("Sequential")]
public class SendCodeCommandTests : TestBase
{
    [Fact]
    public async Task Should_SendEmail_When_CommandIsValid() =>
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
            var command = new SendCodeCommand.Command
            {
                Provider = "Email",
            };

            // Act
            var result = await mediator.Send(command);

            // Assert
            Assert.True(result.IsValid);

            var mock = GetMock<IEmailSender>();
            mock!.Verify(x => 
                x.Send(email, It.IsAny<string>(), It.IsAny<string>()),
                Times.Once());
        });

    [Fact]
    public async Task Should_NotBeValid_When_ProviderIsNotSet() =>
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
            var command = new SendCodeCommand.Command();

            // Act
            var response = await mediator.Send(command);

            // Assert
            Assert.False(response.IsValid);
            Assert.Contains(response.Errors, error =>
                error.ErrorCode == "NotEmptyValidator" && error.PropertyName == "Provider");
        });

    [Fact]
    public async Task Should_NotBeValid_When_NoLoginAttempIsInProgress() =>
        await MediatorTest(async (mediator, services) =>
        {
            // Arrange
            var userManager = services.GetRequiredService<UserManager<DynamoDbUser>>();
            var command = new SendCodeCommand.Command
            {
                Provider = "Email",
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
            var command = new SendCodeCommand.Command
            {
                Provider = "Phone",
            };

            // Act
            var response = await mediator.Send(command);

            // Assert
            Assert.False(response.IsValid);
            Assert.Contains(response.Errors, error =>
                error.ErrorCode == "TwoFactorProviderNotValid" && error.PropertyName == "Provider");
        });
}