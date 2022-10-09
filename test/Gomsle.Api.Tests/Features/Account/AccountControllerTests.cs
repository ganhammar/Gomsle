using AspNetCore.Identity.AmazonDynamoDB;
using FluentValidation.Results;
using Gomsle.Api.Features.Account;
using Gomsle.Api.Features.Email;
using Gomsle.Api.Tests.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Gomsle.Api.Tests.Features.Account;

[Collection("Sequential")]
public class AccountControllerTests : TestBase
{
    private Func<IServiceProvider, object[]> ConfigureController = (services) =>
    {
        var mediator = services.GetRequiredService<IMediator>();

        return new object[] { mediator };
    };

    [Fact]
    public async Task Should_RegisterUser_When_RequestIsValid() => await ControllerTest<AccountController>(
        // Arrange
        ConfigureController,
        // Act & Assert
        async (controller, services) =>
        {
            var email = "test@gomsle.com";
            var result = await controller.Register(new()
            {
                Email = email,
                Password = "itsaseasyas123",
                ReturnUrl = "http://gomsle.com",
            });

            Assert.NotNull(result);

            var okResult = result as OkObjectResult;

            Assert.NotNull(okResult);

            var response = okResult!.Value as DynamoDbUser;

            Assert.NotNull(response);
            Assert.Equal(email, response!.Email);

            var mock = GetMock<IEmailSender>();
            mock!.Verify(x => 
                x.Send(email, It.IsAny<string>(), It.IsAny<string>()),
                Times.Once());
        });

    [Fact]
    public async Task Should_SendEmail_When_Registering() => await ControllerTest<AccountController>(
        // Arrange
        ConfigureController,
        // Act & Assert
        async (controller, services) =>
        {
            var email = "test@gomsle.com";
            var result = await controller.Register(new()
            {
                Email = email,
                Password = "itsaseasyas123",
                ReturnUrl = "http://gomsle.com",
            }) as OkObjectResult;

            var response = result!.Value as DynamoDbUser;

            var mock = GetMock<IEmailSender>();
            mock!.Verify(x => 
                x.Send(email, It.IsAny<string>(), It.IsAny<string>()),
                Times.Once());
        });

    [Fact]
    public async Task Should_ReturnBadRequest_When_UserAlreadyExists() => await ControllerTest<AccountController>(
        // Arrange
        ConfigureController,
        // Act & Assert
        async (controller, services) =>
        {
            var email = "test@gomsle.com";

            var userManager = services.GetRequiredService<UserManager<DynamoDbUser>>();
            await userManager.CreateAsync(new()
            {
                Email = email,
                UserName = email,
            });

            var result = await controller.Register(new()
            {
                Email = email,
                Password = "itsaseasyas123",
                ReturnUrl = "http://gomsle.com",
            });

            Assert.NotNull(result);

            var badRequestResult = result as BadRequestObjectResult;

            Assert.NotNull(badRequestResult);

            var errors = badRequestResult!.Value as IEnumerable<ValidationFailure>;

            Assert.NotNull(errors);
            Assert.Contains(errors, error => error.ErrorCode == "DuplicateEmail");
        });

    [Fact]
    public async Task Should_ReturnBadRequest_When_EmailIsNotSet() => await ControllerTest<AccountController>(
        // Arrange
        ConfigureController,
        // Act & Assert
        async (controller, services) =>
        {
            var result = await controller.Register(new()
            {
                Email = string.Empty,
                Password = "itsaseasyas123",
                ReturnUrl = "http://gomsle.com",
            });

            Assert.NotNull(result);

            var badRequestResult = result as BadRequestObjectResult;

            Assert.NotNull(badRequestResult);

            var errors = badRequestResult!.Value as IEnumerable<ValidationFailure>;

            Assert.NotEmpty(errors);
            Assert.Contains(errors, error =>
                error.ErrorCode == "NotEmptyValidator" && error.PropertyName == "Email");
        });

    [Fact]
    public async Task Should_ConfirmAccount_When_RequestIsValid() => await ControllerTest<AccountController>(
        // Arrange
        ConfigureController,
        // Act & Assert
        async (controller, services) =>
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
            var url = "https://gomsle.com/my-client-app";

            // Act
            var result = await controller.Confirm(user.Id, token, url);

            // Assert
            Assert.NotNull(result);

            var redirectResult = result as RedirectResult;

            Assert.NotNull(redirectResult);
            Assert.Equal(url, redirectResult!.Url);
        });

    [Fact]
    public async Task Should_ReturnForbidden_When_ConfirmRequestIsInvalid() => await ControllerTest<AccountController>(
        // Arrange
        ConfigureController,
        // Act & Assert
        async (controller, services) =>
        {
            // Act
            var result = await controller.Confirm(string.Empty, string.Empty, string.Empty);

            // Assert
            Assert.NotNull(result);

            var forbidResult = result as ForbidResult;

            Assert.NotNull(forbidResult);
        });
}