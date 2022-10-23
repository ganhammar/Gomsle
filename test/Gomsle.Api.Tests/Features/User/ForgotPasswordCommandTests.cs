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
public class ForgotPasswordCommandTests : TestBase
{
    [Fact]
    public async Task Should_SendResetEmail_When_CommandIsValid() =>
        await MediatorTest(async (mediator, services) =>
        {
            // Arrange
            var userManager = services.GetRequiredService<UserManager<DynamoDbUser>>();
            var email = "test@gomsle.com";
            var user = new DynamoDbUser
            {
                Email = email,
                UserName = email,
            };
            await userManager.CreateAsync(user);
            var command = new ForgotPasswordCommand.Command
            {
                Email = email,
                ResetUrl = "https://gomsle.com/reset",
            };

            // Act
            var response = await mediator.Send(command);

            // Assert
            Assert.True(response.IsValid);

            var mock = GetMock<IEmailSender>();
            mock!.Verify(x => 
                x.Send(email, It.IsAny<string>(), It.IsAny<string>()),
                Times.Once());
        });

    [Fact]
    public async Task Should_NotSendResetEmail_When_UserDoesntExist() =>
        await MediatorTest(async (mediator, services) =>
        {
            // Arrange
            var userManager = services.GetRequiredService<UserManager<DynamoDbUser>>();
            var email = "test@gomsle.com";
            var command = new ForgotPasswordCommand.Command
            {
                Email = email,
                ResetUrl = "https://gomsle.com/reset",
            };

            // Act
            var response = await mediator.Send(command);

            // Assert
            Assert.True(response.IsValid);

            var mock = GetMock<IEmailSender>();
            mock!.Verify(x => 
                x.Send(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Never());
        });

    [Fact]
    public async Task Should_NotBeValid_When_EmailIsNotSet() =>
        await MediatorTest(async (mediator, services) =>
        {
            // Arrange
            var command = new ForgotPasswordCommand.Command
            {
                ResetUrl = "https://gomsle.com/reset",
            };
            var validator = new ForgotPasswordCommand.CommandValidator();

            // Act
            var response = await validator.ValidateAsync(command);

            // Assert
            Assert.False(response.IsValid);
            Assert.Contains(response.Errors, error =>
                error.ErrorCode == "NotEmptyValidator" && error.PropertyName == "Email");
        });

    [Fact]
    public async Task Should_NotBeValid_When_ResetUrlIsNotSet() =>
        await MediatorTest(async (mediator, services) =>
        {
            // Arrange
            var command = new ForgotPasswordCommand.Command
            {
                Email = "test@gomsle.com",
            };
            var validator = new ForgotPasswordCommand.CommandValidator();

            // Act
            var response = await validator.ValidateAsync(command);

            // Assert
            Assert.False(response.IsValid);
            Assert.Contains(response.Errors, error =>
                error.ErrorCode == "NotEmptyValidator" && error.PropertyName == "ResetUrl");
        });

    [Fact]
    public async Task Should_NotBeValid_When_EmailIsNotEmailAddress() =>
        await MediatorTest(async (mediator, services) =>
        {
            // Arrange
            var command = new ForgotPasswordCommand.Command
            {
                Email = "not-a-email",
                ResetUrl = "https://gomsle.com/reset",
            };
            var validator = new ForgotPasswordCommand.CommandValidator();

            // Act
            var response = await validator.ValidateAsync(command);

            // Assert
            Assert.False(response.IsValid);
            Assert.Contains(response.Errors, error =>
                error.ErrorCode == "EmailValidator" && error.PropertyName == "Email");
        });
}