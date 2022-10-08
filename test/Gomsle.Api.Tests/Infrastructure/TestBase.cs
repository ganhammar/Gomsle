using Amazon.DynamoDBv2;
using AspNetCore.Identity.AmazonDynamoDB;
using Gomsle.Api.Features.Email;
using Gomsle.Api.Infrastructure;
using Gomsle.Api.Tests.Features.Email;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using OpenIddict.AmazonDynamoDB;

namespace Gomsle.Api.Tests.Infrastructure;

public abstract class TestBase
{
    public IServiceProvider GetServiceProvider(Action<IServiceCollection> configure)
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection
            .AddOpenIddict()
            .AddCore()
            .UseDynamoDb();
        serviceCollection.AddIdentity();
        serviceCollection.AddSingleton<IEmailSender, MockEmailSender>();
        serviceCollection.AddControllers();

        configure(serviceCollection);

        return serviceCollection.BuildServiceProvider();
    }

    public async Task ControllerTest<T>(Func<IServiceProvider, object[]> getParams, Func<T, Task> actAndAssert)
        where T : Controller
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            var serviceProvider = GetServiceProvider(services =>
                services.AddSingleton<IAmazonDynamoDB>(database.Client));

            AspNetCoreIdentityDynamoDbSetup.EnsureInitialized(serviceProvider);
            OpenIddictDynamoDbSetup.EnsureInitialized(serviceProvider);

            var request = new Mock<HttpRequest>();
            request.Setup(x => x.Scheme).Returns("http");
            request.Setup(x => x.Host).Returns(HostString.FromUriComponent("http://gomsle.com"));
            request.Setup(x => x.PathBase).Returns(PathString.FromUriComponent("/api"));

            var httpContext = new Mock<HttpContext>();
            httpContext.Setup(x => x.Request).Returns(request.Object);
            httpContext.Setup(x => x.RequestServices).Returns(serviceProvider);

            var controllerContext = new ControllerContext
            {
                HttpContext = httpContext.Object,
            };

            var mockUrlHelper = new Mock<IUrlHelper>();
            mockUrlHelper
                .Setup(x => x.Link(It.IsAny<string>(), It.IsAny<object>()))
                .Returns("http://some-url.com");

            var args = getParams(serviceProvider);
            var argTypes = args.Select(x => x.GetType()).ToArray();

            var constructor = typeof(T).GetConstructor(argTypes);
            var controller = constructor!.Invoke(args) as T;
            controller!.ControllerContext = controllerContext;
            controller!.Url = mockUrlHelper.Object;

            try
            {
                await actAndAssert(controller);
            }
            catch(Exception)
            {
                database.Dispose();
                throw;
            }
        }
    }
}
