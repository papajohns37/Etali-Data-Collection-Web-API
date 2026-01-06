using Microsoft.AspNetCore.Identity;

namespace Etali_Data_Collection_Web_API.Models.Auth;

/// <summary>
/// Represents an application user with authentication capabilities.
/// Extends IdentityUser to support ASP.NET Core Identity features.
/// </summary>
public class ApplicationUser : IdentityUser
{
    /// <summary>
    /// Gets or sets the UTC timestamp when the user account was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
