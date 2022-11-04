using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using AspNetCore.Identity.AmazonDynamoDB;
using Gomsle.Api.Features.Account;
using Gomsle.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Gomsle.Api.Tests.Features.Account;

[Collection("Sequential")]
public class AcceptInvitationCommandTests : TestBase
{
    private async Task<AccountInvitationModel> CreateInvitation(
        string email, string accountName, IServiceProvider services)
    {
        var database = services.GetRequiredService<IAmazonDynamoDB>();
        var context = new DynamoDBContext(database);
        var model = new AccountInvitationModel
        {
            NormalizedAccountName = accountName,
            Email = email,
            Role = AccountRole.Administrator,
            SuccessUrl = "https://gomsle.com/microsoft/login",
        };
        await context.SaveAsync(model);

        return model;
    }

    [Fact]
    public async Task Should_AddUserAsMember_When_RequestIsValid() =>
        await MediatorTest(async (mediator, services) =>
        {
            // Arrange
            var user = await CreateAndLoginValidUser(services);
            var accountName = "microsoft";
            await mediator.Send(new CreateCommand.Command
            {
                Name = accountName,
            });
            var model = await CreateInvitation("test@gomsle.com", accountName, services);
            var command = new AcceptInvitationCommand.Command
            {
                Token = model.Id,
                Password = "itsaseasyas123",
            };

            // Act
            var response = await mediator.Send(command);

            // Assert
            Assert.True(response.IsValid);
        });

    [Fact]
    public async Task Should_NotBeValid_When_TokenIsNotSet() =>
        await MediatorTest(async (mediator, services) =>
        {
            // Arrange
            var command = new AcceptInvitationCommand.Command
            {
                Password = "itsaseasyas123",
            };
            var database = services.GetRequiredService<IAmazonDynamoDB>();
            var userManager = services.GetRequiredService<UserManager<DynamoDbUser>>();
            var validator = new AcceptInvitationCommand.CommandValidator(database, userManager);

            // Act
            var response = await validator.ValidateAsync(command);

            // Assert
            Assert.False(response.IsValid);
            Assert.Contains(response.Errors, x => x.ErrorCode == "NotEmptyValidator" &&
                x.PropertyName == nameof(AcceptInvitationCommand.Command.Token));
        });

    [Fact]
    public async Task Should_NotBeValid_When_PasswordIsNotSet() =>
        await MediatorTest(async (mediator, services) =>
        {
            // Arrange
            var model = await CreateInvitation("test@gomsle.com", "Microsoft", services);
            var command = new AcceptInvitationCommand.Command
            {
                Token = model.Id,
            };
            var database = services.GetRequiredService<IAmazonDynamoDB>();
            var userManager = services.GetRequiredService<UserManager<DynamoDbUser>>();
            var validator = new AcceptInvitationCommand.CommandValidator(database, userManager);

            // Act
            var response = await validator.ValidateAsync(command);

            // Assert
            Assert.False(response.IsValid);
            Assert.Contains(response.Errors, x => x.ErrorCode == "NotEmptyValidator" &&
                x.PropertyName == nameof(AcceptInvitationCommand.Command.Password));
        });

    [Fact]
    public async Task Should_BeValid_When_PasswordIsNotSetButUserHasAccount() =>
        await MediatorTest(async (mediator, services) =>
        {
            // Arrange
            var database = services.GetRequiredService<IAmazonDynamoDB>();
            var userManager = services.GetRequiredService<UserManager<DynamoDbUser>>();
            var accountName = "microsoft";
            var email = "test@gomsle.com";

            // Create account
            await mediator.Send(new CreateCommand.Command
            {
                Name = accountName,
            });

            // Create user
            await userManager.CreateAsync(new()
            {
                Email = email,
            });

            // Create invitation
            var model = await CreateInvitation(email, accountName, services);

            // Command for test
            var command = new AcceptInvitationCommand.Command
            {
                Token = model.Id,
            };
            var validator = new AcceptInvitationCommand.CommandValidator(database, userManager);

            // Act
            var response = await validator.ValidateAsync(command);

            // Assert
            Assert.False(response.IsValid);
            Assert.Contains(response.Errors, x => x.ErrorCode == "NotEmptyValidator" &&
                x.PropertyName == nameof(AcceptInvitationCommand.Command.Password));
        });

    [Fact]
    public async Task Should_NotBeValid_When_TokenDoesntExist() =>
        await MediatorTest(async (mediator, services) =>
        {
            // Arrange
            var command = new AcceptInvitationCommand.Command
            {
                Token = Guid.NewGuid().ToString(),
                Password = "itsaseasyas123",
            };
            var database = services.GetRequiredService<IAmazonDynamoDB>();
            var userManager = services.GetRequiredService<UserManager<DynamoDbUser>>();
            var validator = new AcceptInvitationCommand.CommandValidator(database, userManager);

            // Act
            var response = await validator.ValidateAsync(command);

            // Assert
            Assert.False(response.IsValid);
            Assert.Contains(response.Errors, x => x.ErrorCode == "TokenNotValid" &&
                x.PropertyName == nameof(AcceptInvitationCommand.Command.Token));
        });
}