using System.ComponentModel;

namespace Etali_Data_Collection_Web_API.Models;

/// <summary>
/// Represents a randomly generated hand for the maximum mana dataset workflow.
/// </summary>
/// <param name="Id">Unique identifier for this hand.</param>
/// <param name="Cards">Array of exactly 7 cards drawn randomly from the decklist.</param>
/// <param name="SeatOrder">The player's seat position in the game (1-4).</param>
/// <param name="Stickers">Array of exactly 3 randomly selected sticker values.</param>
/// <param name="DecklistId">The database ID of the decklist this hand was generated from.</param>
/// <param name="DecklistName">The name of the decklist this hand was generated from.</param>
/// <param name="LowestGambleOdds">The lowest Gamble odds value for this hand.</param>
public record MaximumManaHand(
    [property: Description("Unique identifier for this hand")]
    int Id,

    [property: Description("Array of 7 cards in the opening hand")]
    Card[] Cards,

    [property: Description("Player's seat position (1-4), representing turn order")]
    int SeatOrder,

    [property: Description("Array of 3 sticker values applied to Etali")]
    int[] Stickers,

    [property: Description("Database ID of the source decklist")]
    int DecklistId,

    [property: Description("Name of the source decklist")]
    string DecklistName,

    [property: Description("Lowest Gamble odds value for this hand (0.0 to 1.0)")]
    double LowestGambleOdds
);
