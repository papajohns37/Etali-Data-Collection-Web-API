using Etali_Data_Collection_Web_API.Configuration;
using Etali_Data_Collection_Web_API.Models;

namespace Etali_Data_Collection_Web_API.Services;

/// <summary>
/// Service responsible for generating random hands of cards with associated game metadata.
/// </summary>
public class HandGenerationService : IHandGenerationService
{
    private readonly GameConfiguration _configuration;
    private readonly Random _random;

    /// <summary>
    /// Initializes a new instance of the <see cref="HandGenerationService"/> class.
    /// </summary>
    /// <param name="configuration">The game configuration containing decklist and sticker values.</param>
    public HandGenerationService(GameConfiguration configuration)
    {
        _configuration = configuration;
        _random = new Random();
    }

    /// <inheritdoc/>
    public Hand GenerateHand(int handId)
    {
        var selectedCards = SelectRandomCards();
        var seatOrder = GenerateSeatOrder();
        var stickers = GenerateStickers();

        return new Hand(handId, selectedCards, seatOrder, stickers);
    }

    /// <summary>
    /// Selects 7 random cards from the decklist.
    /// </summary>
    /// <returns>An array of 7 randomly selected card names.</returns>
    private string[] SelectRandomCards()
    {
        return _configuration.Decklist
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
}
