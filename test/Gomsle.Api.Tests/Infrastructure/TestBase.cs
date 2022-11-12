using System.Security.Claims;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using AspNetCore.Identity.AmazonDynamoDB;
using Gomsle.Api.Features.Account;
using Gomsle.Api.Features.Application;
using Gomsle.Api.Features.Email;
using Gomsle.Api.Features.User;
using Gomsle.Api.Infrastructure;
using Gomsle.Api.Infrastructure.Extensions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using OpenIddict.Abstractions;
using OpenIddict.AmazonDynamoDB;
using OpenIddict.Server;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

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
        request.Setup(x => x.Host).Returns(HostString.FromUriComponent("gomsle.com"));
        request.Setup(x => x.PathBase).Returns(PathString.FromUriComponent("/api"));
        Mocks.Add(request);

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
        serviceCollection.AddSingleton<SignInManager<DynamoDbUser>, MockSignInManager>();
        serviceCollection.Configure<IdentityOptions>(options =>
        {
            options.ClaimsIdentity.UserIdClaimType = Claims.Subject;
        });

        if (configure != default)
        {
            configure(serviceCollection);
        }

        var services = serviceCollection.BuildServiceProvider();

        httpContext.Setup(x => x.RequestServices).Returns(services);
        httpContext.Setup(x => x.User).Returns(() =>
        {
            var signInManager = services.GetRequiredService<SignInManager<DynamoDbUser>>() as MockSignInManager;
            if (signInManager?.CurrentUser == default)
            {
                return default(ClaimsPrincipal)!;
            }

            return signInManager.CreateClaimsPrincipal(signInManager.CurrentUser);
        });

        return services;
    }

    protected async Task MediatorTest(Func<IMediator, IServiceProvider, Task> assert)
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            var serviceProvider = GetServiceProvider(services =>
                services.AddSingleton<IAmazonDynamoDB>(database.Client));

            AspNetCoreIdentityDynamoDbSetup.EnsureInitialized(serviceProvider);
            OpenIddictDynamoDbSetup.EnsureInitialized(serviceProvider);
            DynamoDbSetup.EnsureInitialized(serviceProvider);

            var mediator = serviceProvider.GetRequiredService<IMediator>();

            try
            {
                await assert(mediator, serviceProvider);
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
            DynamoDbSetup.EnsureInitialized(serviceProvider);

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

    protected async Task<DynamoDbUser> CreateAndLoginValidUser(IServiceProvider services)
    {
        var mediator = services.GetRequiredService<IMediator>();
        var userManager = services.GetRequiredService<UserManager<DynamoDbUser>>();
        var email = "valid@gomsle.com";
        var password = "itsaseasyas123";
        var user = new DynamoDbUser
        {
            Email = email,
            UserName = email,
            EmailConfirmed = true,
            TwoFactorEnabled = false,
        };
        await userManager.CreateAsync(user, password);
        await mediator.Send(new LoginCommand.Command
        {
            Email = email,
            Password = password,
            RememberMe = false,
        });
        var httpContext = GetMock<HttpContext>();
        var featureCollection = new FeatureCollection();
        featureCollection.Set(new OpenIddictServerAspNetCoreFeature
        {
            Transaction = new OpenIddictServerTransaction
            {
                Request = new OpenIddictRequest
                {
                    Scope = "test",
                },
            },
        });
        httpContext!.Setup(x => x.Features).Returns(featureCollection);

        return user;
    }

    protected async Task<AccountModel> CreateAccount(IServiceProvider services, Dictionary<string, AccountRole>? members = default)
    {
        var database = services.GetRequiredService<IAmazonDynamoDB>();
        var context = new DynamoDBContext(database);

        var accountName = "Microsoft";
        var account = new AccountModel
        {
            Name = accountName,
            NormalizedName = accountName.UrlFriendly(),
            Members = members ?? new(),
        };
        await context.SaveAsync(account);

        return account;
    }
}
