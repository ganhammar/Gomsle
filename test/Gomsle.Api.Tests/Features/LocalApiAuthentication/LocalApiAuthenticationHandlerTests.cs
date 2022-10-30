using System.Net;
using Amazon.DynamoDBv2;
using Gomsle.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Gomsle.Api.Tests.Features.LocalApiAuthentication;

public class LocalApiAuthenticationHandlerTests
{
    [Fact]
    public async Task Should_ReturnUnauthorized_When_UserIsNotAuthenticated()
    {
        using (var database = DynamoDbLocalServerUtils.CreateDatabase())
        {
            var host = new HostBuilder()
                .ConfigureWebHost(webBuilder =>
                {
                    webBuilder.UseEnvironment("Development");
                    webBuilder.UseStartup<Startup>();
                    webBuilder.ConfigureTestServices(services =>
                    {
                        services.AddSingleton<IAmazonDynamoDB>(database.Client);
                    });
                    webBuilder.UseTestServer();
                })
                .Build();
            
            await host.StartAsync();

            var client = host.GetTestServer().CreateClient();

            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("Name", "Microsoft"),
            });

            var response = await client.PostAsync("/account/create", content);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}