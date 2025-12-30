using System.Net.Http;
using System.Text.Json;
using Etali_Data_Collection_Web_API.Models;
using PuppeteerSharp;

namespace Etali_Data_Collection_Web_API.Services
{
    public class DecklistAddingService : IDecklistAddingService
    {
        EtaliDatabase _database;
        ILogger<DecklistAddingService> _logger;

        public DecklistAddingService(EtaliDatabase database, ILogger<DecklistAddingService> logger)
        {
            _database = database;
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task AddDecklist(string link, string name)
        {
            string json = await GetDecklistAsync(link);
            _logger.LogDebug(json);
            
            // Parse the JSON and extract mainboard card names
            List<string> cardNames = ExtractMainboardCardNames(json);
            
            // Get the next ID
            int nextId = _database.Decklists.Any() ? _database.Decklists.Max(d => d.Id) + 1 : 1;
            
            _database.Decklists.Add(new Decklist
            {
                Id = nextId,
                Name = name,
                CardNames = cardNames.ToArray()
            });
            
            await _database.SaveChangesAsync();
            _logger.LogInformation($"Decklist '{name}' added with ID {nextId} containing {cardNames.Count} cards");
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

        private string moxfieldBaseAPIUrl = "https://api2.moxfield.com/v3/decks/all/";

        async Task<string> GetDecklistAsync(string link)
        {
            string deckId = link.Split('/').Last();
            var requestUrl = moxfieldBaseAPIUrl + deckId;
            
            _logger.LogInformation($"Requesting decklist from: {requestUrl}");

            // Download browser if not already present
            var browserFetcher = new BrowserFetcher();
            await browserFetcher.DownloadAsync();

            // Launch browser with more realistic options
            await using var browser = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = true,
                Args = new[] 
                { 
                    "--no-sandbox",
                    "--disable-setuid-sandbox",
                    "--disable-blink-features=AutomationControlled"
                }
            });

            await using var page = await browser.NewPageAsync();
            
            // Set a realistic user agent
            await page.SetUserAgentAsync("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            
            // Set extra headers
            await page.SetExtraHttpHeadersAsync(new Dictionary<string, string>
            {
                { "Accept", "application/json, text/plain, */*" },
                { "Accept-Language", "en-US,en;q=0.9" },
                { "Accept-Encoding", "gzip, deflate, br" },
                { "Referer", "https://www.moxfield.com/" },
                { "Origin", "https://www.moxfield.com" }
            });
            
            // Navigate to the API URL
            var response = await page.GoToAsync(requestUrl, new NavigationOptions
            {
                WaitUntil = new[] { WaitUntilNavigation.Networkidle0 },
                Timeout = 30000
            });

            if (response.Status != System.Net.HttpStatusCode.OK)
            {
                _logger.LogError($"Failed to fetch decklist. Status: {response.Status}");
                throw new HttpRequestException($"Failed to fetch decklist. Status: {response.Status}");
            }

            // Extract JSON from the HTML body
            var jsonContent = await page.EvaluateFunctionAsync<string>("() => document.body.innerText");
            
            _logger.LogInformation("Successfully retrieved decklist");
            
            return jsonContent;
        }
    }
}
