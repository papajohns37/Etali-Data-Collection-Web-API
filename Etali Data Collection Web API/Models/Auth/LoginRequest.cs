using System.ComponentModel.DataAnnotations;

namespace Etali_Data_Collection_Web_API.Models.Auth;

/// <summary>
/// Request model for user login.
/// </summary>
/// <param name="Email">User's email address.</param>
/// <param name="Password">User's password.</param>
public record LoginRequest(
    [Required]
    [EmailAddress]
    string Email,
    
    [Required]
    string Password
);
