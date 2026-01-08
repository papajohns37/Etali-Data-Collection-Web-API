using System.ComponentModel.DataAnnotations;

namespace Etali_Data_Collection_Web_API.Models.Auth;

/// <summary>
/// Request model for user registration.
/// Only administrators can register new users.
/// </summary>
public class RegisterRequest
{
    /// <summary>
    /// Username for the new user.
    /// </summary>
    [Required]
    [MinLength(3)]
    [MaxLength(50)]
    [RegularExpression(@"^[a-zA-Z0-9\-._]+$", ErrorMessage = "Username can only contain letters, numbers, hyphens, periods, and underscores")]
    public required string Username { get; set; }
    
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
