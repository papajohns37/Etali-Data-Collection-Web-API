using System.ComponentModel;

namespace Etali_Data_Collection_Web_API.Models;

/// <summary>
/// Summary of a completed mulligan session.
/// </summary>
/// <param name="SessionId">The ID of the completed session.</param>
/// <param name="FoundSuccessfulHand">Whether a hand that can cast Etali by turn 2 was found.</param>
/// <param name="FinalHandSize">The size of the final kept hand (3-7).</param>
/// <param name="TotalMulligans">The total number of mulligans taken (0-5).</param>
/// <param name="TotalSerumPowderActivations">The total number of times Serum Powder was activated.</param>
/// <example>
/// {
///   "sessionId": 42,
///   "foundSuccessfulHand": true,
///   "finalHandSize": 5,
///   "totalMulligans": 3,
///   "totalSerumPowderActivations": 1
/// }
/// </example>
public record FullMulliganSessionSummary(
    [property: Description("The ID of the completed session")]
    int SessionId,

    [property: Description("Whether a successful hand was found")]
    bool FoundSuccessfulHand,

    [property: Description("Size of the final kept hand (3-7)")]
    int FinalHandSize,

    [property: Description("Total number of mulligans taken (0-5)")]
    int TotalMulligans,

    [property: Description("Total number of Serum Powder activations")]
    int TotalSerumPowderActivations
);
