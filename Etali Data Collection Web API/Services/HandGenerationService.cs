using Etali_Data_Collection_Web_API.Configuration;
using Etali_Data_Collection_Web_API.Models;
using Microsoft.EntityFrameworkCore;

namespace Etali_Data_Collection_Web_API.Services;

/// <summary>
/// Service responsible for generating random hands of cards with associated game metadata.
/// Also serves old hands from the migration database for user evaluation.
/// </summary>
public class HandGenerationService(GameConfiguration configuration, EtaliDatabase database, ILogger<HandGenerationService> logger) : IHandGenerationService
{
    /// <inheritdoc/>
    public Hand GenerateHand(int handId, string decklistName)
    {
        // First, check if there are any old hands that need evaluation for this decklist
        var oldHand = database.OldEvaluatedHands
            .FirstOrDefault(h => h.DecklistName.ToLower() == decklistName.ToLower());

        if (oldHand != null)
        {
            logger.LogInformation(
                "Serving old hand {OldHandId} from '{DecklistName}' for evaluation (new hand ID: {NewHandId})",
                oldHand.Id, oldHand.DecklistName, handId);
            
            // Remove the old hand immediately so it won't be served again
            database.OldEvaluatedHands.Remove(oldHand);
            database.SaveChanges();
            
            logger.LogDebug("Removed old hand {OldHandId} from old_evaluated_hands after serving", oldHand.Id);
            
            return ConvertOldHandToHand(oldHand, handId);
        }

        // No old hands available, generate a new random hand
        logger.LogDebug("No old hands available for '{DecklistName}', generating new random hand", decklistName);
        return GenerateRandomHand(handId, decklistName);
    }

    /// <summary>
    /// Converts an old evaluated hand to a Hand for user evaluation.
    /// </summary>
    private Hand ConvertOldHandToHand(OldEvaluatedHand oldHand, int newHandId)
    {
        // Look up the decklist to get card types
        var decklist = database.Decklists.Find(oldHand.DecklistId);
        if (decklist == null)
        {
            throw new InvalidOperationException(
                $"Decklist ID {oldHand.DecklistId} not found for old hand {oldHand.Id}");
        }

        // Convert card names to Card objects
        var cards = new Card[7];
        var cardNames = new[] 
        { 
            oldHand.Card1, oldHand.Card2, oldHand.Card3, oldHand.Card4,
            oldHand.Card5, oldHand.Card6, oldHand.Card7 
        };

        for (int i = 0; i < 7; i++)
        {
            var cardName = cardNames[i];
            if (string.IsNullOrWhiteSpace(cardName))
            {
                throw new InvalidOperationException(
                    $"Old hand {oldHand.Id} has missing card at position {i + 1}");
            }

            // Find the card in the decklist to get its type
            var cardFromDecklist = decklist.Cards.FirstOrDefault(
                c => c.Name.Equals(cardName, StringComparison.OrdinalIgnoreCase));
            
            if (cardFromDecklist == null)
            {
                logger.LogWarning(
                    "Card '{CardName}' from old hand {OldHandId} not found in decklist '{DecklistName}', using Unknown type",
                    cardName, oldHand.Id, decklist.Name);
                cards[i] = new Card { Name = cardName, Type = "Unknown" };
            }
            else
            {
                cards[i] = cardFromDecklist;
            }
        }

        // Generate random values for fields that weren't tracked in old data
        var seatOrder = GenerateSeatOrder();
        var stickers = GenerateStickers();
        var lowestGambleOdds = GenerateLowestGambleOdds();

        return new Hand(
            newHandId, 
            cards, 
            seatOrder, 
            stickers, 
            decklist.Id, 
            decklist.Name, 
            lowestGambleOdds);
    }

    /// <summary>
    /// Generates a new random hand from the specified decklist.
    /// </summary>
    private Hand GenerateRandomHand(int handId, string decklistName)
    {
        // Look up the decklist by name (case-insensitive)
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
