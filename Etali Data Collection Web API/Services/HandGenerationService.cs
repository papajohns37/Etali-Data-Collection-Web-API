using Etali_Data_Collection_Web_API.Configuration;
using Etali_Data_Collection_Web_API.Models;
using Microsoft.EntityFrameworkCore;

namespace Etali_Data_Collection_Web_API.Services;

/// <summary>
/// Service responsible for generating random hands of cards with associated game metadata.
/// </summary>
public class HandGenerationService : IHandGenerationService
{
    private readonly GameConfiguration _configuration;
    private readonly EtaliDatabase _database;
    private readonly Random _random;

    /// <summary>
    /// Initializes a new instance of the <see cref="HandGenerationService"/> class.
    /// </summary>
    /// <param name="configuration">The game configuration containing sticker values.</param>
    /// <param name="database">The database context for accessing decklists.</param>
    public HandGenerationService(GameConfiguration configuration, EtaliDatabase database)
    {
        _configuration = configuration;
        _database = database;
        _random = new Random();
    }

    /// <inheritdoc/>
    public Hand GenerateHand(int handId, string decklistName)
    {
        // Look up the decklist by name (case-insensitive)
        var decklist = _database.Decklists
            .FirstOrDefault(d => d.Name.ToLower() == decklistName.ToLower());

        if (decklist == null)
        {
            throw new InvalidOperationException($"Decklist '{decklistName}' not found.");
        }

        if (decklist.CardNames.Length < 7)
        {
            throw new InvalidOperationException($"Decklist '{decklistName}' has insufficient cards. At least 7 cards are required.");
        }

        var selectedCards = SelectRandomCards(decklist.CardNames);
        var seatOrder = GenerateSeatOrder();
        var stickers = GenerateStickers();
        var lowestGambleOdds = GenerateLowestGambleOdds();

        return new Hand(handId, selectedCards, seatOrder, stickers, decklist.Id, decklist.Name, lowestGambleOdds);
    }

    /// <summary>
    /// Selects 7 random cards from the provided card list.
    /// </summary>
    /// <param name="cardNames">The array of card names to select from.</param>
    /// <returns>An array of 7 randomly selected card names.</returns>
    private string[] SelectRandomCards(string[] cardNames)
    {
        return cardNames
            .OrderBy(_ => _random.Next())
            .Take(7)
            .ToArray();
    }

    /// <summary>
    /// Generates a random seat order position for the player.
    /// </summary>
    /// <returns>A random seat position between 1 and 4.</returns>
    private int GenerateSeatOrder()
    {
        return _random.Next(1, 5);
    }

    /// <summary>
    /// Generates 3 random sticker values from the available pool.
    /// </summary>
    /// <returns>An array of 3 randomly selected sticker values.</returns>
    private int[] GenerateStickers()
    {
        return _configuration.AvailableStickers
            .OrderBy(_ => _random.Next())
            .Take(3)
            .ToArray();
    }

    /// <summary>
    /// Generates the lowest Gamble odds value for this hand.
    /// </summary>
    /// <returns>A float value representing the lowest Gamble odds.</returns>
    /// <remarks>
    /// Generates a random value between 0 and 1 with a triangular probability distribution
    /// where the probability density at any value x equals x itself.
    /// This means higher values (closer to 1) are more likely than lower values.
    /// </remarks>
    private float GenerateLowestGambleOdds()
    {
        // Generate a uniform random number between 0 and 1
        double uniformRandom = _random.NextDouble();
        
        // Transform to triangular distribution where probability density at x equals x
        // Using inverse transform sampling: x = sqrt(2 * u) where u is uniform [0,1]
        // This gives us a distribution where P(X ? x) = x²/2 normalized to [0,1]
        double odds = Math.Sqrt(2.0 * uniformRandom);
        
        // Clamp to [0, 1] range and convert to float
        return (float)Math.Min(Math.Max(odds, 0.0), 1.0);
    }
}
