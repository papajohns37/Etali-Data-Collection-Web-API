using System.ComponentModel.DataAnnotations;

namespace Etali_Data_Collection_Web_API.Models.Auth;

/// <summary>
/// Request model for user login.
/// </summary>
/// <param name="Username">User's username.</param>
/// <param name="Password">User's password.</param>
public record LoginRequest(
    [Required]
    [MinLength(3)]
    [MaxLength(50)]
    string Username,
    
    [Required]
    string Password
);
