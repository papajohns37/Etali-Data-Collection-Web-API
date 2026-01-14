namespace Etali_Data_Collection_Web_API.Models.Auth;

/// <summary>
/// Response model for successful login.
/// </summary>
/// <param name="Token">JWT bearer token for authentication.</param>
/// <param name="Expiration">Token expiration timestamp in UTC.</param>
/// <param name="Username">User's username.</param>
/// <param name="UserId">User's unique identifier.</param>
/// <param name="Roles">User's assigned roles.</param>
public record LoginResponse(
    string Token,
    DateTime Expiration,
    string Username,
    string UserId,
    string[] Roles
);
