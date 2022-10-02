using AspNetCore.Identity.AmazonDynamoDB;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Logging;
using OpenIddict.Abstractions;
using OpenIddict.AmazonDynamoDB;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Gomsle;

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
        services
            .AddIdentity<DynamoDbUser, DynamoDbRole>(options =>
            {
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequiredUniqueChars = 3;
                options.Password.RequiredLength = 6;
                options.User.RequireUniqueEmail = true;
            })
            .AddDynamoDbStores();

        services
            .Configure<IdentityOptions>(options =>
            {
                options.ClaimsIdentity.UserNameClaimType = OpenIddictConstants.Claims.Name;
                options.ClaimsIdentity.UserIdClaimType = OpenIddictConstants.Claims.Subject;
                options.ClaimsIdentity.RoleClaimType = OpenIddictConstants.Claims.Role;
                options.ClaimsIdentity.EmailClaimType = OpenIddictConstants.Claims.Email;
            });

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
                    .SetLogoutEndpointUris("/connect/logout")
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

        if (Environment.IsDevelopment())
        {
            IdentityModelEventSource.ShowPII = true; 
        }

        services
            .AddHealthChecks();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        DynamoDbSetup.EnsureInitialized(app.ApplicationServices);
        OpenIddictDynamoDbSetup.EnsureInitialized(app.ApplicationServices);

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
        app.UseStaticFiles();
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