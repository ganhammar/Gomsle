using Gomsle.Api.Features.Authorization;
using Gomsle.Api.Tests.Infrastructure;
using Xunit;

namespace Gomsle.Api.Tests.Features.Authorize;

[Collection("Sequential")]
public class LogoutCommandTests : TestBase
{
    [Fact]
    public async Task Should_BeSuccessful_When_LoggingOut() =>
        await MediatorTest(async (mediator, services) =>
        {
            // Arrange
            var command = new LogoutCommand.Command();

            // Act
            var result = await mediator.Send(command);

            // Assert
            Assert.True(result.IsValid);
        });
}
