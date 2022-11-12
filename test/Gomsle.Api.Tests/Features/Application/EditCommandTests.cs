using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Gomsle.Api.Features.Account;
using Gomsle.Api.Features.Application;
using Gomsle.Api.Infrastructure.Validators;
using Gomsle.Api.Tests.Infrastructure;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Gomsle.Api.Tests.Features.Application;

[Collection("Sequential")]
public class EditCommandTests : TestBase
{
    [Fact]
    public async Task Should_EditApplication_When_RequestIsValid() =>
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
    public async Task Should_EditApplication_When_MinimumRequirementsIsSet() =>
        await MediatorTest(async (mediator, services) =>
        {
            // Arrange
            var user = await CreateAndLoginValidUser(services);
            var account = await CreateAccount(services, new()
            {
                { user.Id, AccountRole.Owner },
            });
            var applicationId = await CreateApplication(mediator, account.Id);
            var command = new EditCommand.Command
            {
                Id = applicationId,
                AutoProvision = true,
                EnableProvision = true,
                DisplayName = "Microsoft Azure AD Application",
            };

            // Act
            var response = await mediator.Send(command);

            // Assert
            Assert.True(response.IsValid);
            Assert.Empty(response.Result!.Origins);
            Assert.Empty(response.Result!.RedirectUris);
            Assert.Empty(response.Result!.PostLogoutRedirectUris);
        });

    
    [Fact]
    public async Task Should_NotBeValid_When_UserIsntAuthorized() =>
        await MediatorTest(async (mediator, services) =>
        {
            // Arrange
            var user = await CreateAndLoginValidUser(services);
            var account = await CreateAccount(services, new()
            {
                { user.Id, AccountRole.Owner },
            });
            var applicationId = await CreateApplication(mediator, account.Id);

            var database = services.GetRequiredService<IAmazonDynamoDB>();
            var context = new DynamoDBContext(database);
            account.Members = new()
            {
                { user.Id, AccountRole.Reader },
            };
            await context.SaveAsync(account);

            var command = GetValidCommand(applicationId);
            var validator = new EditCommand.CommandValidator(services);

            // Act
            var response = await validator.ValidateAsync(command);

            // Assert
            Assert.False(response.IsValid);
            Assert.Contains(response.Errors, error => error.PropertyName == nameof(EditCommand.Command.Id)
                && error.ErrorCode == nameof(ErrorCodes.NotAuthorized));
        });

    [Fact]
    public async Task Should_NotBeValid_When_AutoProvisionIsNotSet() =>
        await MediatorTest(async (mediator, services) =>
        {
            // Arrange
            var command = await Prepare(services, mediator);
            command.AutoProvision = default;
            var validator = new EditCommand.CommandValidator(services);

            // Act
            var response = await validator.ValidateAsync(command);

            // Assert
            Assert.False(response.IsValid);
            Assert.Contains(response.Errors, error => error.PropertyName == nameof(EditCommand.Command.AutoProvision)
                && error.ErrorCode == "NotEmptyValidator");
        });

    [Fact]
    public async Task Should_NotBeValid_When_EnableProvisionIsNotSet() =>
        await MediatorTest(async (mediator, services) =>
        {
            // Arrange
            var command = await Prepare(services, mediator);
            command.EnableProvision = default;
            var validator = new EditCommand.CommandValidator(services);

            // Act
            var response = await validator.ValidateAsync(command);

            // Assert
            Assert.False(response.IsValid);
            Assert.Contains(response.Errors, error => error.PropertyName == nameof(EditCommand.Command.EnableProvision)
                && error.ErrorCode == "NotEmptyValidator");
        });

    [Fact]
    public async Task Should_NotBeValid_When_DisplayNameIsNotSet() =>
        await MediatorTest(async (mediator, services) =>
        {
            // Arrange
            var command = await Prepare(services, mediator);
            command.DisplayName = default;
            var validator = new EditCommand.CommandValidator(services);

            // Act
            var response = await validator.ValidateAsync(command);

            // Assert
            Assert.False(response.IsValid);
            Assert.Contains(response.Errors, error => error.PropertyName == nameof(EditCommand.Command.DisplayName)
                && error.ErrorCode == "NotEmptyValidator");
        });

