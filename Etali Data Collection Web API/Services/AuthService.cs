using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Etali_Data_Collection_Web_API.Configuration;
using Etali_Data_Collection_Web_API.Models.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace Etali_Data_Collection_Web_API.Services;

/// <summary>
/// Service for user authentication and JWT token generation.
/// </summary>
public class AuthService(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    JwtConfiguration jwtConfig,
    ILogger<AuthService> logger) : IAuthService
{
    private const string AdminRole = "Admin";
    private const string ContributorRole = "Contributor";

    /// <inheritdoc/>
    public async Task<(bool Success, string[] Errors)> RegisterUserAsync(string username, string password, bool isAdmin)
    {
        var user = new ApplicationUser
        {
            UserName = username,
            Email = null,
            EmailConfirmed = false,
            CreatedAt = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(user, password);

        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToArray();
            logger.LogWarning("Failed to register user {Username}: {Errors}", username, string.Join(", ", errors));
            return (false, errors);
        }

        // Assign role
        var role = isAdmin ? AdminRole : ContributorRole;
        var roleResult = await userManager.AddToRoleAsync(user, role);

        if (!roleResult.Succeeded)
        {
            var errors = roleResult.Errors.Select(e => e.Description).ToArray();
            logger.LogError("Failed to assign role {Role} to user {Username}: {Errors}", role, username, string.Join(", ", errors));
            // User was created but role assignment failed - consider this a failure
            await userManager.DeleteAsync(user);
            return (false, errors);
        }

        logger.LogInformation("User {Username} registered successfully with role {Role}", username, role);
        return (true, []);
    }

    /// <inheritdoc/>
    public async Task<LoginResponse?> LoginAsync(string username, string password)
    {
        // Validate inputs
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("Login attempt failed: Missing username or password");
            return null;
        }

        var user = await userManager.FindByNameAsync(username);
        if (user == null)
        {
            logger.LogWarning("Login attempt failed: User {Username} not found", username);
            return null;
        }

        var result = await signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: false);
        if (!result.Succeeded)
        {
            logger.LogWarning("Login attempt failed: Invalid password for user {Username}", username);
            return null;
        }

        // Get user roles
        var roles = await userManager.GetRolesAsync(user);
        
        // Generate JWT token
        var token = GenerateJwtToken(user, roles);
        var expiration = DateTime.UtcNow.AddDays(jwtConfig.ExpirationInDays);

        logger.LogInformation("User {Username} logged in successfully", username);

        return new LoginResponse(
            Token: token,
            Expiration: expiration,
            Username: user.UserName!,
            UserId: user.Id,
            Roles: [.. roles]
        );
    }

    /// <summary>
    /// Generates a JWT token for the authenticated user.
    /// </summary>
    private string GenerateJwtToken(ApplicationUser user, IList<string> roles)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName!),
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // Add role claims
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfig.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiration = DateTime.UtcNow.AddDays(jwtConfig.ExpirationInDays);

        var token = new JwtSecurityToken(
            issuer: jwtConfig.Issuer,
            audience: jwtConfig.Audience,
            claims: claims,
            expires: expiration,
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
