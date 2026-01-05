using System.ComponentModel;

namespace Etali_Data_Collection_Web_API.Models;

/// <summary>
/// Represents a hand generated for deck comparison evaluation.
/// Includes comparison context and the number of generation attempts made.
/// </summary>
/// <param name="Id">Unique identifier for this hand.</param>
/// <param name="ComparisonId">The ID of the deck comparison this hand belongs to.</param>
/// <param name="WhichDeck">Which deck in the comparison this hand came from (1 or 2).</param>
/// <param name="Cards">Array of exactly 7 cards drawn randomly from the decklist.</param>
/// <param name="SeatOrder">The player's seat position in the game (1-4), representing turn order.</param>
/// <param name="Stickers">Array of exactly 3 randomly selected sticker values from the available pool.</param>
/// <param name="DecklistId">The database ID of the decklist this hand was generated from.</param>
/// <param name="DecklistName">The human-readable name of the decklist this hand was generated from.</param>
/// <param name="LowestGambleOdds">The lowest Gamble odds value for this hand, representing probability.</param>
/// <param name="GenerationAttempts">The number of hands generated before finding one with a card unique to this deck.</param>
/// <example>
/// {
///   "id": 1,
///   "comparisonId": 5,
///   "whichDeck": 1,
///   "cards": [
///     { "name": "Sol Ring", "type": "Artifact" },
///     { "name": "Mana Vault", "type": "Artifact" },
///     { "name": "Ancient Tomb", "type": "Land" }
///   ],
///   "seatOrder": 2,
///   "stickers": [6, 5, 4],
///   "decklistId": 1,
///   "decklistName": "Fast Etali",
///   "lowestGambleOdds": 0.75,
///   "generationAttempts": 3
/// }
/// </example>
public record ComparisonHand(
    [property: Description("Unique identifier for this hand")]
    int Id,
    
    [property: Description("ID of the deck comparison this hand belongs to")]
    int ComparisonId,
    
    [property: Description("Which deck in the comparison (1 or 2)")]
    int WhichDeck,
    
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
    
    [property: Description("Lowest Gamble odds value for this hand")]
    float LowestGambleOdds,
    
    [property: Description("Number of hands generated before finding one with unique card")]
    int GenerationAttempts
);
