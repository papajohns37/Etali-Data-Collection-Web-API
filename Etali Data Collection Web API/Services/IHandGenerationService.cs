using Etali_Data_Collection_Web_API.Models;

namespace Etali_Data_Collection_Web_API.Services;

/// <summary>
/// Defines the contract for generating random hands of cards.
/// </summary>
public interface IHandGenerationService
{
    /// <summary>
    /// Generates a new random hand with cards, seat order, and stickers from a specific decklist.
    /// </summary>
    /// <param name="handId">The unique identifier to assign to this hand.</param>
    /// <param name="decklistName">The name of the decklist to generate the hand from.</param>
    /// <returns>A newly generated hand with random cards and metadata.</returns>
    Hand GenerateHand(int handId, string decklistName);
}
