using System.Net.Http;
using System.Text.Json;
using Etali_Data_Collection_Web_API.Configuration;
using Etali_Data_Collection_Web_API.Models;

namespace Etali_Data_Collection_Web_API.Services
{
    /// <summary>
    /// Service for adding decklists from Moxfield.
    /// </summary>
    public class DecklistAddingService : IDecklistAddingService
    {
        private readonly EtaliDatabase _database;
        private readonly ILogger<DecklistAddingService> _logger;
        private readonly MoxfieldConfiguration _moxfieldConfig;
        private readonly IRateLimiter _rateLimiter;

        /// <summary>
        /// Initializes a new instance of the <see cref="DecklistAddingService"/> class.
        /// </summary>
        public DecklistAddingService(
            EtaliDatabase database, 
            ILogger<DecklistAddingService> logger,
            MoxfieldConfiguration moxfieldConfig,
            IRateLimiter rateLimiter)
        {
            _database = database;
            _logger = logger;
            _moxfieldConfig = moxfieldConfig;
            _rateLimiter = rateLimiter;
        }

        /// <inheritdoc/>
        public async Task AddDecklist(string link, string name)
        {
            string json = await GetDecklistAsync(link);
            
            // Parse the JSON and extract mainboard card names
            List<string> cardNames = ExtractMainboardCardNames(json);
            
            // Extract Moxfield metadata
            var (moxfieldUrl, moxfieldId) = ExtractMoxfieldMetadata(json);
            
            // Get the next ID
            int nextId = _database.Decklists.Any() ? _database.Decklists.Max(d => d.Id) + 1 : 1;
            
            var createdAt = DateTime.UtcNow;
            
            _database.Decklists.Add(new Decklist
            {
                Id = nextId,
                Name = name,
                CardNames = cardNames.ToArray(),
                MoxfieldUrl = moxfieldUrl,
                MoxfieldId = moxfieldId,
                CreatedAt = createdAt
            });
            
            await _database.SaveChangesAsync();
            _logger.LogInformation($"Decklist '{name}' (Moxfield ID: {moxfieldId}) added with ID {nextId} containing {cardNames.Count} cards at {createdAt:yyyy-MM-dd HH:mm:ss} UTC");
        }

        private List<string> ExtractMainboardCardNames(string json)
        {
            var cardNames = new List<string>();
            
            using (JsonDocument document = JsonDocument.Parse(json))
            {
                var root = document.RootElement;
                
                // Navigate to boards.mainboard
                if (root.TryGetProperty("boards", out var boards))
                {
                    if (boards.TryGetProperty("mainboard", out var mainboard))
                    {
                        if (mainboard.TryGetProperty("cards", out var cards))
                        {
                            // Iterate through each card in the mainboard
                            foreach (var cardProperty in cards.EnumerateObject())
                            {
                                var cardData = cardProperty.Value;
                                
                                // Get the card name
                                if (cardData.TryGetProperty("card", out var cardInfo))
                                {
                                    if (cardInfo.TryGetProperty("name", out var cardName))
                                    {
                                        cardNames.Add(cardName.GetString() ?? string.Empty);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            
            return cardNames;
        }

        private (string? moxfieldUrl, string? moxfieldId) ExtractMoxfieldMetadata(string json)
        {
            using (JsonDocument document = JsonDocument.Parse(json))
            {
                var root = document.RootElement;
                
                string? publicUrl = null;
                string? publicId = null;
                
                // Extract publicUrl
                if (root.TryGetProperty("publicUrl", out var urlProperty))
                {
                    publicUrl = urlProperty.GetString();
                }
                
                // Extract publicId
                if (root.TryGetProperty("publicId", out var idProperty))
                {
                    publicId = idProperty.GetString();
                }
                
                return (publicUrl, publicId);
            }
        }

        private async Task<string> GetDecklistAsync(string link)
        {
            // Extract deck ID from the Moxfield URL
            string deckId = link.Split('/').Last();
            var requestUrl = _moxfieldConfig.BaseUrl + deckId;
            
            _logger.LogInformation($"Requesting decklist from: {requestUrl}");

            // Enforce rate limiting (1 request per second)
            await _rateLimiter.WaitIfNeededAsync();

            // Create HTTP client with proper configuration
            var handler = new HttpClientHandler
            {
                AutomaticDecompression = System.Net.DecompressionMethods.All
            };

            using var client = new HttpClient(handler);

            // Create request with Moxfield API key as User-Agent
            var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            
            // Use TryAddWithoutValidation to handle special characters in the API key
            request.Headers.TryAddWithoutValidation("User-Agent", _moxfieldConfig.ApiKey);
            request.Headers.TryAddWithoutValidation("Accept", "application/json, text/plain, */*");

            // Send the request
            var response = await client.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Failed to fetch decklist. Status: {response.StatusCode}");
                throw new HttpRequestException($"Failed to fetch decklist. Status: {response.StatusCode} - {response.ReasonPhrase}");
            }

            // Read and return the JSON content
            var jsonContent = await response.Content.ReadAsStringAsync();
            
            _logger.LogInformation("Successfully retrieved decklist");
            
            return jsonContent;
        }
    }
}
