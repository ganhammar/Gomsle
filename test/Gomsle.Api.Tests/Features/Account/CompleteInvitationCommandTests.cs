using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Gomsle.Api.Features.Account;
using Gomsle.Api.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Gomsle.Api.Tests.Features.Account;

[Collection("Sequential")]
public class CompleteInvitationCommandTests : TestBase
{
    [Fact]
    public async Task Should_AddUserAsMember_When_RequestIsValid() =>
        await MediatorTest(async (mediator, services) =>
        {
            // Arrange
            var database = services.GetRequiredService<IAmazonDynamoDB>();
            var context = new DynamoDBContext(database);
            var user = await CreateAndLoginValidUser(services);
            var accountName = "microsoft";
            await mediator.Send(new CreateCommand.Command
            {
                Name = accountName,
            });
            var model = new AccountInvitationModel
            {
                NormalizedAccountName = accountName,
                Email = "test@gomsle.com",
                Role = AccountRole.Administrator,
                SuccessUrl = "https://gomsle.com/microsoft/login",
            };
            await context.SaveAsync(model);
            var command = new CompleteInvitationCommand.Command
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
            var command = new CompleteInvitationCommand.Command
            {
                Password = "itsaseasyas123",
            };
            var database = services.GetRequiredService<IAmazonDynamoDB>();
            var validator = new CompleteInvitationCommand.CommandValidator(database);

            // Act
            var response = await validator.ValidateAsync(command);

            // Assert
            Assert.False(response.IsValid);
            Assert.Contains(response.Errors, x => x.ErrorCode == "NotEmptyValidator" &&
                x.PropertyName == nameof(CompleteInvitationCommand.Command.Token));
        });

    [Fact]
    public async Task Should_NotBeValid_When_PasswordIsNotSet() =>
        await MediatorTest(async (mediator, services) =>
        {
            // Arrange
            var command = new CompleteInvitationCommand.Command
            {
                Token = Guid.NewGuid().ToString(),
            };
            var database = services.GetRequiredService<IAmazonDynamoDB>();
            var validator = new CompleteInvitationCommand.CommandValidator(database);

            // Act
            var response = await validator.ValidateAsync(command);

            // Assert
            Assert.False(response.IsValid);
            Assert.Contains(response.Errors, x => x.ErrorCode == "NotEmptyValidator" &&
                x.PropertyName == nameof(CompleteInvitationCommand.Command.Password));
        });

    [Fact]
    public async Task Should_NotBeValid_When_TokenDoesntExist() =>
        await MediatorTest(async (mediator, services) =>
        {
            // Arrange
            var command = new CompleteInvitationCommand.Command
            {
                Token = Guid.NewGuid().ToString(),
                Password = "itsaseasyas123",
            };
            var database = services.GetRequiredService<IAmazonDynamoDB>();
            var validator = new CompleteInvitationCommand.CommandValidator(database);

            // Act
            var response = await validator.ValidateAsync(command);

            // Assert
            Assert.False(response.IsValid);
            Assert.Contains(response.Errors, x => x.ErrorCode == "TokenNotValid" &&
                x.PropertyName == nameof(CompleteInvitationCommand.Command.Token));
        });
}