using Gomsle.Api.Features.Account;
using Gomsle.Api.Infrastructure.Validators;
using Gomsle.Api.Tests.Infrastructure;
using MediatR;
using Xunit;
using CreateCommand = Gomsle.Api.Features.Application.Oidc.CreateCommand;

namespace Gomsle.Api.Tests.Features.Application.Oidc;

[Collection("Sequential")]
public class CreateCommandTests : TestBase
{
    private async Task<string> CreateApplication(IMediator mediator, string accountId)
    {
        var result = await mediator.Send(new Gomsle.Api.Features.Application.CreateCommand.Command
        {
            AccountId = accountId,
            AutoProvision = true,
            EnableProvision = true,
            DisplayName = "Microsoft Azure AD Application",
        });

        return result.Result!.Id!;
    }

    private CreateCommand.Command GetValidCommand(string applicationId)
        => new CreateCommand.Command
        {
            ApplicationId = applicationId,
            AuthorityUrl = "https://microsoft.com",
            ClientId = "microsoft-internal-azure-id-client",
            ClientSecret = "microsoft-internal-azure-id-client.secret",
            IsDefault = false,
            IsVisible = true,
            Name = "Micrsoft Internal",
            ResponseType = "code",
            Scopes = new() { "email", "profile" },
        };

