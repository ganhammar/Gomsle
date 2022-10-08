using Gomsle.Api.Features.Account;
using Gomsle.Api.Tests.Infrastructure;
using Xunit;

namespace Gomsle.Api.Tests.Features.Account;

[Collection("Sequential")]
public class RegisterCommandTests : TestBase
{
    [Fact]
    public async Task Should_RegisterUser_When_CommandIsValid() =>
        await MediatorTest(new RegisterCommand.Command
        {
            Email = "test@gomsle.com",
            UserName = "test",
            Password = "itsaseasyas123",
            ReturnUrl = "https://gomsle.com",
        },
        (response) =>
        {
            Assert.True(response.IsValid);
            Assert.Equal("test", response.Result!.UserName);
        });
}