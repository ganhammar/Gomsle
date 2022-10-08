using System.ComponentModel.DataAnnotations;

namespace Gomsle.Api.Features.Account;

public class RegisterDto
{
    [Required]
    [EmailAddress]
    public string? Email { get; set; }
    [Required]
    public string? Password { get; set; }
    [Required]
    public string? ReturnUrl { get; set; }
}