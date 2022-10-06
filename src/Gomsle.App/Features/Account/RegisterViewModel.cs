using System.ComponentModel.DataAnnotations;

namespace Gomsle.App.Features.Account;

public class RegisterViewModel
{
    [Required]
    [EmailAddress]
    [Display(Name = "Email")]
    public string? Email { get; set; }

    [Required]
    [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string? Password { get; set; }

    [Required]
    [Compare("Password", ErrorMessage = "Password and Confirm Password must match")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm Password")]
    public string? ConfirmPassword { get; set; }

    public string? ReturnUrl { get; set; }

    public bool EmailIsReadOnly { get; set; }
}