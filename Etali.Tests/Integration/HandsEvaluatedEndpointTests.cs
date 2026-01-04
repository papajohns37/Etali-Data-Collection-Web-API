using System.Net;
using System.Net.Http.Json;
using Etali_Data_Collection_Web_API.Models;
using Xunit;

namespace Etali.Tests.Integration;

/// <summary>
/// Integration tests for GET /hands/evaluated endpoint.
/// </summary>
[Trait("Category", "Integration")]
public class HandsEvaluatedEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public HandsEvaluatedEndpointTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetHandsEvaluated_ReturnsOkWithArray()
    {
        // Act
        var response = await _client.GetAsync("/hands/evaluated");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var evaluations = await response.Content.ReadFromJsonAsync<List<EvaluatedHand>>();
        Assert.NotNull(evaluations);
    }
}
