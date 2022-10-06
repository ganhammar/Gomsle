using AspNetCore.Identity.AmazonDynamoDB;
using Gomsle.App.Features.Email;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Gomsle.App.Features.Account;

public class AccountController : Controller
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

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Register(
        string? email = null, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (email != null)
        {
            var user = await _userManager.FindByEmailAsync(email);

            if (user != default)
            {
                return RedirectToAction("login", new
                {
                    returnUrl,
                    message = "You already have an account with us, login to access the application",
                });
            }
        }

        var model = new RegisterViewModel
        {
            Email = email,
            EmailIsReadOnly = email != null,
        };

        return View(model);
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (ModelState.IsValid)
        {
            var user = new DynamoDbUser
            {
                UserName = model.Email,
                Email = model.Email,
                EmailConfirmed = model.EmailIsReadOnly ? true : false,
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                await SendConfirmationEmail(user, returnUrl);
                return RedirectToAction("login", new { returnUrl, message = "Account created, check your email for a confirmation message" });
            }

            AddErrors(result);
        }

        // If we got this far, something failed.
        return View(model);
    }

    private async Task SendConfirmationEmail(DynamoDbUser user, string? returnUrl)
    {
        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var url = Url.Action(
            "confirm",
            "account",
            new { UserId = user.Id, Token = token, ReturnUrl = returnUrl },
            protocol: Request.Scheme);

        var body = $"Follow the link below to confirm your Huvudloes account:<br /><a href=\"{url}\">{url}</a>";

        await _emailSender.Send(user.Email, "Confirm account", body);
    }

    private void AddErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
    }
}