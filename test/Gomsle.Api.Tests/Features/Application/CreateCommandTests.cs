using Gomsle.Api.Features.Account;
using Gomsle.Api.Infrastructure.Validators;
using Gomsle.Api.Tests.Infrastructure;
using Xunit;
using CreateCommand = Gomsle.Api.Features.Application.CreateCommand;
using CreateOidcProviderCommand = Gomsle.Api.Features.OidcProvider.CreateCommand;

namespace Gomsle.Api.Tests.Features.Application;

[Collection("Sequential")]
public class CreateCommandTests : TestBase
{
    [Fact]
    public async Task Should_CreateApplication_When_RequestIsValid() =>
        await MediatorTest(async (mediator, services) =>
        {
            // Arrange
            var user = await CreateAndLoginValidUser(services);
            var account = await CreateAccount(services, new()
            {
                { user.Id, AccountRole.Owner },
            });
            var command = GetValidCommand(account.Id);

            // Act
            var response = await mediator.Send(command);

            // Assert
            Assert.True(response.IsValid);
        });

    [Fact]
    public async Task Should_CreateApplication_When_MinimumRequestIsValid() =>
        await MediatorTest(async (mediator, services) =>
        {
            // Arrange
            var user = await CreateAndLoginValidUser(services);
            var account = await CreateAccount(services, new()
            {
                { user.Id, AccountRole.Owner },
            });
            var command = new CreateCommand.Command
            {
                AccountId = account.Id,
                AutoProvision = true,
                EnableProvision = true,
                DisplayName = "Microsoft Azure AD Application",
            };

            // Act
            var response = await mediator.Send(command);

            // Assert
            Assert.True(response.IsValid);
        });

    [Fact]
    public async Task Should_CreateApplication_When_OidcProviderIsSpecified() =>
        await MediatorTest(async (mediator, services) =>
        {
            // Arrange
            var user = await CreateAndLoginValidUser(services);
            var account = await CreateAccount(services, new()
            {
                { user.Id, AccountRole.Owner },
            });
            var command = GetValidCommand(account.Id);
            var oidcProvider = await mediator.Send(new CreateOidcProviderCommand.Command
            {
                AccountId = account.Id,
                AuthorityUrl = "https://microsoft.com",
                ClientId = "microsoft-internal-azure-id-client",
                ClientSecret = "microsoft-internal-azure-id-client.secret",
                IsDefault = false,
                IsVisible = true,
                Name = "Micrsoft Internal",
                ResponseType = "code",
                Scopes = new() { "email", "profile" },
            });
            command.ConnectedOidcProviders.Add(oidcProvider.Result!.Id);

            // Act
            var response = await mediator.Send(command);

            // Assert
            Assert.True(response.IsValid);
        });

    [Fact]
    public async Task Should_NotBeValid_When_UserIsntAuthorized() =>
        await MediatorTest(async (mediator, services) =>
        {
            // Arrange
            var user = await CreateAndLoginValidUser(services);
            var account = await CreateAccount(services, new()
            {
                { user.Id, AccountRole.Reader },
            });
            var command = GetValidCommand(account.Id);
            var validator = new CreateCommand.CommandValidator(services);

            // Act
            var response = await validator.ValidateAsync(command);

            // Assert
            Assert.False(response.IsValid);
            Assert.Contains(response.Errors, error => error.PropertyName == nameof(CreateCommand.Command.AccountId)
                && error.ErrorCode == nameof(ErrorCodes.MisingRoleForAccount));
        });

