using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Etali.Tests.Integration.TestHelpers;

/// <summary>
/// Helper class for creating web application factories with custom service configurations
/// while preserving test authentication.
/// </summary>
public static class TestFactoryHelper
{
    /// <summary>
    /// Creates a modified WebApplicationFactory with custom services while preserving
    /// the test authentication that was configured in CustomWebApplicationFactory.
    /// This is simpler than trying to reconfigure authentication - we just modify services.
    /// </summary>
    /// <typeparam name="TEntryPoint">The application entry point type.</typeparam>
    /// <param name="factory">The base factory to modify.</param>
    /// <param name="configureServices">Action to configure services (e.g., add mocks).</param>
    /// <returns>A new WebApplicationFactory with custom services and inherited test authentication.</returns>
    public static WebApplicationFactory<TEntryPoint> WithMockedServices<TEntryPoint>(
        this WebApplicationFactory<TEntryPoint> factory,
        Action<IServiceCollection> configureServices) where TEntryPoint : class
    {
        return factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Simply apply custom service configuration
                // The test authentication is inherited from CustomWebApplicationFactory
                configureServices(services);
            });
        });
    }
}
