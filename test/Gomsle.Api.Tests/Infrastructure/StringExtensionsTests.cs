using Gomsle.Api.Infrastructure.Extensions;
using Xunit;

namespace Gomsle.Api.Tests.Infrastructure;

public class StringExtensionsTests
{
    [Theory]
    [InlineData("something-url-friendly", "something-url-friendly")]
    [InlineData("sömething-nöt-ürl-friéndly", "something-not-url-friendly")]
    [InlineData("\not/okay right?", "otokay-right")]
    [InlineData("wwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwww", "wwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwww")]
    [InlineData("", "")]
    [InlineData("UPPER CASE", "upper-case")]
    public void Should_ReturnExpectedString_When_TransformingToUrlFriendly(
        string input, string expected)
    {
        Assert.Equal(expected, input.UrlFriendly());
    }

    [Fact]
    public void Should_ChangeGuids_When_TransformingToUrlFriendly()
    {
        var guid = Guid.NewGuid().ToString();
        Assert.NotEqual(guid, guid.UrlFriendly());
    }
}