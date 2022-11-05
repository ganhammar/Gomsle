using AspNetCore.Identity.AmazonDynamoDB;
using Gomsle.Api.Features.Account;
using Gomsle.Api.Features.Email;
using Gomsle.Api.Infrastructure.Validators;
using Gomsle.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Gomsle.Api.Tests.Features.Account;

[Collection("Sequential")]
public class InviteCommandTests : TestBase
{
    [Fact]
    public async Task Should_InviteUser_When_RequestIsValid() =>
        await MediatorTest(async (mediator, services) =>
        {
            // Arrange
            var user = await CreateAndLoginValidUser(services);
            var accountName = "Microsoft";
            var email = "test@gomsle.com";
            var url = "https://gomsle.com/microsoft/invite";
            var account = await mediator.Send(new CreateCommand.Command
            {
                Name = accountName,
            });
            var command = new InviteCommand.Command
            {
                AccountId = account.Result!.Id,
                Email = email,
                InvitationUrl = url,
                Role = AccountRole.Administrator,
                SuccessUrl = "https://gomsle.com/microsoft/login",
            };

            // Act
            var response = await mediator.Send(command);

            // Assert
            Assert.True(response.IsValid);

            var mock = GetMock<IEmailSender>();
            mock!.Verify(x => 
                x.Send(email, It.IsAny<string>(), It.Is<string>(x => x.Contains(url)), It.IsAny<CancellationToken>()),
                Times.Once());
        });

    [Fact]
    public async Task Should_ContainInternalUrl_When_InvitedUserExists() =>
        await MediatorTest(async (mediator, services) =>
        {
            // Arrange
            var user = await CreateAndLoginValidUser(services);
            var accountName = "Microsoft";
            var email = "test@gomsle.com";
            var userManager = services.GetRequiredService<UserManager<DynamoDbUser>>();
            var test = await userManager.CreateAsync(new()
            {
                Email = email,
                UserName = email,
            });
            var account = await mediator.Send(new CreateCommand.Command
            {
                Name = accountName,
            });
            var command = new InviteCommand.Command
            {
                AccountId = account.Result!.Id,
                Email = email,
                InvitationUrl = "https://elsmog.com/microsoft/invite",
                Role = AccountRole.Administrator,
                SuccessUrl = "https://elsmog.com/microsoft/login",
            };

            // Act
            var response = await mediator.Send(command);

            // Assert
            Assert.True(response.IsValid);

            var mock = GetMock<IEmailSender>();
            mock!.Verify(x => 
                x.Send(email, It.IsAny<string>(), It.Is<string>(x => x.Contains("http://gomsle.com")), It.IsAny<CancellationToken>()),
                Times.Once());
        });

    [Fact]
    public async Task Should_NotBeValid_When_AccountIdIsNotSet() =>
        await MediatorTest(async (mediator, services) =>
        {
            // Arrange
            var command = new InviteCommand.Command
            {
                Email = "test@gomsle.com",
                InvitationUrl = "https://gomsle.com/microsoft/invite",
                Role = AccountRole.Administrator,
                SuccessUrl = "https://gomsle.com/microsoft/login",
            };
            var validator = new InviteCommand.CommandValidator(services);

            // Act
            var response = await validator.ValidateAsync(command);

            // Assert
            Assert.False(response.IsValid);
            Assert.Contains(response.Errors, x => x.ErrorCode == "NotEmptyValidator" &&
                x.PropertyName == nameof(InviteCommand.Command.AccountId));
        });

    [Fact]
    public async Task Should_NotBeValid_When_AccountDoesntExist() =>
        await MediatorTest(async (mediator, services) =>
        {
            // Arrange
            var command = new InviteCommand.Command
            {
                AccountId = Guid.NewGuid().ToString(),
                Email = "test@gomsle.com",
                InvitationUrl = "https://gomsle.com/microsoft/invite",
                Role = AccountRole.Administrator,
                SuccessUrl = "https://gomsle.com/microsoft/login",
            };
            var validator = new InviteCommand.CommandValidator(services);

            // Act
            var response = await validator.ValidateAsync(command);

            // Assert
            Assert.False(response.IsValid);
            Assert.Contains(response.Errors, x => x.ErrorCode == nameof(ErrorCodes.NotAuthenticated) &&
                x.PropertyName == nameof(InviteCommand.Command.AccountId));
        });

