using Etali_Data_Collection_Web_API.Configuration;
using Etali_Data_Collection_Web_API.Models;

namespace Etali_Data_Collection_Web_API.Services;

/// <summary>
/// Service responsible for generating random hands for the maximum mana dataset workflow.
/// </summary>
public class MaximumManaHandGenerationService(GameConfiguration configuration, EtaliDatabase database) : IMaximumManaHandGenerationService
{
    /// <inheritdoc/>
    public MaximumManaHand GenerateHand(int handId, string decklistName)
    {
        var lowerName = decklistName.ToLower();
        var decklist = database.Decklists
            .FirstOrDefault(d => d.Name.ToLower() == lowerName);

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

        return new MaximumManaHand(handId, selectedCards, seatOrder, stickers, decklist.Id, decklist.Name, lowestGambleOdds);
    }

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

    private static int GenerateSeatOrder()
    {
        return Random.Shared.Next(1, 5);
    }

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

    private static double GenerateLowestGambleOdds()
    {
        return Random.Shared.NextDouble();
    }
}
