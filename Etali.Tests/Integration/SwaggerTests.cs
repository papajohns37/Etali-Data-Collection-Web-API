using System.Net;
using Xunit;

namespace Etali.Tests.Integration;

/// <summary>
/// Integration tests for Swagger documentation endpoints.
/// </summary>
[Trait("Category", "Integration")]
public class SwaggerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public SwaggerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Swagger_Documentation_IsAccessible()
    {
        // Act
        var response = await _client.GetAsync("/swagger/v1/swagger.json");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Etali Data Collection API", content);
        Assert.Contains("/hand", content);
        Assert.Contains("/hand/evaluate", content);
        Assert.Contains("/decklists", content);
    }

    [Fact]
    public async Task SwaggerUI_IsAccessible()
    {
        // Act
        var response = await _client.GetAsync("/");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("swagger", content, StringComparison.OrdinalIgnoreCase);
    }
}
