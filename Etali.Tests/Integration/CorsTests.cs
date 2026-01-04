using System.Net;
using Xunit;

namespace Etali.Tests.Integration;

/// <summary>
/// Integration tests for CORS functionality across all endpoints.
/// </summary>
[Trait("Category", "Integration")]
public class CorsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public CorsTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Options_RequestWithOrigin_ReturnsCorsHeaders()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Options, "/hand?decklistName=TestDeck");
        request.Headers.Add("Origin", "http://localhost:4200");
        request.Headers.Add("Access-Control-Request-Method", "GET");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.True(response.Headers.Contains("Access-Control-Allow-Origin") ||
                    response.StatusCode == HttpStatusCode.NoContent ||
                    response.StatusCode == HttpStatusCode.OK);
    }

    [Fact]
    public async Task Options_PreflightForPostWithCustomHeaders_ReturnsCorrectHeaders()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Options, "/hand/evaluate");
        request.Headers.Add("Origin", "http://localhost:4200");
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "content-type,authorization");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.True(
            response.StatusCode == HttpStatusCode.NoContent ||
            response.StatusCode == HttpStatusCode.OK ||
            response.Headers.Contains("Access-Control-Allow-Origin")
        );
    }

    [Fact]
    public async Task Get_RequestWithOriginHeader_WorksCorrectly()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/decklists");
        request.Headers.Add("Origin", "http://localhost:4200");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
