using System.ComponentModel;

namespace Etali_Data_Collection_Web_API.Models;

/// <summary>
/// Response model returned when starting a new mulligan session.
/// Contains the first hand and session information.
/// </summary>
/// <param name="SessionId">Unique identifier for this mulligan session.</param>
/// <param name="CurrentHand">The first 7-card hand to evaluate.</param>
/// <param name="HandNumber">Current hand number in the sequence (always 1 for start).</param>
/// <param name="TargetHandSize">The hand size this represents (always 7 for first hand).</param>
/// <param name="CanMulligan">Whether the user can mulligan this hand (always true for first hand).</param>
/// <example>
/// {
///   "sessionId": 42,
///   "currentHand": {
///     "id": 1001,
///     "cards": [
///       { "name": "Sol Ring", "type": "Artifact" },
///       { "name": "Ancient Tomb", "type": "Land" }
///     ],
///     "seatOrder": 2,
///     "stickers": [6, 5, 4],
///     "decklistId": 1,
///     "decklistName": "Fast Etali",
///     "lowestGambleOdds": 0.75
///   },
///   "handNumber": 1,
///   "targetHandSize": 7,
///   "canMulligan": true
/// }
/// </example>
public record FullMulliganStartResponse(
    [property: Description("Unique identifier for the mulligan session")]
    int SessionId,
    
    [property: Description("The first 7-card hand to evaluate")]
    Hand CurrentHand,
    
    [property: Description("Current hand number (1 for first hand)")]
    int HandNumber,
    
    [property: Description("Target hand size (7 for first hand)")]
    int TargetHandSize,
    
    [property: Description("Whether the user can mulligan this hand")]
    bool CanMulligan
);
