using System.Net;
using Etali.Tests.Fixtures;
using Etali_Data_Collection_Web_API;
using Etali_Data_Collection_Web_API.Configuration;
using Etali_Data_Collection_Web_API.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;

namespace Etali.Tests.Integration;

/// <summary>
/// Unit tests for DecklistAddingService JSON parsing logic.
/// NOTE: These tests require DecklistAddingService refactoring to accept IHttpClientFactory
/// for proper HTTP mocking. Currently marked as External until refactoring is complete.
/// 
/// TODO: Refactor DecklistAddingService to use IHttpClientFactory instead of creating HttpClient internally.
/// </summary>
[Trait("Category", "External")]
public class DecklistAddingServiceParsingTests : IDisposable
{
    // Tests moved to External category until service refactoring is complete
    // to allow HTTP client injection for proper mocking
    
    private readonly Mock<ILogger<DecklistAddingService>> _mockLogger;
    private readonly Mock<IRateLimiter> _mockRateLimiter;
    private readonly MoxfieldConfiguration _config;
    private readonly EtaliDatabase _database;

    public DecklistAddingServiceParsingTests()
    {
        _mockLogger = new Mock<ILogger<DecklistAddingService>>();
        _mockRateLimiter = new Mock<IRateLimiter>();
        _config = new MoxfieldConfiguration
        {
            BaseUrl = "https://api2.moxfield.com/v3/decks/all/",
            ApiKey = "test-api-key"
        };

        var options = new DbContextOptionsBuilder<EtaliDatabase>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _database = new EtaliDatabase(options);
    }

    // Tests are temporarily disabled - require service refactoring to inject HttpClient
    // See: DecklistAddingService.GetDecklistAsync method

    public void Dispose()
    {
        _database.Dispose();
    }
}
