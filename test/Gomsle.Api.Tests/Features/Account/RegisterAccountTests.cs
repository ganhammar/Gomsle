using Gomsle.Api.Features.User;
using Gomsle.Api.Tests.Infrastructure;
using Xunit;

namespace Gomsle.Api.Tests.Features.User;

[Collection("Sequential")]
public class RegisterTests : TestBase
{
    [Fact]
    public async Task Should_RegisterUser_When_CommandIsValid() =>
        await MediatorTest(async (mediator, services) =>
        {
            // Arrange
            var command = new RegisterCommand.Command
            {
                Email = "test@gomsle.com",
                UserName = "test",
                Password = "itsaseasyas123",
                ReturnUrl = "https://gomsle.com",
            };

            // Act
            var response = await mediator.Send(command);

            // Assert
            Assert.True(response.IsValid);
            Assert.Equal("test", response.Result!.UserName);
        });
}