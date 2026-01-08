using System.Net;
using System.Net.Http.Json;
using Etali_Data_Collection_Web_API.Models;
using Etali_Data_Collection_Web_API.Services;
using Etali.Tests.Integration.TestHelpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Xunit;

namespace Etali.Tests.Integration;

/// <summary>
/// Integration tests for POST /decklist endpoint.
/// </summary>
[Trait("Category", "Integration")]
public class DecklistEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public DecklistEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PostDecklist_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsync("/decklist?link=https://test.com&name=NewDeck", null);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostDecklist_WithMockService_ReturnsOk()
    {
        // Arrange - Ensure database is seeded first
        await _factory.CreateAuthenticatedClientAsync(); // This seeds the database
        
        // Create a factory with mocked DecklistAddingService
        var mockService = new Mock<IDecklistAddingService>();
        mockService.Setup(s => s.AddDecklist(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var factory = _factory.WithMockedServices(services =>
        {
            services.RemoveAll<IDecklistAddingService>();
            services.AddScoped(_ => mockService.Object);
        });

        // Create authenticated client from modified factory
        var client = factory.CreateClient();
        await client.AuthenticateAs(_factory, CustomWebApplicationFactory.TestUsername);

        // Act
        var response = await client.PostAsync("/decklist?link=https://test.com&name=NewDeck", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.Equal("Decklist added successfully", result.GetProperty("message").GetString());

        mockService.Verify(s => s.AddDecklist("https://test.com", "NewDeck"), Times.Once);
    }

    [Fact]
    public async Task PostDecklist_MissingParameters_ReturnsBadRequest()
    {
        // Arrange
        var client = await _factory.CreateAuthenticatedClientAsync();

        // Act
        var response = await client.PostAsync("/decklist", null);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostDecklist_ServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange - Ensure database is seeded first
        await _factory.CreateAuthenticatedClientAsync(); // This seeds the database
        
        // Mock service that throws
        var mockService = new Mock<IDecklistAddingService>();
        mockService.Setup(s => s.AddDecklist(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("Moxfield error"));

        var factory = _factory.WithMockedServices(services =>
        {
            services.RemoveAll<IDecklistAddingService>();
            services.AddScoped(_ => mockService.Object);
        });

        // Create authenticated client from modified factory
        var client = factory.CreateClient();
        await client.AuthenticateAs(_factory, CustomWebApplicationFactory.TestUsername);

        // Act
        var response = await client.PostAsync("/decklist?link=https://test.com&name=TestDeck", null);

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    public async Task PostDecklist_MissingLinkParameter_ReturnsBadRequest()
    {
        // Arrange
        var client = await _factory.CreateAuthenticatedClientAsync();

        // Act
        var response = await client.PostAsync("/decklist?name=TestDeck", null);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostDecklist_MissingNameParameter_ReturnsBadRequest()
    {
        // Arrange
        var client = await _factory.CreateAuthenticatedClientAsync();

        // Act
        var response = await client.PostAsync("/decklist?link=https://test.com", null);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