    private async Task<CreateCommand.Command> Prepare(
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

    [Fact]
    public async Task Should_CreateApplication_When_RequestIsValid() =>
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
    public async Task Should_NotBeValid_When_ApplicationIdIsNotSet() =>
        await MediatorTest(async (mediator, services) =>
        {
            // Arrange
            var command = await Prepare(services, mediator);
            command.ApplicationId = default;
            var validator = new CreateCommand.CommandValidator(services);

            // Act
            var response = await validator.ValidateAsync(command);

            // Assert
            Assert.False(response.IsValid);
            Assert.Contains(response.Errors, error => error.PropertyName == nameof(CreateCommand.Command.ApplicationId)
                && error.ErrorCode == "NotEmptyValidator");
        });

    [Fact]
    public async Task Should_NotBeValid_When_ApplicationDoesntExist() =>
        await MediatorTest(async (mediator, services) =>
        {
            // Arrange
            var command = await Prepare(services, mediator);
            command.ApplicationId = Guid.NewGuid().ToString();
            var validator = new CreateCommand.CommandValidator(services);

            // Act
            var response = await validator.ValidateAsync(command);

            // Assert
            Assert.False(response.IsValid);
            Assert.Contains(response.Errors, error => error.PropertyName == nameof(CreateCommand.Command.ApplicationId)
                && error.ErrorCode == nameof(ErrorCodes.MisingRoleForAccount));
        });

    [Fact]
    public async Task Should_NotBeValid_When_ClientIdIsNotSet() =>
        await MediatorTest(async (mediator, services) =>
        {
            // Arrange
            var command = await Prepare(services, mediator);
            command.ClientId = default;
            var validator = new CreateCommand.CommandValidator(services);

            // Act
            var response = await validator.ValidateAsync(command);

            // Assert
            Assert.False(response.IsValid);
            Assert.Contains(response.Errors, error => error.PropertyName == nameof(CreateCommand.Command.ClientId)
                && error.ErrorCode == "NotEmptyValidator");
        });

    [Fact]
    public async Task Should_NotBeValid_When_ClientSecretIsNotSet() =>
        await MediatorTest(async (mediator, services) =>
        {
            // Arrange
            var command = await Prepare(services, mediator);
            command.ClientSecret = default;
            var validator = new CreateCommand.CommandValidator(services);

            // Act
            var response = await validator.ValidateAsync(command);

            // Assert
            Assert.False(response.IsValid);
            Assert.Contains(response.Errors, error => error.PropertyName == nameof(CreateCommand.Command.ClientSecret)
                && error.ErrorCode == "NotEmptyValidator");
        });

    [Fact]
    public async Task Should_NotBeValid_When_NameIsNotSet() =>
        await MediatorTest(async (mediator, services) =>
        {
            // Arrange
            var command = await Prepare(services, mediator);
            command.Name = default;
            var validator = new CreateCommand.CommandValidator(services);

            // Act
            var response = await validator.ValidateAsync(command);

            // Assert
            Assert.False(response.IsValid);
            Assert.Contains(response.Errors, error => error.PropertyName == nameof(CreateCommand.Command.Name)
                && error.ErrorCode == "NotEmptyValidator");
        });

    [Fact]
    public async Task Should_NotBeValid_When_AuthorityUrlIsNotSet() =>
        await MediatorTest(async (mediator, services) =>
        {
            // Arrange
            var command = await Prepare(services, mediator);
            command.AuthorityUrl = default;
            var validator = new CreateCommand.CommandValidator(services);

            // Act
            var response = await validator.ValidateAsync(command);

            // Assert
            Assert.False(response.IsValid);
            Assert.Contains(response.Errors, error => error.PropertyName == nameof(CreateCommand.Command.AuthorityUrl)
                && error.ErrorCode == "NotEmptyValidator");
        });

    [Fact]
    public async Task Should_NotBeValid_When_AuthorityUrlIsNotAUri() =>
        await MediatorTest(async (mediator, services) =>
        {
            // Arrange
            var command = await Prepare(services, mediator);
            command.AuthorityUrl = "not-a-valid-uri";
            var validator = new CreateCommand.CommandValidator(services);

            // Act
            var response = await validator.ValidateAsync(command);

            // Assert
            Assert.False(response.IsValid);
            Assert.Contains(response.Errors, error => error.PropertyName == nameof(CreateCommand.Command.AuthorityUrl)
                && error.ErrorCode == nameof(ErrorCodes.InvalidUri));
        });

    [Fact]
    public async Task Should_NotBeValid_When_ResponseTypeIsNotSet() =>
        await MediatorTest(async (mediator, services) =>
        {
            // Arrange
            var command = await Prepare(services, mediator);
            command.ResponseType = default;
            var validator = new CreateCommand.CommandValidator(services);

            // Act
            var response = await validator.ValidateAsync(command);

            // Assert
            Assert.False(response.IsValid);
            Assert.Contains(response.Errors, error => error.PropertyName == nameof(CreateCommand.Command.ResponseType)
                && error.ErrorCode == "NotEmptyValidator");
        });

    [Fact]
    public async Task Should_NotBeValid_When_ResponseTypeIsNotValidValue() =>
        await MediatorTest(async (mediator, services) =>
        {
            // Arrange
            var command = await Prepare(services, mediator);
            command.ResponseType = "not-in-the-spec";
            var validator = new CreateCommand.CommandValidator(services);

            // Act
            var response = await validator.ValidateAsync(command);

            // Assert
            Assert.False(response.IsValid);
            Assert.Contains(response.Errors, error => error.PropertyName == nameof(CreateCommand.Command.ResponseType)
                && error.ErrorCode == nameof(ErrorCodes.ResponseTypeIsInvalid));
        });

    [Fact]
    public async Task Should_NotBeValid_When_IsDefaultIsNotSet() =>
        await MediatorTest(async (mediator, services) =>
        {
            // Arrange
            var command = await Prepare(services, mediator);
            command.IsDefault = default;
            var validator = new CreateCommand.CommandValidator(services);

            // Act
            var response = await validator.ValidateAsync(command);

            // Assert
            Assert.False(response.IsValid);
            Assert.Contains(response.Errors, error => error.PropertyName == nameof(CreateCommand.Command.IsDefault)
                && error.ErrorCode == "NotEmptyValidator");
        });

    [Fact]
    public async Task Should_NotBeValid_When_IsVisibleIsNotSet() =>
        await MediatorTest(async (mediator, services) =>
        {
            // Arrange
            var command = await Prepare(services, mediator);
            command.IsVisible = default;
            var validator = new CreateCommand.CommandValidator(services);

            // Act
            var response = await validator.ValidateAsync(command);

            // Assert
            Assert.False(response.IsValid);
            Assert.Contains(response.Errors, error => error.PropertyName == nameof(CreateCommand.Command.IsVisible)
                && error.ErrorCode == "NotEmptyValidator");
        });
}