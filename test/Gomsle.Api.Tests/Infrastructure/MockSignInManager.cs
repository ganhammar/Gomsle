using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using AspNetCore.Identity.AmazonDynamoDB;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Gomsle.Api.Tests.Infrastructure;

public class MockSignInManager : SignInManager<DynamoDbUser>
{
    public MockSignInManager(
            UserManager<DynamoDbUser> userManager,
            IHttpContextAccessor contextAccessor,
            IUserClaimsPrincipalFactory<DynamoDbUser> claimsFactory,
            IOptions<IdentityOptions> optionsAccessor,
            ILogger<SignInManager<DynamoDbUser>> logger,
            IAuthenticationSchemeProvider schemes,
            IUserConfirmation<DynamoDbUser> confirmation)
        : base(userManager, contextAccessor, claimsFactory, optionsAccessor, logger, schemes, confirmation)
    {
    }

    private bool _signInRequetInProgress = false;

    public override Task<SignInResult> PasswordSignInAsync(string userName, string password, bool isPersistent, bool lockoutOnFailure)
    {
        if (userName == "valid@gomsle.com" && password == "itsaseasyas123")
        {
            _signInRequetInProgress = true;
            return Task.FromResult(SignInResult.Success);
        }

        return Task.FromResult(SignInResult.Failed);
    }

    public override async Task<SignInResult> TwoFactorSignInAsync(string provider, string code, bool isPersistent, bool rememberClient)
    {
        var user = await GetTwoFactorAuthenticationUserAsync();
        if (user == default)
        {
            return SignInResult.Failed;
        }

        var result = await UserManager.VerifyTwoFactorTokenAsync(user, provider, code);
        return result ? SignInResult.Success : SignInResult.Failed;
    }

    public override async Task<DynamoDbUser> GetTwoFactorAuthenticationUserAsync()
    {
        if (!_signInRequetInProgress)
        {
            return default!;
        }

        var database = base.Context.RequestServices.GetRequiredService<IAmazonDynamoDB>();
        var context = new DynamoDBContext(database);
        var scan = context.ScanAsync<DynamoDbUser>(new List<ScanCondition>());
        var users = await scan.GetNextSetAsync();

        return users.First();
    }
}