using Gomsle.Api.Features.Account;
using Gomsle.Api.Infrastructure.Validators;
using Gomsle.Api.Tests.Infrastructure;
using MediatR;
using Xunit;
using CreateCommand = Gomsle.Api.Features.OidcProvider.CreateCommand;
using EditCommand = Gomsle.Api.Features.OidcProvider.EditCommand;

namespace Gomsle.Api.Tests.Features.OidcProvider;

[Collection("Sequential")]
public class EditCommandTests : TestBase
{
    [Fact]
    public async Task Should_EditProvider_When_RequestIsValid() =>
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
            var validator = new EditCommand.CommandValidator(services);

            // Act
            var response = await validator.ValidateAsync(command);

            // Assert
            Assert.False(response.IsValid);
            Assert.Contains(response.Errors, error => error.PropertyName == nameof(EditCommand.Command.Id)
                && error.ErrorCode == "NotEmptyValidator");
        });

    [Fact]
    public async Task Should_NotBeValid_When_ProviderDoesntExist() =>
        await MediatorTest(async (mediator, services) =>
        {
            // Arrange
            var command = await Prepare(services, mediator);
            command.Id = Guid.NewGuid().ToString();
            var validator = new EditCommand.CommandValidator(services);

            // Act
            var response = await validator.ValidateAsync(command);

            // Assert
            Assert.False(response.IsValid);
            Assert.Contains(response.Errors, error => error.PropertyName == nameof(EditCommand.Command.Id)
                && error.ErrorCode == nameof(ErrorCodes.NotAuthorized));
        });

    [Fact]
    public async Task Should_NotBeValid_When_ClientIdIsNotSet() =>
        await MediatorTest(async (mediator, services) =>
        {
            // Arrange
            var command = await Prepare(services, mediator);
            command.ClientId = default;
            var validator = new EditCommand.CommandValidator(services);

            // Act
            var response = await validator.ValidateAsync(command);

            // Assert
            Assert.False(response.IsValid);
            Assert.Contains(response.Errors, error => error.PropertyName == nameof(EditCommand.Command.ClientId)
                && error.ErrorCode == "NotEmptyValidator");
        });

    [Fact]
    public async Task Should_NotBeValid_When_NameIsNotSet() =>
        await MediatorTest(async (mediator, services) =>
        {
            // Arrange
            var command = await Prepare(services, mediator);
            command.Name = default;
            var validator = new EditCommand.CommandValidator(services);

            // Act
            var response = await validator.ValidateAsync(command);

            // Assert
            Assert.False(response.IsValid);
            Assert.Contains(response.Errors, error => error.PropertyName == nameof(EditCommand.Command.Name)
                && error.ErrorCode == "NotEmptyValidator");
        });

    [Fact]
    public async Task Should_NotBeValid_When_AuthorityUrlIsNotSet() =>
        await MediatorTest(async (mediator, services) =>
        {
            // Arrange
            var command = await Prepare(services, mediator);
            command.AuthorityUrl = default;
            var validator = new EditCommand.CommandValidator(services);

            // Act
            var response = await validator.ValidateAsync(command);

            // Assert
            Assert.False(response.IsValid);
            Assert.Contains(response.Errors, error => error.PropertyName == nameof(EditCommand.Command.AuthorityUrl)
                && error.ErrorCode == "NotEmptyValidator");
        });

    [Fact]
    public async Task Should_NotBeValid_When_AuthorityUrlIsNotAUri() =>
        await MediatorTest(async (mediator, services) =>
        {
            // Arrange
            var command = await Prepare(services, mediator);
            command.AuthorityUrl = "not-a-valid-uri";
            var validator = new EditCommand.CommandValidator(services);

            // Act
            var response = await validator.ValidateAsync(command);

            // Assert
            Assert.False(response.IsValid);
            Assert.Contains(response.Errors, error => error.PropertyName == nameof(EditCommand.Command.AuthorityUrl)
                && error.ErrorCode == nameof(ErrorCodes.InvalidUri));
        });

    [Fact]
    public async Task Should_NotBeValid_When_ResponseTypeIsNotSet() =>
        await MediatorTest(async (mediator, services) =>
        {
            // Arrange
            var command = await Prepare(services, mediator);
            command.ResponseType = default;
            var validator = new EditCommand.CommandValidator(services);

            // Act
            var response = await validator.ValidateAsync(command);

            // Assert
            Assert.False(response.IsValid);
            Assert.Contains(response.Errors, error => error.PropertyName == nameof(EditCommand.Command.ResponseType)
                && error.ErrorCode == "NotEmptyValidator");
        });

    [Fact]
    public async Task Should_NotBeValid_When_ResponseTypeIsNotValidValue() =>
        await MediatorTest(async (mediator, services) =>
        {
            // Arrange
            var command = await Prepare(services, mediator);
            command.ResponseType = "not-in-the-spec";
            var validator = new EditCommand.CommandValidator(services);

            // Act
            var response = await validator.ValidateAsync(command);

            // Assert
            Assert.False(response.IsValid);
            Assert.Contains(response.Errors, error => error.PropertyName == nameof(EditCommand.Command.ResponseType)
                && error.ErrorCode == nameof(ErrorCodes.ResponseTypeIsInvalid));
        });

    [Fact]
    public async Task Should_NotBeValid_When_IsDefaultIsNotSet() =>
        await MediatorTest(async (mediator, services) =>
        {
            // Arrange
            var command = await Prepare(services, mediator);
            command.IsDefault = default;
            var validator = new EditCommand.CommandValidator(services);

            // Act
            var response = await validator.ValidateAsync(command);

            // Assert
            Assert.False(response.IsValid);
            Assert.Contains(response.Errors, error => error.PropertyName == nameof(EditCommand.Command.IsDefault)
                && error.ErrorCode == "NotEmptyValidator");
        });

    [Fact]
    public async Task Should_NotBeValid_When_IsVisibleIsNotSet() =>
        await MediatorTest(async (mediator, services) =>
        {
            // Arrange
            var command = await Prepare(services, mediator);
            command.IsVisible = default;
            var validator = new EditCommand.CommandValidator(services);

            // Act
            var response = await validator.ValidateAsync(command);

            // Assert
            Assert.False(response.IsValid);
            Assert.Contains(response.Errors, error => error.PropertyName == nameof(EditCommand.Command.IsVisible)
                && error.ErrorCode == "NotEmptyValidator");
        });

    private async Task<string> CreateOidcProvider(IMediator mediator, string accountId)
    {
        var result = await mediator.Send(new CreateCommand.Command
        {
            AccountId = accountId,
            AuthorityUrl = "https://microsoft.com",
            ClientId = "microsoft-internal-azure-id-client",
            ClientSecret = "microsoft-internal-azure-id-client.secret",
            IsDefault = false,
            IsVisible = true,
            Name = "Micrsoft Internal",
            ResponseType = "code",
            Scopes = new() { "email", "profile" },
        });

        return result.Result!.Id;
    }

    private EditCommand.Command GetValidCommand(string id)
        => new EditCommand.Command
        {
            Id = id,
            AuthorityUrl = "https://microsoft.com",
            ClientId = "microsoft-internal-azure-id-client",
            ClientSecret = "microsoft-internal-azure-id-client.secret",
            IsDefault = false,
            IsVisible = true,
            Name = "Micrsoft Internal",
            ResponseType = "code",
            Scopes = new() { "email", "profile" },
        };

    private async Task<EditCommand.Command> Prepare(
        IServiceProvider services, IMediator mediator)
    {
        var user = await CreateAndLoginValidUser(services);
        var account = await CreateAccount(services, new()
        {
            { user.Id, AccountRole.Owner },
        });
        var oidcProviderId = await CreateOidcProvider(mediator, account.Id);
        return GetValidCommand(oidcProviderId);
    }
}