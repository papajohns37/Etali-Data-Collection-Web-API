using Etali_Data_Collection_Web_API.Models;

namespace Etali_Data_Collection_Web_API.Services;

public interface IMaximumManaHandGenerationService
{
    /// <summary>
    /// Generates a random hand from a specified decklist for the maximum mana dataset flow.
    /// </summary>
    /// <param name="handId">The unique identifier for this hand.</param>
    /// <param name="decklistName">The name of the decklist to generate from (case-insensitive).</param>
    /// <returns>A randomly generated hand with cards, stickers, and metadata.</returns>
    MaximumManaHand GenerateHand(int handId, string decklistName);
}
