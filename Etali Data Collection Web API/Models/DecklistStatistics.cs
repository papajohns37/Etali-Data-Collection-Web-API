using System.ComponentModel;

namespace Etali_Data_Collection_Web_API.Models;

/// <summary>
/// Represents comprehensive statistics for a decklist's hand testing data.
/// Includes success rates for different hand sizes and overall mulligan probability.
/// </summary>
/// <param name="DecklistId">The unique identifier of the decklist.</param>
/// <param name="DecklistName">The name of the decklist.</param>
/// <param name="TotalHandsTested">Total number of unique hands tested for this decklist.</param>
/// <param name="HandSizeStatistics">Array of statistics for each hand size (3-7 cards).</param>
/// <param name="OverallMulliganProbability">Probability of casting Etali by turn 2 after going through full mulligan sequence (7, 7, 6, 5, 4, 3 card hands).</param>
/// <param name="OverallMulliganConfidenceIntervalLower">Lower bound of 90% confidence interval for overall mulligan probability.</param>
/// <param name="OverallMulliganConfidenceIntervalUpper">Upper bound of 90% confidence interval for overall mulligan probability.</param>
/// <example>
/// {
///   "decklistId": 123,
///   "decklistName": "Etali Combo",
///   "totalHandsTested": 250,
///   "handSizeStatistics": [
///     {
///       "handSize": 7,
///       "totalHands": 250,
///       "successfulHands": 175,
///       "probability": 0.70,
///       "confidenceIntervalLower": 0.6421,
///       "confidenceIntervalUpper": 0.7521
///     },
///     {
///       "handSize": 6,
///       "totalHands": 250,
///       "successfulHands": 150,
///       "probability": 0.60,
///       "confidenceIntervalLower": 0.5373,
///       "confidenceIntervalUpper": 0.6602
///     }
///   ],
///   "overallMulliganProbability": 0.9234,
///   "overallMulliganConfidenceIntervalLower": 0.8956,
///   "overallMulliganConfidenceIntervalUpper": 0.9456
/// }
/// </example>
public record DecklistStatistics(
    [property: Description("Unique identifier of the decklist")]
    int DecklistId,
    
    [property: Description("Name of the decklist")]
    string DecklistName,
    
    [property: Description("Total number of unique hands tested")]
    int TotalHandsTested,
    
    [property: Description("Statistics for each hand size (3-7 cards)")]
    HandSizeStatistics[] HandSizeStatistics,
    
    [property: Description("Overall probability after full mulligan sequence (7, 7, 6, 5, 4, 3)")]
    double OverallMulliganProbability,
    
    [property: Description("Lower bound of 90% CI for overall mulligan probability")]
    double OverallMulliganConfidenceIntervalLower,
    
    [property: Description("Upper bound of 90% CI for overall mulligan probability")]
    double OverallMulliganConfidenceIntervalUpper
);
