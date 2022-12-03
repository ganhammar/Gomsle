using Gomsle.Api.Infrastructure.Extensions;

namespace Gomsle.Api.Features.Application;

public class ApplicationMiddleware
{
    private readonly RequestDelegate _next;

    public ApplicationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext httpContext)
    {
        var cancellationToken = httpContext.RequestAborted;
        await httpContext.GetCurrentApplicationId(cancellationToken);
        await _next(httpContext);
    }
}