using System.ComponentModel.DataAnnotations;

namespace Etali_Data_Collection_Web_API.Models.Auth;

/// <summary>
/// Request model for user registration.
/// Only administrators can register new users.
/// </summary>
public class RegisterRequest
{
    /// <summary>
    /// Email address for the new user.
    /// </summary>
    [Required]
    [EmailAddress]
    public required string Email { get; set; }
    
    /// <summary>
    /// Password for the new user.
    /// </summary>
    [Required]
    [MinLength(6)]
    public required string Password { get; set; }
    
    /// <summary>
    /// Password confirmation (must match Password).
    /// </summary>
    [Required]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match")]
    public required string ConfirmPassword { get; set; }
    
    /// <summary>
    /// Whether to assign the Admin role (default: false, assigns Contributor role).
    /// </summary>
    public bool IsAdmin { get; set; } = false;
}
