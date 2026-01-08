using Etali_Data_Collection_Web_API.Models.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Etali.Tests.Integration.TestHelpers;

/// <summary>
/// Extension methods for HttpClient to simplify authentication in tests.
/// </summary>
public static class HttpClientAuthExtensions
{
    /// <summary>
    /// Adds test authentication headers to an HttpClient for a specific user.
    /// </summary>
    /// <param name="client">The HttpClient to authenticate.</param>
    /// <param name="factory">The factory to get user manager from.</param>
    /// <param name="username">The username of the user to authenticate as.</param>
    public static async Task AuthenticateAs(
        this HttpClient client, 
        CustomWebApplicationFactory factory, 
        string username)
    {
        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByNameAsync(username);
        
        if (user == null)
        {
            throw new InvalidOperationException($"User {username} not found. Ensure database is seeded.");
        }
        
        var roles = await userManager.GetRolesAsync(user);
        var rolesString = string.Join(",", roles);
        
        // Set test auth header: "UserId:Username:Roles"
        client.DefaultRequestHeaders.Remove("X-Test-Auth");
        client.DefaultRequestHeaders.Add("X-Test-Auth", $"{user.Id}:{username}:{rolesString}");
    }
    
    /// <summary>
    /// Removes authentication headers from an HttpClient.
    /// </summary>
    public static void RemoveAuthentication(this HttpClient client)
    {
        client.DefaultRequestHeaders.Remove("X-Test-Auth");
        client.DefaultRequestHeaders.Remove("Authorization");
    }
}
