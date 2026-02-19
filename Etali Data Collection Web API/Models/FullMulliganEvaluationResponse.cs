using System.ComponentModel;

namespace Etali_Data_Collection_Web_API.Models;

/// <summary>
/// Response model returned after evaluating a hand during a mulligan session.
/// Contains either the next hand to evaluate or the final session summary.
/// </summary>
/// <param name="SessionComplete">Whether the mulligan session has been completed.</param>
/// <param name="NextHand">The next hand to evaluate (null if session complete).</param>
/// <param name="NextHandNumber">The number of the next hand (null if session complete).</param>
/// <param name="NextTargetHandSize">The target size of the next hand (null if session complete).</param>
/// <param name="CanMulligan">Whether the user can mulligan the next hand (false if at 3 cards).</param>
/// <param name="Summary">Summary of the completed session (null if session not complete).</param>
/// <example>
/// // When session continues:
/// {
///   "sessionComplete": false,
///   "nextHand": {
///     "id": 1002,
///     "cards": [...],
///     "seatOrder": 2,
///     "stickers": [6, 5, 4],
///     "decklistId": 1,
///     "decklistName": "Fast Etali",
///     "lowestGambleOdds": 0.75
///   },
///   "nextHandNumber": 2,
///   "nextTargetHandSize": 7,
///   "canMulligan": true,
///   "summary": null
/// }
/// 
/// // When session completes:
/// {
///   "sessionComplete": true,
///   "nextHand": null,
///   "nextHandNumber": null,
///   "nextTargetHandSize": null,
///   "canMulligan": false,
///   "summary": {
///     "sessionId": 42,
///     "foundSuccessfulHand": true,
///     "finalHandSize": 5,
///     "totalMulligans": 3
///   }
/// }
/// </example>
public record FullMulliganEvaluationResponse(
    [property: Description("Whether the mulligan session has been completed")]
    bool SessionComplete,
    
    [property: Description("The next hand to evaluate (null if session complete)")]
    Hand? NextHand,
    
    [property: Description("The number of the next hand (null if session complete)")]
    int? NextHandNumber,
    
    [property: Description("The target size of the next hand (null if session complete)")]
    int? NextTargetHandSize,
    
    [property: Description("Whether the user can mulligan the next hand")]
    bool CanMulligan,
    
    [property: Description("Summary of the completed session (null if not complete)")]
    FullMulliganSessionSummary? Summary
);
