namespace Etali_Data_Collection_Web_API.Models;

/// <summary>
/// Represents a randomly generated hand of cards with associated game metadata.
/// </summary>
/// <param name="Id">Unique identifier for this hand.</param>
/// <param name="Cards">Array of 7 card names drawn from the decklist.</param>
/// <param name="SeatOrder">The player's seat position in the game (1-4).</param>
/// <param name="Stickers">Array of 3 randomly selected sticker values.</param>
public record Hand(
    int Id,
    string[] Cards,
    int SeatOrder,
    int[] Stickers
);
