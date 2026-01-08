using System.Net.Http.Headers;

namespace Etali.Tests.Integration.TestHelpers;

/// <summary>
/// Base class for integration tests that provides common authentication and setup functionality.
/// Ensures proper database seeding and provides authenticated HTTP clients.
/// </summary>
public abstract class AuthenticatedTestBase : IClassFixture<CustomWebApplicationFactory>
{
    protected readonly CustomWebApplicationFactory Factory;
    protected readonly HttpClient Client;

    protected AuthenticatedTestBase(CustomWebApplicationFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
    }

    /// <summary>
    /// Creates an authenticated HTTP client for testing protected endpoints.
    /// Automatically ensures the database is seeded before returning the client.
    /// </summary>
    /// <param name="useAdmin">If true, creates a client authenticated as an admin user.</param>
    /// <returns>An authenticated HttpClient with proper test authentication headers.</returns>
    protected async Task<HttpClient> GetAuthenticatedClientAsync(bool useAdmin = false)
    {
        return await Factory.CreateAuthenticatedClientAsync(useAdmin);
    }

    /// <summary>
    /// Creates an unauthenticated HTTP client for testing public endpoints.
    /// Still ensures database is seeded for consistent test data.
    /// </summary>
    protected async Task<HttpClient> GetClientAsync()
    {
        // Ensure database is seeded even for unauthenticated tests
        await Factory.CreateAuthenticatedClientAsync();
        return Client;
    }
}
