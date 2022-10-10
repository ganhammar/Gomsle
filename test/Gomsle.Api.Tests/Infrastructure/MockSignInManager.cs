using AspNetCore.Identity.AmazonDynamoDB;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
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

    public override Task<SignInResult> PasswordSignInAsync(string userName, string password, bool isPersistent, bool lockoutOnFailure)
    {
        if (userName == "valid@gomsle.com" && password == "itsaseasyas123")
        {
            return Task.FromResult(SignInResult.Success);
        }

        return Task.FromResult(SignInResult.Failed);
    }
}