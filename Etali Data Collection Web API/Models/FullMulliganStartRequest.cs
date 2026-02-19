using System.ComponentModel;

namespace Etali_Data_Collection_Web_API.Models;

/// <summary>
/// Request model for starting a new full mulligan session.
/// </summary>
/// <param name="DecklistId">The ID of the decklist to test.</param>
/// <example>
/// {
///   "decklistId": 1
/// }
/// </example>
public record FullMulliganStartRequest(
    [property: Description("The ID of the decklist to test")]
    int DecklistId
);
