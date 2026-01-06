namespace Etali_Data_Collection_Web_API.Models.Auth;

/// <summary>
/// Response model for successful login.
/// </summary>
/// <param name="Token">JWT bearer token for authentication.</param>
/// <param name="Expiration">Token expiration timestamp in UTC.</param>
/// <param name="Email">User's email address.</param>
/// <param name="Roles">User's assigned roles.</param>
public record LoginResponse(
    string Token,
    DateTime Expiration,
    string Email,
    string[] Roles
);
