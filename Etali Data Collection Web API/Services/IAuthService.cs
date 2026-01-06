using Etali_Data_Collection_Web_API.Models.Auth;

namespace Etali_Data_Collection_Web_API.Services;

/// <summary>
/// Service interface for user authentication and registration.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Registers a new user with the specified email and password.
    /// </summary>
    /// <param name="email">User's email address.</param>
    /// <param name="password">User's password.</param>
    /// <param name="isAdmin">Whether to assign Admin role (true) or Contributor role (false).</param>
    /// <returns>Result containing success status and any error messages.</returns>
    Task<(bool Success, string[] Errors)> RegisterUserAsync(string email, string password, bool isAdmin);
    
    /// <summary>
    /// Authenticates a user and generates a JWT token.
    /// </summary>
    /// <param name="email">User's email address.</param>
    /// <param name="password">User's password.</param>
    /// <returns>LoginResponse if successful, null if authentication fails.</returns>
    Task<LoginResponse?> LoginAsync(string email, string password);
}