    [Fact]
    public async Task Should_NotBeValid_When_DefaultUriIsInListOfOrigins() =>
        await MediatorTest(async (mediator, services) =>
        {
            // Arrange
            var command = await Prepare(services, mediator);
            command.Origins.Add(command.DefaultOrigin!);
            var validator = new EditCommand.CommandValidator(services);

            // Act
            var response = await validator.ValidateAsync(command);

            // Assert
            Assert.False(response.IsValid);
            Assert.Contains(response.Errors, error => error.PropertyName == nameof(EditCommand.Command.Origins)
                && error.ErrorCode == nameof(ErrorCodes.DuplicateOrigin));
        });

    [Fact]
    public async Task Should_NotBeValid_When_RedirectUriIsNotUri() =>
        await MediatorTest(async (mediator, services) =>
        {
            // Arrange
            var command = await Prepare(services, mediator);
            command.RedirectUris.Add("not-a-valid-uri");
            var validator = new EditCommand.CommandValidator(services);

            // Act
            var response = await validator.ValidateAsync(command);

            // Assert
            Assert.False(response.IsValid);
            Assert.Contains(response.Errors, error => error.PropertyName.Contains(nameof(EditCommand.Command.RedirectUris))
                && error.ErrorCode == nameof(ErrorCodes.InvalidUri));
        });

    [Fact]
    public async Task Should_NotBeValid_When_PostLogoutRedirectUriIsNotUri() =>
        await MediatorTest(async (mediator, services) =>
        {
            // Arrange
            var command = await Prepare(services, mediator);
            command.PostLogoutRedirectUris.Add("not-a-valid-uri");
            var validator = new EditCommand.CommandValidator(services);

            // Act
            var response = await validator.ValidateAsync(command);

            // Assert
            Assert.False(response.IsValid);
            Assert.Contains(response.Errors, error => error.PropertyName.Contains(nameof(EditCommand.Command.PostLogoutRedirectUris))
                && error.ErrorCode == nameof(ErrorCodes.InvalidUri));
        });

    [Fact]
    public async Task Should_NotBeValid_When_OriginsIsNotUri() =>
        await MediatorTest(async (mediator, services) =>
        {
            // Arrange
            var command = await Prepare(services, mediator);
            command.Origins.Add("not-a-valid-uri");
            var validator = new EditCommand.CommandValidator(services);

            // Act
            var response = await validator.ValidateAsync(command);

            // Assert
            Assert.False(response.IsValid);
            Assert.Contains(response.Errors, error => error.PropertyName.Contains(nameof(EditCommand.Command.Origins))
                && error.ErrorCode == nameof(ErrorCodes.InvalidUri));
        });

    [Fact]
    public async Task Should_NotBeValid_When_DefaultOriginIsNotUri() =>
        await MediatorTest(async (mediator, services) =>
        {
            // Arrange
            var command = await Prepare(services, mediator);
            command.DefaultOrigin = "not-a-valid-uri";
            var validator = new EditCommand.CommandValidator(services);

            // Act
            var response = await validator.ValidateAsync(command);

            // Assert
            Assert.False(response.IsValid);
            Assert.Contains(response.Errors, error => error.PropertyName == nameof(EditCommand.Command.DefaultOrigin)
                && error.ErrorCode == nameof(ErrorCodes.InvalidUri));
        });

    [Fact]
    public async Task Should_NotBeValid_When_DefaultOriginIsNotSetButOriginsIs() =>
        await MediatorTest(async (mediator, services) =>
        {
            // Arrange
            var command = await Prepare(services, mediator);
            command.DefaultOrigin = default;
            var validator = new EditCommand.CommandValidator(services);

            // Act
            var response = await validator.ValidateAsync(command);

            // Assert
            Assert.False(response.IsValid);
            Assert.Contains(response.Errors, error => error.PropertyName == nameof(EditCommand.Command.Origins)
                && error.ErrorCode == "EmptyValidator");
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

    private EditCommand.Command GetValidCommand(string id)
        => new EditCommand.Command
        {
            Id = id,
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

    private async Task<EditCommand.Command> Prepare(
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