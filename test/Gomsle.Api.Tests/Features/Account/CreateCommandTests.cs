using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Gomsle.Api.Features.Account;
using Gomsle.Api.Infrastructure.Extensions;
using Gomsle.Api.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Gomsle.Api.Tests.Features.Account;

[Collection("Sequential")]
public class CreateCommandTests : TestBase
{
    [Fact]
    public async Task Should_CreateAccount_When_RequestIsValid() =>
        await MediatorTest(async (mediator, services) =>
        {
            // Arrange
            var user = await CreateAndLoginValidUser(services);
            var command = new CreateCommand.Command
            {
                Name = "Microsoft",
            };

            // Act
            var response = await mediator.Send(command);

            // Assert
            Assert.True(response.IsValid);
            Assert.Equal(command.Name.UrlFriendly(), response.Result!.NormalizedName);
            Assert.Contains(response.Result.Members, x => x.Key == user.Id);
        });

    [Fact]
    public async Task Should_NotBeValid_When_UserIsntAuthenticated() =>
        await MediatorTest(async (mediator, services) =>
        {
            // Arrange
            var command = new CreateCommand.Command
            {
                Name = "Microsoft",
            };

            // Act
            var response = await mediator.Send(command);

            // Assert
            Assert.False(response.IsValid);
            Assert.Contains(
                response.Errors,
                (error) => error.ErrorCode == "NotAuthorized");
        });

    [Fact]
    public async Task Should_NotBeValid_When_NameIsNotSet() =>
        await MediatorTest(async (mediator, services) =>
        {
            // Arrange
            await CreateAndLoginValidUser(services);
            var command = new CreateCommand.Command
            {
                Name = default!,
            };

            // Act
            var response = await mediator.Send(command);

            // Assert
            Assert.False(response.IsValid);
            Assert.Contains(
                response.Errors,
                (error) => error.ErrorCode == "NotEmptyValidator");
        });

    [Fact]
    public async Task Should_NotBeValid_When_NameIsTaken() =>
        await MediatorTest(async (mediator, services) =>
        {
            // Arrange
            await CreateAndLoginValidUser(services);
            var database = services.GetRequiredService<IAmazonDynamoDB>();
            var context = new DynamoDBContext(database);
            var name = "Microsoft";
            await context.SaveAsync(new AccountModel
            {
                Name = name,
                NormalizedName = name.UrlFriendly(),
            });
            var command = new CreateCommand.Command
            {
                Name = name,
            };

            // Act
            var response = await mediator.Send(command);

            // Assert
            Assert.False(response.IsValid);
            Assert.Contains(
                response.Errors,
                (error) => error.ErrorCode == "NameNotUnique");
        });
}