using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Gomsle.Api.Infrastructure;

public abstract class ApiControllerBase : Controller
{
    public IActionResult Respond<T>(T result)
        where T : new()
    {
        if (typeof(IdentityResult).IsAssignableFrom(typeof(T)))
        {
            var identityResult = result as IdentityResult;

            if (identityResult!.Succeeded)
            {
                return Ok(new Response());
            }
            else
            {
                return BadRequest(new Response
                {
                    Errors = identityResult!.Errors.Select(x => new Error
                    {
                        Code = x.Code,
                        Message = x.Description,
                    }),
                });
            }
        }
        else if (typeof(ModelStateDictionary).IsAssignableFrom(typeof(T)))
        {
            var modelState = result as ModelStateDictionary;

            if (modelState!.IsValid)
            {
                return Ok(new Response());
            }
            else
            {
                return BadRequest(new Response
                {
                    Errors = modelState
                        .Where(x => x.Value != default)
                        .SelectMany(x => x.Value!.Errors.Select(y => new Error
                        {
                            Code = x.Key,
                            PropertyName = x.Key,
                            Message = y.ErrorMessage,
                        })),
                });
            }
        }

        return Ok(new Response<T>
        {
            Result = result,
        });
    }
}