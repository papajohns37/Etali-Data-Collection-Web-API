using System.Text.Json;
using System.Text.RegularExpressions;
using Etali_Data_Collection_Web_API.Configuration;
using Etali_Data_Collection_Web_API.Models;

namespace Etali_Data_Collection_Web_API.Services;

/// <summary>
/// Service for adding decklists from Moxfield.
/// </summary>
public partial class DecklistAddingService(
    EtaliDatabase database,
    IHttpClientFactory httpClientFactory,
    MoxfieldConfiguration moxfieldConfig,
    IRateLimiter rateLimiter) : IDecklistAddingService
{
    private static readonly Regex MoxfieldUrlPattern = new(@"moxfield\.com/decks/([a-zA-Z0-9_-]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <inheritdoc/>
    public async Task AddDecklist(string link, string name, string? createdByUserId = null)
    {
        string json = await GetDecklistAsync(link);
        
        // Parse the JSON and extract mainboard cards with their properties
        List<Card> cards = ExtractMainboardCards(json);
        
        // Extract Moxfield metadata
        var (moxfieldUrl, moxfieldId) = ExtractMoxfieldMetadata(json);
        
        // Get the next ID
        int nextId = database.Decklists.Any() ? database.Decklists.Max(d => d.Id) + 1 : 1;
        
        var createdAt = DateTime.UtcNow;
        
        database.Decklists.Add(new Decklist
        {
            Id = nextId,
            Name = name,
            Cards = [.. cards],
            MoxfieldUrl = moxfieldUrl,
            MoxfieldId = moxfieldId,
            CreatedAt = createdAt,
            CreatedByUserId = createdByUserId
        });
        
        await database.SaveChangesAsync();
    }

    private List<Card> ExtractMainboardCards(string json)
    {
        var cards = new List<Card>();
        
        using (JsonDocument document = JsonDocument.Parse(json))
        {
            var root = document.RootElement;
            
            // Navigate to boards.mainboard
            if (root.TryGetProperty("boards", out var boards))
            {
                if (boards.TryGetProperty("mainboard", out var mainboard))
                {
                    if (mainboard.TryGetProperty("cards", out var cardsProperty))
                    {
                        // Iterate through each card in the mainboard
                        foreach (var cardProperty in cardsProperty.EnumerateObject())
                        {
                            var cardData = cardProperty.Value;
                            
                            // Get the quantity of this card
                            int quantity = 1;
                            if (cardData.TryGetProperty("quantity", out var quantityProperty))
                            {
                                quantity = quantityProperty.GetInt32();
                            }
                            
                            // Get the card information
                            if (cardData.TryGetProperty("card", out var cardInfo))
                            {
                                string name = string.Empty;
                                string type = string.Empty;
                                
                                // Extract card name
                                if (cardInfo.TryGetProperty("name", out var cardName))
                                {
                                    name = cardName.GetString() ?? string.Empty;
                                }
                                
                                // Extract card type - try multiple approaches
                                // 1. Try "type_line" at card level (standard single-faced cards)
                                if (cardInfo.TryGetProperty("type_line", out var cardTypeLine))
                                {
                                    type = cardTypeLine.GetString() ?? string.Empty;
                                }
                                // 2. If not found, check card_faces array (double-faced or modal cards)
                                else if (cardInfo.TryGetProperty("card_faces", out var cardFaces) && 
                                         cardFaces.ValueKind == JsonValueKind.Array &&
                                         cardFaces.GetArrayLength() > 0)
                                {
                                    // Get the type_line from the first card face
                                    var firstFace = cardFaces[0];
                                    if (firstFace.TryGetProperty("type_line", out var faceTypeLine))
                                    {
                                        type = faceTypeLine.GetString() ?? string.Empty;
                                    }
                                }
                                // 3. Fall back to "type" if it's a string (some APIs might use this)
                                else if (cardInfo.TryGetProperty("type", out var cardType) && 
                                         cardType.ValueKind == JsonValueKind.String)
                                {
                                    type = cardType.GetString() ?? string.Empty;
                                }
                                
                                // Add the card 'quantity' times to match the deck composition
                                for (int i = 0; i < quantity; i++)
                                {
                                    cards.Add(new Card
                                    {
                                        Name = name,
                                        Type = type
                                    });
                                }
                            }
                        }
                    }
                }
            }
        }
        
        return cards;
    }

    private static (string? moxfieldUrl, string? moxfieldId) ExtractMoxfieldMetadata(string json)
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
        // Extract deck ID from the Moxfield URL using regex
        var match = MoxfieldUrlPattern.Match(link);
        if (!match.Success)
        {
            throw new ArgumentException("Invalid Moxfield URL format. Expected format: https://www.moxfield.com/decks/{deckId}");
        }

        string deckId = match.Groups[1].Value;
        var requestUrl = moxfieldConfig.BaseUrl + deckId;

        // Enforce rate limiting (1 request per second)
        await rateLimiter.WaitIfNeededAsync();

        // Use HttpClientFactory for connection pooling
        var client = httpClientFactory.CreateClient("Moxfield");

        // Create request with Moxfield API key as User-Agent
        var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
        request.Headers.TryAddWithoutValidation("User-Agent", moxfieldConfig.ApiKey);
        request.Headers.TryAddWithoutValidation("Accept", "application/json, text/plain, */*");

        // Send the request
        var response = await client.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var statusCode = (int)response.StatusCode;
            var errorMessage = statusCode switch
            {
                404 => "Deck not found (404). Please verify the Moxfield URL is correct.",
                429 => "Moxfield API rate limit exceeded (429). Please try again later.",
                _ => $"Failed to fetch decklist. Status: {response.StatusCode} - {response.ReasonPhrase}"
            };
            
            throw new HttpRequestException(errorMessage);
        }

        // Read and return the JSON content
        var jsonContent = await response.Content.ReadAsStringAsync();
        
        return jsonContent;
    }
}
