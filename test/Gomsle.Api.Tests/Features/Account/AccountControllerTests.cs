using AspNetCore.Identity.AmazonDynamoDB;
using Gomsle.Api.Features.Account;
using Gomsle.Api.Features.Email;
using Gomsle.Api.Infrastructure;
using Gomsle.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Gomsle.Api.Tests.Features.Account;

public class AccountControllerTests : TestBase
{
    private Func<IServiceProvider, object[]> ConfigureController = (services) =>
    {
        var userManager = services.GetRequiredService<UserManager<DynamoDbUser>>();
        var signInManager = services.GetRequiredService<SignInManager<DynamoDbUser>>();
        var emailSender = services.GetRequiredService<IEmailSender>();

        return new object[] { userManager, signInManager, emailSender };
    };

    [Fact]
    public async Task Should_RegisterUser_When_RequestIsValid() => await ControllerTest<AccountController>(
        // Arrange
        ConfigureController,
        // Act & Assert
        async (controller) =>
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

            var response = okResult!.Value as IResponse<DynamoDbUser>;

            Assert.NotNull(response);
            Assert.True(response!.IsValid);
            Assert.Empty(response.Errors);
            Assert.Equal(email, response!.Result!.Email);
        });

    [Fact]
    public async Task Should_ReturnBadRequest_When_UserAlreadyExists() => await ControllerTest<AccountController>(
        // Arrange
        ConfigureController,
        // Act & Assert
        async (controller) =>
        {
            var email = "test@gomsle.com";

            var userManager = controller.HttpContext.RequestServices
                .GetRequiredService<UserManager<DynamoDbUser>>();
            var createResult = await userManager.CreateAsync(new()
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

            var response = badRequestResult!.Value as IResponse;

            Assert.NotNull(response);
            Assert.False(response!.IsValid);
            Assert.NotEmpty(response.Errors);
            Assert.Contains(response.Errors, error => error.Code == "DuplicateEmail");
        });
}