    [Fact]
    public async Task Should_NotBeValid_When_OidcProviderBelongsToOtherAccount() =>
        await MediatorTest(async (mediator, services) =>
        {
            // Arrange
            var user = await CreateAndLoginValidUser(services);
            var account = await CreateAccount(services, new()
            {
                { user.Id, AccountRole.Owner },
            });
            var command = GetValidCommand(account.Id);
            var otherAccount = await CreateAccount(services, new()
            {
                { user.Id, AccountRole.Owner },
            });
            var oidcProvider = await mediator.Send(new CreateOidcProviderCommand.Command
            {
                AccountId = otherAccount.Id,
                AuthorityUrl = "https://microsoft.com",
                ClientId = "microsoft-internal-azure-id-client",
                ClientSecret = "microsoft-internal-azure-id-client.secret",
                IsDefault = false,
                IsVisible = true,
                Name = "Micrsoft Internal",
                ResponseType = "code",
                Scopes = new() { "email", "profile" },
            });
            command.ConnectedOidcProviders.Add(oidcProvider.Result!.Id);
            var validator = new CreateCommand.CommandValidator(services);

            // Act
            var response = await validator.ValidateAsync(command);

            // Assert
            Assert.False(response.IsValid);
            Assert.Contains(response.Errors, error => error.PropertyName == nameof(CreateCommand.Command.ConnectedOidcProviders)
                && error.ErrorCode == nameof(ErrorCodes.InvalidOidcProvider));
        });

    [Fact]
    public async Task Should_NotBeValid_When_AutoProvisionIsNotSet() =>
        await MediatorTest(async (mediator, services) =>
        {
            // Arrange
            var user = await CreateAndLoginValidUser(services);
            var account = await CreateAccount(services, new()
            {
                { user.Id, AccountRole.Reader },
            });
            var command = GetValidCommand(account.Id);
            command.AutoProvision = default;
            var validator = new CreateCommand.CommandValidator(services);

            // Act
            var response = await validator.ValidateAsync(command);

            // Assert
            Assert.False(response.IsValid);
            Assert.Contains(response.Errors, error => error.PropertyName == nameof(CreateCommand.Command.AutoProvision)
                && error.ErrorCode == "NotEmptyValidator");
        });

    [Fact]
    public async Task Should_NotBeValid_When_EnableProvisionIsNotSet() =>
        await MediatorTest(async (mediator, services) =>
        {
            // Arrange
            var user = await CreateAndLoginValidUser(services);
            var account = await CreateAccount(services, new()
            {
                { user.Id, AccountRole.Reader },
            });
            var command = GetValidCommand(account.Id);
            command.EnableProvision = default;
            var validator = new CreateCommand.CommandValidator(services);

            // Act
            var response = await validator.ValidateAsync(command);

            // Assert
            Assert.False(response.IsValid);
            Assert.Contains(response.Errors, error => error.PropertyName == nameof(CreateCommand.Command.EnableProvision)
                && error.ErrorCode == "NotEmptyValidator");
        });

    [Fact]
    public async Task Should_NotBeValid_When_DisplayNameIsNotSet() =>
        await MediatorTest(async (mediator, services) =>
        {
            // Arrange
            var user = await CreateAndLoginValidUser(services);
            var account = await CreateAccount(services, new()
            {
                { user.Id, AccountRole.Reader },
            });
            var command = GetValidCommand(account.Id);
            command.DisplayName = default;
            var validator = new CreateCommand.CommandValidator(services);

            // Act
            var response = await validator.ValidateAsync(command);

            // Assert
            Assert.False(response.IsValid);
            Assert.Contains(response.Errors, error => error.PropertyName == nameof(CreateCommand.Command.DisplayName)
                && error.ErrorCode == "NotEmptyValidator");
        });

    [Fact]
    public async Task Should_NotBeValid_When_DefaultUriIsInListOfOrigins() =>
        await MediatorTest(async (mediator, services) =>
        {
            // Arrange
            var user = await CreateAndLoginValidUser(services);
            var account = await CreateAccount(services, new()
            {
                { user.Id, AccountRole.Reader },
            });
            var command = GetValidCommand(account.Id);
            command.Origins.Add(command.DefaultOrigin!);
            var validator = new CreateCommand.CommandValidator(services);

            // Act
            var response = await validator.ValidateAsync(command);

            // Assert
            Assert.False(response.IsValid);
            Assert.Contains(response.Errors, error => error.PropertyName == nameof(CreateCommand.Command.Origins)
                && error.ErrorCode == nameof(ErrorCodes.DuplicateOrigin));
        });

