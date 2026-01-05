using Etali_Data_Collection_Web_API.Configuration;
using Etali_Data_Collection_Web_API.Models;
using Microsoft.EntityFrameworkCore;

namespace Etali_Data_Collection_Web_API.Services;

/// <summary>
/// Service responsible for generating random hands of cards with associated game metadata.
/// </summary>
public class HandGenerationService(GameConfiguration configuration, EtaliDatabase database) : IHandGenerationService
{
    /// <inheritdoc/>
    public Hand GenerateHand(int handId, string decklistName)
    {
        // Look up the decklist by name (case-insensitive)
        var decklist = database.Decklists
            .FirstOrDefault(d => string.Equals(d.Name, decklistName, StringComparison.OrdinalIgnoreCase));

        if (decklist == null)
        {
            throw new InvalidOperationException($"Decklist '{decklistName}' not found.");
        }

        if (decklist.Cards.Length < 7)
        {
            throw new InvalidOperationException($"Decklist '{decklistName}' has insufficient cards. At least 7 cards are required.");
        }

        var selectedCards = SelectRandomCards(decklist.Cards);
        var seatOrder = GenerateSeatOrder();
        var stickers = GenerateStickers();
        var lowestGambleOdds = GenerateLowestGambleOdds();

        return new Hand(handId, selectedCards, seatOrder, stickers, decklist.Id, decklist.Name, lowestGambleOdds);
    }

    /// <summary>
    /// Selects 7 random cards from the provided card list using efficient algorithm.
    /// </summary>
    /// <param name="cards">The array of cards to select from.</param>
    /// <returns>An array of 7 randomly selected cards.</returns>
    private static Card[] SelectRandomCards(Card[] cards)
    {
        var result = new Card[7];
        var indices = new HashSet<int>();

        for (int i = 0; i < 7; i++)
        {
            int index;
            do
            {
                index = Random.Shared.Next(cards.Length);
            } while (!indices.Add(index));

            result[i] = cards[index];
        }

        return result;
    }

    /// <summary>
    /// Generates a random seat order position for the player.
    /// </summary>
    /// <returns>A random seat position between 1 and 4.</returns>
    private static int GenerateSeatOrder()
    {
        return Random.Shared.Next(1, 5);
    }

    /// <summary>
    /// Generates 3 random sticker values from the available pool using efficient algorithm.
    /// </summary>
    /// <returns>An array of 3 randomly selected sticker values.</returns>
    private int[] GenerateStickers()
    {
        var result = new int[3];
        var indices = new HashSet<int>();

        for (int i = 0; i < 3; i++)
        {
            int index;
            do
            {
                index = Random.Shared.Next(configuration.AvailableStickers.Length);
            } while (!indices.Add(index));

            result[i] = configuration.AvailableStickers[index];
        }

        return result;
    }

    /// <summary>
    /// Generates the lowest Gamble odds value for this hand.
    /// </summary>
    /// <returns>A float value representing the lowest Gamble odds.</returns>
    /// <remarks>
    /// Generates a uniformly distributed random value between 0 and 1.
    /// This value is used to determine Gamble card success: if the Gamble probability
    /// is greater than this value, Gamble succeeds.
    /// For example: with 50% Gamble odds (0.5), this value will be less than 0.5
    /// exactly 50% of the time, making Gamble succeed 50% of the time.
    /// This ensures that over a large sample, Gamble success rate matches its probability.
    /// </remarks>
    private static float GenerateLowestGambleOdds()
    {
        return (float)Random.Shared.NextDouble();
    }
}
