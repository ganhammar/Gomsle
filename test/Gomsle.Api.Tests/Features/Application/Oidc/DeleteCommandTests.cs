using Gomsle.Api.Features.Account;
using Gomsle.Api.Features.Application.Oidc;
using Gomsle.Api.Infrastructure.Validators;
using Gomsle.Api.Tests.Infrastructure;
using MediatR;
using Xunit;
using CreateCommand = Gomsle.Api.Features.Application.Oidc.CreateCommand;

namespace Gomsle.Api.Tests.Features.Application.Oidc;

[Collection("Sequential")]
public class DeleteCommandTests : TestBase
{
    [Fact]
    public async Task Should_DeleteApplication_When_RequestIsValid() =>
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
            DisplayName = "Microsoft Azure AD Application",
        });

        return result.Result!.Id!;
    }

    private async Task<string> CreateOidcProvider(IMediator mediator, string applicationId)
    {
        var result = await mediator.Send(new CreateCommand.Command
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
        });

        return result.Result!.Id;
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
        var oidcProviderId = await CreateOidcProvider(mediator, applicationId);
        return GetValidCommand(oidcProviderId);
    }
}