using Gomsle.Api.Features.Account;
using Gomsle.Api.Features.OidcProvider;
using Gomsle.Api.Tests.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using CreateCommand = Gomsle.Api.Features.OidcProvider.CreateCommand;

namespace Gomsle.Api.Tests.Features.OidcProvider;

[Collection("Sequential")]
public class OidcProviderControllerTests : TestBase
{
    private Func<IServiceProvider, object[]> ConfigureController = (services) =>
    {
        var mediator = services.GetRequiredService<IMediator>();

        return new object[] { mediator };
    };

    [Fact]
    public async Task Should_CreateOidcProvider_When_RequestIsValid() => await ControllerTest<OidcProviderController>(
        // Arrange
        ConfigureController,
        // Act & Assert
        async (controller, services) =>
        {
            // Act
            var user = await CreateAndLoginValidUser(services);
            var account = await CreateAccount(services, new()
            {
                { user.Id, AccountRole.Owner },
            });
            var result = await controller.Create(new()
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

            // Assert
            Assert.NotNull(result);

            var okResult = result as OkObjectResult;
            Assert.NotNull(okResult);

            var response = okResult!.Value as OidcProviderModel;
            Assert.NotNull(response);
        });

    [Fact]
    public async Task Should_EditOidcProvider_When_RequestIsValid() => await ControllerTest<OidcProviderController>(
        // Arrange
        ConfigureController,
        // Act & Assert
        async (controller, services) =>
        {
            // Act
            var mediator = services.GetRequiredService<IMediator>();
            var user = await CreateAndLoginValidUser(services);
            var account = await CreateAccount(services, new()
            {
                { user.Id, AccountRole.Owner },
            });
            var OidcProviderId = await CreateOidcProvider(mediator, account.Id);
            var result = await controller.Edit(new()
            {
                Id = OidcProviderId,
                AuthorityUrl = "https://microsoft.com",
                ClientId = "microsoft-internal-azure-id-client",
                ClientSecret = "microsoft-internal-azure-id-client.secret",
                IsDefault = false,
                IsVisible = true,
                Name = "Micrsoft Internal Provider",
                ResponseType = "code",
                Scopes = new() { "email", "profile" },
            });

            // Assert
            Assert.NotNull(result);

            var okResult = result as OkObjectResult;
            Assert.NotNull(okResult);

            var response = okResult!.Value as OidcProviderModel;
            Assert.NotNull(response);
        });

    [Fact]
    public async Task Should_DeleteOidcProvider_When_RequestIsValid() => await ControllerTest<OidcProviderController>(
        // Arrange
        ConfigureController,
        // Act & Assert
        async (controller, services) =>
        {
            // Act
            var mediator = services.GetRequiredService<IMediator>();
            var user = await CreateAndLoginValidUser(services);
            var account = await CreateAccount(services, new()
            {
                { user.Id, AccountRole.Owner },
            });
            var OidcProviderId = await CreateOidcProvider(mediator, account.Id);
            var result = await controller.Delete(new()
            {
                Id = OidcProviderId,
            });

            // Assert
            Assert.NotNull(result);

            var noContentResult = result as NoContentResult;
            Assert.NotNull(noContentResult);
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

        return result.Result!.Id!;
    }
}