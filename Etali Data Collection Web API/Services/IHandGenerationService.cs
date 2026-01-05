namespace Etali_Data_Collection_Web_API.Services;

using Etali_Data_Collection_Web_API.Models;

public interface IHandGenerationService
{
    /// <summary>
    /// Generates a random hand from a specified decklist.
    /// </summary>
    /// <param name="handId">The unique identifier for this hand.</param>
    /// <param name="decklistName">The name of the decklist to generate from (case-insensitive).</param>
    /// <returns>A randomly generated hand with cards, stickers, and metadata.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the decklist is not found or has insufficient cards.
    /// </exception>
    Hand GenerateHand(int handId, string decklistName);
}
