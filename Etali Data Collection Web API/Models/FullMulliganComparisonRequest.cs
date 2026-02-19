using System.ComponentModel;

namespace Etali_Data_Collection_Web_API.Models;

/// <summary>
/// Request model for comparing mulligan statistics between two decklists.
/// </summary>
/// <param name="DecklistId1">The ID of the first decklist to compare.</param>
/// <param name="DecklistId2">The ID of the second decklist to compare.</param>
/// <example>
/// {
///   "decklistId1": 1,
///   "decklistId2": 2
/// }
/// </example>
public record FullMulliganComparisonRequest(
    [property: Description("First decklist ID")]
    int DecklistId1,
    
    [property: Description("Second decklist ID")]
    int DecklistId2
);
