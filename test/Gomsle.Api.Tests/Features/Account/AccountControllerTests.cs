using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using AspNetCore.Identity.AmazonDynamoDB;
using Gomsle.Api.Features.Account;
using Gomsle.Api.Infrastructure;
using Gomsle.Api.Tests.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
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
    public async Task Should_CreateAccount_When_RequestIsValid() => await ControllerTest<AccountController>(
        // Arrange
        ConfigureController,
        // Act & Assert
        async (controller, services) =>
        {
            // Act
            await CreateAndLoginValidUser(services);
            var result = await controller.Create(new()
            {
                Name = "Microsoft",
            });

            // Assert
            Assert.NotNull(result);

            var okResult = result as OkObjectResult;
            Assert.NotNull(okResult);

            var response = okResult!.Value as AccountModel;
            Assert.NotNull(response);
        });

    [Fact]
    public async Task Should_ReturnOkResponse_When_InviteRequestIsValid() => await ControllerTest<AccountController>(
        // Arrange
        ConfigureController,
        // Act & Assert
        async (controller, services) =>
        {
            // Arrange
            var mediator = services.GetRequiredService<IMediator>();
            var database = services.GetRequiredService<IAmazonDynamoDB>();
            var context = new DynamoDBContext(database);

            // Create account
            var accountName = "Microsoft";
            var accountId = Guid.NewGuid().ToString();
            await context.SaveAsync(new AccountModel
            {
                Id = accountId,
                Name = accountName,
                NormalizedName = accountName.UrlFriendly(),
                Members = new Dictionary<string, AccountRole>(),
            });

            // Act
            var result = await controller.Invite(new()
            {
                AccountId = accountId,
                Email = "test@gomsle.com",
                Role = AccountRole.Administrator,
                InvitationUrl = "https://gomsle.com/microsoft/invite",
                SuccessUrl = "https://gomsle.com/microsoft/login",
            });

            // Assert
            Assert.NotNull(result);

            var noContentResult = result as NoContentResult;
            Assert.NotNull(noContentResult);
        });

    [Fact]
    public async Task Should_AcceptInvitation_When_RequestIsValid() => await ControllerTest<AccountController>(
        // Arrange
        ConfigureController,
        // Act & Assert
        async (controller, services) =>
        {
            // Arrange
            var mediator = services.GetRequiredService<IMediator>();
            var database = services.GetRequiredService<IAmazonDynamoDB>();
            var context = new DynamoDBContext(database);

            // Create account
            var accountName = "Microsoft";
            var accountId = Guid.NewGuid().ToString();
            await context.SaveAsync(new AccountModel
            {
                Id = accountId,
                Name = accountName,
                NormalizedName = accountName.UrlFriendly(),
                Members = new Dictionary<string, AccountRole>(),
            });

            // Create invitation
            var model = new AccountInvitationModel
            {
                AccountId = accountId,
                Email = "test@gomsle.com",
                Role = AccountRole.Administrator,
                SuccessUrl = "https://gomsle.com/microsoft/login",
            };
            await context.SaveAsync(model);
            
            // Act
            var result = await controller.AcceptInvitation(new()
            {
                Token = model.Id,
                Password = "itsaseasyas123",
            });

            // Assert
            Assert.NotNull(result);

            var okResult = result as OkObjectResult;
            Assert.NotNull(okResult);

            var response = okResult!.Value as DynamoDbUser;
            Assert.NotNull(response);
        });
}