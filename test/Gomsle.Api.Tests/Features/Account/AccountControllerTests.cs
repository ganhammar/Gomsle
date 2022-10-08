using Amazon.DynamoDBv2;
using AspNetCore.Identity.AmazonDynamoDB;
using Gomsle.Api.Features.Account;
using Gomsle.Api.Features.Email;
using Gomsle.Api.Infrastructure;
using Gomsle.Api.Tests.Features.Email;
using Gomsle.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using OpenIddict.AmazonDynamoDB;
using Xunit;

namespace Gomsle.Api.Tests.Features.Account;

public class AccountControllerTests
{
    [Fact]
    public async Task Should_RegisterUser_When_RequestIsValid()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            // Arrange
            var serviceCollection = new ServiceCollection();
            serviceCollection
                .AddOpenIddict()
                .AddCore()
                .UseDynamoDb();
            serviceCollection.AddIdentity();
            serviceCollection.AddSingleton<IEmailSender, MockEmailSender>();
            serviceCollection.AddSingleton<IAmazonDynamoDB>(database.Client);
            serviceCollection.AddControllers();
            var serviceProvider = serviceCollection.BuildServiceProvider();

            await AspNetCoreIdentityDynamoDbSetup.EnsureInitializedAsync(serviceProvider);
            await OpenIddictDynamoDbSetup.EnsureInitializedAsync(serviceProvider);

            var userManager = serviceProvider.GetRequiredService<UserManager<DynamoDbUser>>();
            var signInManager = serviceProvider.GetRequiredService<SignInManager<DynamoDbUser>>();
            var emailSender = serviceProvider.GetRequiredService<IEmailSender>();

            var request = new Mock<HttpRequest>();
            request.Setup(x => x.Scheme).Returns("http");
            request.Setup(x => x.Host).Returns(HostString.FromUriComponent("http://gomsle.com"));
            request.Setup(x => x.PathBase).Returns(PathString.FromUriComponent("/api"));

            var httpContext = Mock.Of<HttpContext>(_ => 
                _.Request == request.Object
            );

            var controllerContext = new ControllerContext
            {
                HttpContext = httpContext,
            };

            var mockUrlHelper = new Mock<IUrlHelper>();
            mockUrlHelper
                .Setup(x => x.Link(It.IsAny<string>(), It.IsAny<object>()))
                .Returns("http://some-url.com");

            var controller = new AccountController(userManager, signInManager, emailSender)
            {
                ControllerContext = controllerContext,
                Url = mockUrlHelper.Object,
            };

            // Act
            var email = "test@gomsle.com";
            var result = await controller.Register(new()
            {
                Email = email,
                Password = "itsaseasyas123",
                ReturnUrl = "http://gomsle.com",
            });

            // Assert
            Assert.NotNull(result);

            var okResult = result as OkObjectResult;

            Assert.NotNull(okResult);

            var response = okResult!.Value as IResponse<DynamoDbUser>;

            Assert.NotNull(response);
            Assert.True(response!.IsValid);
            Assert.Empty(response.Errors);
            Assert.Equal(email, response!.Result!.Email);
        }
    }
}