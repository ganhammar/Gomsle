using Amazon.DynamoDBv2;
using Gomsle.Api.Features.Application;
using Gomsle.Api.Features.Cors;
using Gomsle.Api.Features.Email;
using Gomsle.Api.Features.LocalApiAuthentication;
using Gomsle.Api.Infrastructure;
using Gomsle.Api.Infrastructure.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.IdentityModel.Logging;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Gomsle.Api;

public class Startup
{
    public Startup(IConfiguration configuration, IHostEnvironment environment)
    {
        Configuration = configuration;
        Environment = environment;
    }

    public IConfiguration Configuration { get; }
    public IHostEnvironment Environment { get; }

    public void ConfigureServices(IServiceCollection services)
    {
        EmailSenderOptions emailSenderOptions = new();
        Configuration.GetSection(nameof(EmailSenderOptions)).Bind(emailSenderOptions);
        services.AddSingleton<EmailSenderOptions>(emailSenderOptions);

        var dynamoDbConfig = Configuration.GetSection("DynamoDB");

        services
            .AddDefaultAWSOptions(Configuration.GetAWSOptions())
            .AddSingleton<IAmazonDynamoDB>(_ => new AmazonDynamoDBClient(new AmazonDynamoDBConfig
            {
                ServiceURL = dynamoDbConfig.GetValue<string>("ServiceUrl"),
            }));

        services.AddIdentity();

        services.AddCors();
        services.AddTransient<ICorsPolicyProvider, CorsPolicyProvider>();

        services
            .AddOpenIddict()
            .AddCore(builder =>
            {
                builder.UseDynamoDb();
            })
            .AddServer(builder =>
            {
                builder
                    .SetAuthorizationEndpointUris("/connect/authorize")
                    .SetLogoutEndpointUris("/connect/LogoutCommand")
                    .SetIntrospectionEndpointUris("/connect/introspect")
                    .SetUserinfoEndpointUris("/connect/userinfo")
                    .SetTokenEndpointUris("/connect/token");

                builder.AllowImplicitFlow();
                builder.AllowRefreshTokenFlow();
                builder.AllowClientCredentialsFlow();
                builder.AllowAuthorizationCodeFlow();

                builder.UseReferenceAccessTokens();
                builder.UseReferenceRefreshTokens();

                builder.RegisterScopes(Scopes.Email, Scopes.Profile, Scopes.Roles);

                builder.SetAccessTokenLifetime(TimeSpan.FromMinutes(30));
                builder.SetRefreshTokenLifetime(TimeSpan.FromDays(1));

                if (Environment.IsDevelopment())
                {
                    builder
                        .AddDevelopmentEncryptionCertificate()
                        .AddDevelopmentSigningCertificate();
                }
                
                var aspNetCoreBuilder = builder
                    .UseAspNetCore()
                    .EnableAuthorizationEndpointPassthrough()
                    .EnableLogoutEndpointPassthrough()
                    .EnableUserinfoEndpointPassthrough()
                    .EnableStatusCodePagesIntegration()
                    .EnableTokenEndpointPassthrough();

                if (Environment.IsDevelopment())
                {
                    aspNetCoreBuilder.DisableTransportSecurityRequirement();
                }
            })
            .AddValidation(builder =>
            {
                builder.UseLocalServer();
                builder.UseAspNetCore();
            });

        services
            .Configure<CookiePolicyOptions>(options =>
            {
                options.Secure = CookieSecurePolicy.Always;
                options.HttpOnly = HttpOnlyPolicy.Always;
            });

        services.AddAuthentication()
            .AddScheme<LocalApiAuthenticationOptions, LocalApiAuthenticationHandler>(
                Constants.LocalApiAuthenticationScheme,
                null,
                options =>
                {
                    options.ExpectedScope = Constants.LocalApiScope;
                })
            .AddOpenIdConnect(Constants.FakeOidcHandler, Constants.FakeOidcHandler, _ => {});

        services.AddTransient<IAuthenticationSchemeProvider, ApplicationAuthenticationSchemeProvider>();

        if (Environment.IsDevelopment())
        {
            IdentityModelEventSource.ShowPII = true; 
        }

        services.AddHttpContextAccessor();
        services.AddSingleton<IEmailSender, EmailSender>();
        services.AddMediatR();

        services.AddAuthorization(options =>
        {
            options.AddPolicy(Constants.LocalApiPolicy, policy =>
            {
                policy.AddAuthenticationSchemes(Constants.LocalApiAuthenticationScheme);
                policy.RequireAuthenticatedUser();
                policy.RequireClaim(OpenIddictConstants.Claims.Scope, Constants.LocalApiScope);
            });
        });
        services
            .AddControllers()
            .AddFeatureFolders();
        services.AddHealthChecks();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        DynamoDbSetup.EnsureInitialized(app.ApplicationServices);

        app.UseForwardedHeaders();

        if (Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseStatusCodePagesWithReExecute("/error");
        }

        app.UseCors();
        app.UseCookiePolicy();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseEndpoints(options =>
        {
            options.MapControllers();
            options.MapDefaultControllerRoute();
            options.MapHealthChecks("/health");
        });
    }
}