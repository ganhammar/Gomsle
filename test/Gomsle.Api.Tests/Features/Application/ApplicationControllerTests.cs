using Gomsle.Api.Features.Account;
using Gomsle.Api.Features.Application;
using Gomsle.Api.Tests.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using CreateCommand = Gomsle.Api.Features.Application.CreateCommand;

namespace Gomsle.Api.Tests.Features.Application;

[Collection("Sequential")]
public class ApplicationControllerTests : TestBase
{
    private Func<IServiceProvider, object[]> ConfigureController = (services) =>
    {
        var mediator = services.GetRequiredService<IMediator>();

        return new object[] { mediator };
    };

    [Fact]
    public async Task Should_CreateApplication_When_RequestIsValid() => await ControllerTest<ApplicationController>(
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
                AutoProvision = true,
                EnableProvision = true,
                DisplayName = "Microsoft Azure AD Application",
            });

            // Assert
            Assert.NotNull(result);

            var okResult = result as OkObjectResult;
            Assert.NotNull(okResult);

            var response = okResult!.Value as ApplicationDto;
            Assert.NotNull(response);
        });

    [Fact]
    public async Task Should_EditApplication_When_RequestIsValid() => await ControllerTest<ApplicationController>(
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
            var applicationId = await CreateApplication(mediator, account.Id);
            var result = await controller.Edit(new()
            {
                Id = applicationId,
                AutoProvision = true,
                EnableProvision = true,
                DisplayName = "Microsoft Azure Active Directory Application",
            });

            // Assert
            Assert.NotNull(result);

            var okResult = result as OkObjectResult;
            Assert.NotNull(okResult);

            var response = okResult!.Value as ApplicationDto;
            Assert.NotNull(response);
        });

    [Fact]
    public async Task Should_DeleteApplication_When_RequestIsValid() => await ControllerTest<ApplicationController>(
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
            var applicationId = await CreateApplication(mediator, account.Id);
            var result = await controller.Delete(new()
            {
                Id = applicationId,
            });

            // Assert
            Assert.NotNull(result);

            var noContentResult = result as NoContentResult;
            Assert.NotNull(noContentResult);
        });

    private async Task<string> CreateApplication(IMediator mediator, string accountId)
    {
        var result = await mediator.Send(new CreateCommand.Command
        {
            AccountId = accountId,
            AutoProvision = true,
            EnableProvision = true,
            DisplayName = "Microsoft Azure AD Application",
        });

        return result.Result!.Id!;
    }
}