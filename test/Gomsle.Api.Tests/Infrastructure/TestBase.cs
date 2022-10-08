using Amazon.DynamoDBv2;
using AspNetCore.Identity.AmazonDynamoDB;
using Gomsle.Api.Features.Email;
using Gomsle.Api.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using OpenIddict.AmazonDynamoDB;

namespace Gomsle.Api.Tests.Infrastructure;

public abstract class TestBase
{
    private List<Mock> Mocks { get; set; } = new List<Mock>();

    protected Mock<T>? GetMock<T>()
        where T : class
    {
        return Mocks
            .Where(x => x.GetType().IsGenericType)
            .Where(x => x.GetType()
                .GetGenericArguments()
                .First()
                .IsAssignableFrom(typeof(T)))
            .FirstOrDefault() as Mock<T>;
    }

    protected IServiceProvider GetServiceProvider(Action<IServiceCollection>? configure = default)
    {
        var emailMock = new Mock<IEmailSender>();
        Mocks.Add(emailMock);

        var request = new Mock<HttpRequest>();
        request.Setup(x => x.Scheme).Returns("http");
        request.Setup(x => x.Host).Returns(HostString.FromUriComponent("http://gomsle.com"));
        request.Setup(x => x.PathBase).Returns(PathString.FromUriComponent("/api"));

        var httpContext = new Mock<HttpContext>();
        httpContext.Setup(x => x.Request).Returns(request.Object);
        Mocks.Add(httpContext);

        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        httpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext.Object);
        Mocks.Add(httpContextAccessor);

        var serviceCollection = new ServiceCollection();
        serviceCollection
            .AddOpenIddict()
            .AddCore()
            .UseDynamoDb();
        serviceCollection.AddSingleton<IEmailSender>(emailMock.Object);
        serviceCollection.AddControllers();
        serviceCollection.AddSingleton<IHttpContextAccessor>(httpContextAccessor.Object);
        serviceCollection.AddIdentity();
        serviceCollection.AddMediatR();

        if (configure != default)
        {
            configure(serviceCollection);
        }

        return serviceCollection.BuildServiceProvider();
    }

    protected async Task MediatorTest<T>(IRequest<T> message, Action<T> assert)
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            var serviceProvider = GetServiceProvider(services =>
                services.AddSingleton<IAmazonDynamoDB>(database.Client));

            AspNetCoreIdentityDynamoDbSetup.EnsureInitialized(serviceProvider);
            OpenIddictDynamoDbSetup.EnsureInitialized(serviceProvider);

            var mediator = serviceProvider.GetRequiredService<IMediator>();
            
            try
            {
                var response = await mediator.Send(message);
                assert(response);
            }
            catch(Exception)
            {
                database.Dispose();
                throw;
            }
        }
    }

    protected async Task ControllerTest<T>(Func<IServiceProvider, object[]> getParams, Func<T, IServiceProvider, Task> actAndAssert)
        where T : Controller
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            var serviceProvider = GetServiceProvider(services =>
                services.AddSingleton<IAmazonDynamoDB>(database.Client));

            AspNetCoreIdentityDynamoDbSetup.EnsureInitialized(serviceProvider);
            OpenIddictDynamoDbSetup.EnsureInitialized(serviceProvider);

            var httpContext = GetMock<HttpContext>();
            var controllerContext = new ControllerContext
            {
                HttpContext = httpContext!.Object,
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
                await actAndAssert(controller, serviceProvider);
            }
            catch(Exception)
            {
                database.Dispose();
                throw;
            }
        }
    }
}
