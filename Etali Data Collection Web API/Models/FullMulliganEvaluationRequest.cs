using System.ComponentModel;

namespace Etali_Data_Collection_Web_API.Models;

/// <summary>
/// Request model for evaluating a hand during a mulligan session.
/// </summary>
/// <param name="SessionId">The ID of the mulligan session.</param>
/// <param name="HandNumber">The current hand number being evaluated.</param>
/// <param name="CanCastEtaliByTurn2">Whether this hand can cast Etali by turn 2.</param>
/// <param name="KeepHand">Whether to keep this hand (false = mulligan to next hand size).</param>
/// <param name="ActivateSerumPowder">Whether to activate Serum Powder (if available in hand).</param>
/// <param name="CardsToBottomIndices">Indices of cards to bottom when activating Serum Powder. Required if ActivateSerumPowder is true. Count must equal (7 - currentHandSize).</param>
/// <example>
/// // Normal mulligan:
/// {
///   "sessionId": 42,
///   "handNumber": 1,
///   "canCastEtaliByTurn2": false,
///   "keepHand": false
/// }
/// 
/// // Activating Serum Powder on a 6-card hand (bottom 1 card):
/// {
///   "sessionId": 42,
///   "handNumber": 3,
///   "canCastEtaliByTurn2": false,
///   "keepHand": false,
///   "activateSerumPowder": true,
///   "cardsToBottomIndices": [2]
/// }
/// </example>
public record FullMulliganEvaluationRequest(
    [property: Description("The ID of the mulligan session")]
    int SessionId,

    [property: Description("Current hand number being evaluated")]
    int HandNumber,

    [property: Description("Can this hand cast Etali by turn 2?")]
    bool CanCastEtaliByTurn2,

    [property: Description("Keep this hand? (false = mulligan)")]
    bool KeepHand,

    [property: Description("Whether to activate Serum Powder")]
    bool? ActivateSerumPowder = null,

    [property: Description("Indices of cards to bottom (required if activating Serum Powder)")]
    int[]? CardsToBottomIndices = null
);
