using Gomsle.Api.Features.Account;
using Gomsle.Api.Features.Application;
using Gomsle.Api.Infrastructure.Validators;
using Gomsle.Api.Tests.Infrastructure;
using MediatR;
using Xunit;

namespace Gomsle.Api.Tests.Features.Application;

[Collection("Sequential")]
public class DeleteCommandTests : TestBase
{
    [Fact]
    public async Task Should_DeleteProvider_When_RequestIsValid() =>
        await MediatorTest(async (mediator, services) =>
        {
            // Arrange
            var command = await Prepare(services, mediator);

            // Act
            var response = await mediator.Send(command);

            // Assert
            Assert.True(response.IsValid);
        });

    [Fact]
    public async Task Should_NotBeValid_When_IdIsNotSet() =>
        await MediatorTest(async (mediator, services) =>
        {
            // Arrange
            var command = await Prepare(services, mediator);
            command.Id = default;
            var validator = new DeleteCommand.CommandValidator(services);

            // Act
            var response = await validator.ValidateAsync(command);

            // Assert
            Assert.False(response.IsValid);
            Assert.Contains(response.Errors, error => error.PropertyName == nameof(DeleteCommand.Command.Id)
                && error.ErrorCode == "NotEmptyValidator");
        });

    [Fact]
    public async Task Should_NotBeValid_When_ProviderDoesntExist() =>
        await MediatorTest(async (mediator, services) =>
        {
            // Arrange
            var command = await Prepare(services, mediator);
            command.Id = Guid.NewGuid().ToString();
            var validator = new DeleteCommand.CommandValidator(services);

            // Act
            var response = await validator.ValidateAsync(command);

            // Assert
            Assert.False(response.IsValid);
            Assert.Contains(response.Errors, error => error.PropertyName == nameof(DeleteCommand.Command.Id)
                && error.ErrorCode == nameof(ErrorCodes.NotAuthorized));
        });

    private async Task<string> CreateApplication(IMediator mediator, string accountId)
    {
        var result = await mediator.Send(new Gomsle.Api.Features.Application.CreateCommand.Command
        {
            AccountId = accountId,
            AutoProvision = true,
            EnableProvision = true,
            DefaultOrigin = "https://microsoft.com",
            DisplayName = "Microsoft Azure AD Application",
            Origins = new()
            {
                "https://microsoft.se",
            },
            RedirectUris = new()
            {
                "https://microsoft.com/login/callback",
                "https://microsoft.se/login/callback",
            },
            PostLogoutRedirectUris = new()
            {
                "https://microsoft.com/logout/callback",
                "https://microsoft.se/logout/callback",
            },
        });

        return result.Result!.Id!;
    }

    private DeleteCommand.Command GetValidCommand(string id)
        => new DeleteCommand.Command
        {
            Id = id,
        };

    private async Task<DeleteCommand.Command> Prepare(
        IServiceProvider services, IMediator mediator)
    {
        var user = await CreateAndLoginValidUser(services);
        var account = await CreateAccount(services, new()
        {
            { user.Id, AccountRole.Owner },
        });
        var applicationId = await CreateApplication(mediator, account.Id);
        return GetValidCommand(applicationId);
    }
}