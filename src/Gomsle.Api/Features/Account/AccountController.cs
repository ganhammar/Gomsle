using AspNetCore.Identity.AmazonDynamoDB;
using Gomsle.Api.Features.Email;
using Gomsle.Api.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Gomsle.Api.Features.Account;

public class AccountController : ApiControllerBase
{
    private readonly UserManager<DynamoDbUser> _userManager;
    private readonly SignInManager<DynamoDbUser> _signInManager;
    private readonly IEmailSender _emailSender;

    public AccountController(
        UserManager<DynamoDbUser> userManager,
        SignInManager<DynamoDbUser> signInManager,
        IEmailSender emailSender)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _emailSender = emailSender;
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Register(RegisterDto model)
    {
        if (ModelState.IsValid)
        {
            var user = new DynamoDbUser
            {
                UserName = model.Email,
                Email = model.Email,
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                await SendConfirmationEmail(user, model.ReturnUrl);
                return Respond(user);
            }
            else
            {
                return Respond(result);
            }
        }
        return Respond(ModelState);
    }

    private async Task SendConfirmationEmail(DynamoDbUser user, string? returnUrl)
    {
        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var url = Url.Action(
            "confirm",
            "account",
            new { UserId = user.Id, Token = token, ReturnUrl = returnUrl },
            Request.Scheme);

        var body = $"Follow the link below to confirm your Gömsle account:<br /><a href=\"{url}\">{url}</a>";

        await _emailSender.Send(user.Email, "Confirm account", body);
    }
}