    [Fact]
    public async Task Should_NotBeValid_When_RedirectUriIsNotUri() =>
        await MediatorTest(async (mediator, services) =>
        {
            // Arrange
            var user = await CreateAndLoginValidUser(services);
            var account = await CreateAccount(services, new()
            {
                { user.Id, AccountRole.Reader },
            });
            var command = GetValidCommand(account.Id);
            command.RedirectUris.Add("not-a-valid-uri");
            var validator = new CreateCommand.CommandValidator(services);

            // Act
            var response = await validator.ValidateAsync(command);

            // Assert
            Assert.False(response.IsValid);
            Assert.Contains(response.Errors, error => error.PropertyName.Contains(nameof(CreateCommand.Command.RedirectUris))
                && error.ErrorCode == nameof(ErrorCodes.InvalidUri));
        });

    [Fact]
    public async Task Should_NotBeValid_When_PostLogoutRedirectUriIsNotUri() =>
        await MediatorTest(async (mediator, services) =>
        {
            // Arrange
            var user = await CreateAndLoginValidUser(services);
            var account = await CreateAccount(services, new()
            {
                { user.Id, AccountRole.Reader },
            });
            var command = GetValidCommand(account.Id);
            command.PostLogoutRedirectUris.Add("not-a-valid-uri");
            var validator = new CreateCommand.CommandValidator(services);

            // Act
            var response = await validator.ValidateAsync(command);

            // Assert
            Assert.False(response.IsValid);
            Assert.Contains(response.Errors, error => error.PropertyName.Contains(nameof(CreateCommand.Command.PostLogoutRedirectUris))
                && error.ErrorCode == nameof(ErrorCodes.InvalidUri));
        });

    [Fact]
    public async Task Should_NotBeValid_When_OriginsIsNotUri() =>
        await MediatorTest(async (mediator, services) =>
        {
            // Arrange
            var user = await CreateAndLoginValidUser(services);
            var account = await CreateAccount(services, new()
            {
                { user.Id, AccountRole.Reader },
            });
            var command = GetValidCommand(account.Id);
            command.Origins.Add("not-a-valid-uri");
            var validator = new CreateCommand.CommandValidator(services);

            // Act
            var response = await validator.ValidateAsync(command);

            // Assert
            Assert.False(response.IsValid);
            Assert.Contains(response.Errors, error => error.PropertyName.Contains(nameof(CreateCommand.Command.Origins))
                && error.ErrorCode == nameof(ErrorCodes.InvalidUri));
        });

    [Fact]
    public async Task Should_NotBeValid_When_DefaultOriginIsNotUri() =>
        await MediatorTest(async (mediator, services) =>
        {
            // Arrange
            var user = await CreateAndLoginValidUser(services);
            var account = await CreateAccount(services, new()
            {
                { user.Id, AccountRole.Reader },
            });
            var command = GetValidCommand(account.Id);
            command.DefaultOrigin = "not-a-valid-uri";
            var validator = new CreateCommand.CommandValidator(services);

            // Act
            var response = await validator.ValidateAsync(command);

            // Assert
            Assert.False(response.IsValid);
            Assert.Contains(response.Errors, error => error.PropertyName == nameof(CreateCommand.Command.DefaultOrigin)
                && error.ErrorCode == nameof(ErrorCodes.InvalidUri));
        });

    [Fact]
    public async Task Should_NotBeValid_When_DefaultOriginIsNotSetButOriginsIs() =>
        await MediatorTest(async (mediator, services) =>
        {
            // Arrange
            var user = await CreateAndLoginValidUser(services);
            var account = await CreateAccount(services, new()
            {
                { user.Id, AccountRole.Reader },
            });
            var command = GetValidCommand(account.Id);
            command.DefaultOrigin = default;
            var validator = new CreateCommand.CommandValidator(services);

            // Act
            var response = await validator.ValidateAsync(command);

            // Assert
            Assert.False(response.IsValid);
            Assert.Contains(response.Errors, error => error.PropertyName == nameof(CreateCommand.Command.Origins)
                && error.ErrorCode == "EmptyValidator");
        });

    private CreateCommand.Command GetValidCommand(string accountId)
        => new CreateCommand.Command
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
        };
}