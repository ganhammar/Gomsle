using OpenIddict.Abstractions;

namespace Gomsle.Api.Infrastructure.Extensions;

public static class HttpContextExtensions
{
    public static async Task<string> GetCurrentApplicationId(this HttpContext httpContext, CancellationToken cancellationToken)
    {
        if (httpContext.Request.Query["ApplicationId"].Any())
        {
            if (string.IsNullOrEmpty(httpContext.Request.Query["ApplicationId"]) == false)
            {
                var applicationId = httpContext.Request.Query["ApplicationId"].ToString();
                httpContext.SetApplicationIdCookie(applicationId);
                return applicationId;
            }
        }

        if (httpContext.Request.Cookies.TryGetValue(Constants.ApplicationCookie, out var applicationIdString))
        {
            if (string.IsNullOrEmpty(applicationIdString) == false)
            {
                return applicationIdString;
            }
        }

        var applicationManager = httpContext.RequestServices
            .GetRequiredService<IOpenIddictApplicationManager>();
        var application = await applicationManager.FindByClientIdAsync(Constants.InternalClientId, cancellationToken);
        var id = (await applicationManager.GetIdAsync(application!, cancellationToken))!;
        httpContext.SetApplicationIdCookie(id);

        return id;
    }

    public static void SetApplicationIdCookie(this HttpContext httpContext, string applicationId)
    {
        httpContext.Response.Cookies.Append(
            Constants.ApplicationCookie,
            applicationId.ToString(),
            new CookieOptions
            {
                Path = "/",
                SameSite = SameSiteMode.Strict,
                HttpOnly = true,
                Secure = true,
            });
    }
}