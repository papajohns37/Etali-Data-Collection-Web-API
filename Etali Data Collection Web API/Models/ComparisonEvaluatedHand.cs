using System.ComponentModel;

namespace Etali_Data_Collection_Web_API.Models;

/// <summary>
/// Represents a hand that has been evaluated as part of a deck comparison.
/// Unlike regular EvaluatedHand, this is linked to a DeckComparison and tracks which deck it came from.
/// These evaluations are kept separate and do not affect individual decklist statistics.
/// </summary>
/// <param name="Id">Unique identifier for this comparison evaluation.</param>
/// <param name="ComparisonId">The ID of the DeckComparison this evaluation belongs to.</param>
/// <param name="WhichDeck">Which deck in the comparison this hand came from (1 or 2).</param>
/// <param name="Cards">Array of exactly 7 cards in the evaluated hand.</param>
/// <param name="SeatOrder">The player's seat position in the game (1-4).</param>
/// <param name="Stickers">Array of exactly 3 sticker values that were applied to Etali.</param>
/// <param name="CanCastEtali">Indicates whether Etali can be cast with this hand. If false, MinimumCardsToCastEtali must be 8.</param>
/// <param name="MinimumCardsToCastEtali">The minimum number of cards required to generate enough mana to cast Etali. Must be 1-7 if CanCastEtali is true, or 8 if false.</param>
/// <param name="DecklistId">The database ID of the decklist this hand was generated from.</param>
/// <param name="DecklistName">The name of the decklist this hand was generated from.</param>
/// <param name="LowestGambleOdds">The lowest Gamble odds value for this hand, representing probability.</param>
/// <param name="GenerationAttempts">The number of hands generated before finding one with a card unique to this deck.</param>
/// <remarks>
/// The evaluation rules:
/// - If CanCastEtali is true, MinimumCardsToCastEtali should be between 1 and 7 (inclusive)
/// - If CanCastEtali is false, MinimumCardsToCastEtali should be 8 (meaning all 7 cards plus one more are insufficient)
/// 
/// Comparison evaluation process:
/// - Hands are only evaluated if they contain at least one card unique to their deck
/// - GenerationAttempts tracks how many hands were generated (including rejected ones)
/// - This count is added to the comparison's N value when the evaluation is submitted
/// </remarks>
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
///   "canCastEtali": true,
///   "minimumCardsToCastEtali": 3,
///   "decklistId": 1,
///   "decklistName": "Fast Etali",
///   "lowestGambleOdds": 0.75,
///   "generationAttempts": 3
/// }
/// </example>
public record ComparisonEvaluatedHand(
    [property: Description("Unique identifier for this comparison evaluation")]
    int Id,
    
    [property: Description("ID of the DeckComparison this evaluation belongs to")]
    int ComparisonId,
    
    [property: Description("Which deck in the comparison (1 or 2)")]
    int WhichDeck,
    
    [property: Description("Array of 7 cards in the opening hand")]
    Card[] Cards,
    
    [property: Description("Player's seat position (1-4)")]
    int SeatOrder,
    
    [property: Description("Array of 3 sticker values applied to Etali")]
    int[] Stickers,
    
    [property: Description("Whether Etali can be cast with this hand")]
    bool CanCastEtali,
    
    [property: Description("Minimum number of cards needed to cast Etali (1-7 if possible, 8 if impossible)")]
    int MinimumCardsToCastEtali,
    
    [property: Description("Database ID of the source decklist")]
    int DecklistId,
    
    [property: Description("Name of the source decklist")]
    string DecklistName,
    
    [property: Description("Lowest Gamble odds value for this hand")]
    float LowestGambleOdds,
    
    [property: Description("Number of hands generated before finding one with unique card")]
    int GenerationAttempts
);
