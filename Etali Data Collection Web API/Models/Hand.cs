using System.ComponentModel;

namespace Etali_Data_Collection_Web_API.Models;

/// <summary>
/// Represents a randomly generated hand of cards with associated game metadata.
/// This model is used when generating new hands for evaluation.
/// </summary>
/// <param name="Id">Unique identifier for this hand.</param>
/// <param name="Cards">Array of exactly 7 card names drawn randomly from the decklist.</param>
/// <param name="SeatOrder">The player's seat position in the game (1-4), representing turn order.</param>
/// <param name="Stickers">Array of exactly 3 randomly selected sticker values from the available pool.</param>
/// <param name="DecklistId">The database ID of the decklist this hand was generated from.</param>
/// <param name="DecklistName">The human-readable name of the decklist this hand was generated from.</param>
/// <param name="LowestGambleOdds">The lowest Gamble odds value for this hand, representing probability.</param>
/// <example>
/// {
///   "id": 1,
///   "cards": ["Sol Ring", "Mana Vault", "Ancient Tomb", "Chrome Mox", "Simian Spirit Guide", "Jeweled Lotus", "Mox Diamond"],
///   "seatOrder": 2,
///   "stickers": [6, 5, 4],
///   "decklistId": 1,
///   "decklistName": "Fast Etali",
///   "lowestGambleOdds": 0.75
/// }
/// </example>
public record Hand(
    [property: Description("Unique identifier for this hand")]
    int Id,
    
    [property: Description("Array of 7 card names in the opening hand")]
    string[] Cards,
    
    [property: Description("Player's seat position (1-4), representing turn order")]
    int SeatOrder,
    
    [property: Description("Array of 3 sticker values applied to Etali")]
    int[] Stickers,
    
    [property: Description("Database ID of the source decklist")]
    int DecklistId,
    
    [property: Description("Name of the source decklist")]
    string DecklistName,
    
    [property: Description("Lowest Gamble odds value for this hand")]
    float LowestGambleOdds
);