    [Fact]
    public async Task Should_NotBeValid_When_EmailIsNotSet() =>
        await MediatorTest(async (mediator, services) =>
        {
            // Arrange
            var command = new InviteCommand.Command
            {
                AccountId = Guid.NewGuid().ToString(),
                InvitationUrl = "https://gomsle.com/microsoft/invite",
                Role = AccountRole.Administrator,
                SuccessUrl = "https://gomsle.com/microsoft/login",
            };
            var validator = new InviteCommand.CommandValidator(services);

            // Act
            var response = await validator.ValidateAsync(command);

            // Assert
            Assert.False(response.IsValid);
            Assert.Contains(response.Errors, x => x.ErrorCode == "NotEmptyValidator" &&
                x.PropertyName == nameof(InviteCommand.Command.Email));
        });

    [Fact]
    public async Task Should_NotBeValid_When_EmailIsNotAnEmail() =>
        await MediatorTest(async (mediator, services) =>
        {
            // Arrange
            var command = new InviteCommand.Command
            {
                Email = "not-an-email",
                AccountId = Guid.NewGuid().ToString(),
                InvitationUrl = "https://gomsle.com/microsoft/invite",
                Role = AccountRole.Administrator,
                SuccessUrl = "https://gomsle.com/microsoft/login",
            };
            var validator = new InviteCommand.CommandValidator(services);

            // Act
            var response = await validator.ValidateAsync(command);

            // Assert
            Assert.False(response.IsValid);
            Assert.Contains(response.Errors, x => x.ErrorCode == "EmailValidator" &&
                x.PropertyName == nameof(InviteCommand.Command.Email));
        });

    [Fact]
    public async Task Should_NotBeValid_When_InvitationUrlIsNotSet() =>
        await MediatorTest(async (mediator, services) =>
        {
            // Arrange
            var command = new InviteCommand.Command
            {
                Email = "test@gomsle.com",
                AccountId = Guid.NewGuid().ToString(),
                Role = AccountRole.Administrator,
                SuccessUrl = "https://gomsle.com/microsoft/login",
            };
            var validator = new InviteCommand.CommandValidator(services);

            // Act
            var response = await validator.ValidateAsync(command);

            // Assert
            Assert.False(response.IsValid);
            Assert.Contains(response.Errors, x => x.ErrorCode == "NotEmptyValidator" &&
                x.PropertyName == nameof(InviteCommand.Command.InvitationUrl));
        });

    [Fact]
    public async Task Should_NotBeValid_When_SuccessUrlIsNotSet() =>
        await MediatorTest(async (mediator, services) =>
        {
            // Arrange
            var command = new InviteCommand.Command
            {
                Email = "test@gomsle.com",
                AccountId = Guid.NewGuid().ToString(),
                Role = AccountRole.Administrator,
                InvitationUrl = "https://gomsle.com/microsoft/invite",
            };
            var validator = new InviteCommand.CommandValidator(services);

            // Act
            var response = await validator.ValidateAsync(command);

            // Assert
            Assert.False(response.IsValid);
            Assert.Contains(response.Errors, x => x.ErrorCode == "NotEmptyValidator" &&
                x.PropertyName == nameof(InviteCommand.Command.SuccessUrl));
        });

    [Fact]
    public async Task Should_NotBeValid_When_RoleIsNotSet() =>
        await MediatorTest(async (mediator, services) =>
        {
            // Arrange
            var command = new InviteCommand.Command
            {
                Email = "test@gomsle.com",
                AccountId = Guid.NewGuid().ToString(),
                InvitationUrl = "https://gomsle.com/microsoft/invite",
                SuccessUrl = "https://gomsle.com/microsoft/login",
            };
            var validator = new InviteCommand.CommandValidator(services);

            // Act
            var response = await validator.ValidateAsync(command);

            // Assert
            Assert.False(response.IsValid);
            Assert.Contains(response.Errors, x => x.ErrorCode == "NotEmptyValidator" &&
                x.PropertyName == nameof(InviteCommand.Command.Role));
        });

    [Fact]
    public async Task Should_NotBeValid_When_RoleIsOwner() =>
        await MediatorTest(async (mediator, services) =>
        {
            // Arrange
            var command = new InviteCommand.Command
            {
                Email = "test@gomsle.com",
                AccountId = Guid.NewGuid().ToString(),
                InvitationUrl = "https://gomsle.com/microsoft/invite",
                Role = AccountRole.Owner,
                SuccessUrl = "https://gomsle.com/microsoft/login",
            };
            var validator = new InviteCommand.CommandValidator(services);

            // Act
            var response = await validator.ValidateAsync(command);

            // Assert
            Assert.False(response.IsValid);
            Assert.Contains(response.Errors, x => x.ErrorCode == "OnlyOneOwner" &&
                x.PropertyName == nameof(InviteCommand.Command.Role));
        });
}