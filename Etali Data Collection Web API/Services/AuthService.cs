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
    public async Task<(bool Success, string[] Errors)> RegisterUserAsync(string email, string password, bool isAdmin)
    {
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true, // Auto-confirm for simplicity
            CreatedAt = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(user, password);

        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToArray();
            logger.LogWarning("Failed to register user {Email}: {Errors}", email, string.Join(", ", errors));
            return (false, errors);
        }

        // Assign role
        var role = isAdmin ? AdminRole : ContributorRole;
        var roleResult = await userManager.AddToRoleAsync(user, role);

        if (!roleResult.Succeeded)
        {
            var errors = roleResult.Errors.Select(e => e.Description).ToArray();
            logger.LogError("Failed to assign role {Role} to user {Email}: {Errors}", role, email, string.Join(", ", errors));
            // User was created but role assignment failed - consider this a failure
            await userManager.DeleteAsync(user);
            return (false, errors);
        }

        logger.LogInformation("User {Email} registered successfully with role {Role}", email, role);
        return (true, []);
    }

    /// <inheritdoc/>
    public async Task<LoginResponse?> LoginAsync(string email, string password)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            logger.LogWarning("Login attempt failed: User {Email} not found", email);
            return null;
        }

        var result = await signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: false);
        if (!result.Succeeded)
        {
            logger.LogWarning("Login attempt failed: Invalid password for user {Email}", email);
            return null;
        }

        // Get user roles
        var roles = await userManager.GetRolesAsync(user);
        
        // Generate JWT token
        var token = GenerateJwtToken(user, roles);
        var expiration = DateTime.UtcNow.AddDays(jwtConfig.ExpirationInDays);

        logger.LogInformation("User {Email} logged in successfully", email);

        return new LoginResponse(
            Token: token,
            Expiration: expiration,
            Email: user.Email!,
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
            new(ClaimTypes.Name, user.Email!),
            new(ClaimTypes.Email, user.Email!),
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email!),
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
