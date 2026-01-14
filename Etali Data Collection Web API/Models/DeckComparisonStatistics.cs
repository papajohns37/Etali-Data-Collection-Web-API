using System.ComponentModel;

namespace Etali_Data_Collection_Web_API.Models;

/// <summary>
/// Represents comprehensive statistical comparison between two decklists.
/// All differences are calculated as (Deck1 - Deck2), so positive values favor Deck1.
/// 
/// Statistics combine comparison-specific evaluations with filtered main dataset evaluations
/// for improved statistical precision. Main dataset hands are filtered per-comparison to only
/// include those containing at least one card unique to the respective deck.
/// 
/// Sample sizes used for statistical inference are the counts of evaluated hands (not generation attempts),
/// as only hands containing unique cards provide information about performance differences.
/// </summary>
/// <param name="ComparisonId">The unique identifier of the deck comparison.</param>
/// <param name="Deck1Id">The database ID of the first decklist.</param>
/// <param name="Deck1Name">The name of the first decklist.</param>
/// <param name="Deck2Id">The database ID of the second decklist.</param>
/// <param name="Deck2Name">The name of the second decklist.</param>
/// <param name="Deck1TotalHandsEvaluated">Total hands evaluated for deck 1 (comparison-specific + filtered main dataset). This is the sample size used for statistical calculations.</param>
/// <param name="Deck2TotalHandsEvaluated">Total hands evaluated for deck 2 (comparison-specific + filtered main dataset). This is the sample size used for statistical calculations.</param>
/// <param name="HandSizeDeltaStatistics">Array of delta statistics for each hand size (3-7 cards).</param>
/// <param name="OverallProbabilityDifference">Difference in overall mulligan probability (Deck1 - Deck2).</param>
/// <param name="OverallConfidenceIntervalLower">Lower bound of 90% confidence interval for overall difference.</param>
/// <param name="OverallConfidenceIntervalUpper">Upper bound of 90% confidence interval for overall difference.</param>
/// <example>
/// {
///   "comparisonId": 1,
///   "deck1Id": 5,
///   "deck1Name": "Fast Etali",
///   "deck2Id": 8,
///   "deck2Name": "Budget Etali",
///   "deck1TotalHandsEvaluated": 45,
///   "deck2TotalHandsEvaluated": 42,
///   "handSizeDeltaStatistics": [
///     {
///       "handSize": 7,
///       "deck1SuccessfulHands": 32,
///       "deck2SuccessfulHands": 25,
///       "deck1Probability": 0.71,
///       "deck2Probability": 0.60,
///       "probabilityDifference": 0.11,
///       "confidenceIntervalLower": 0.02,
///       "confidenceIntervalUpper": 0.20
///     }
///   ],
///   "overallProbabilityDifference": 0.08,
///   "overallConfidenceIntervalLower": 0.01,
///   "overallConfidenceIntervalUpper": 0.15
/// }
/// </example>
public record DeckComparisonStatistics(
    [property: Description("Unique identifier of the deck comparison")]
    int ComparisonId,
    
    [property: Description("Database ID of the first decklist")]
    int Deck1Id,
    
    [property: Description("Name of the first decklist")]
    string Deck1Name,
    
    [property: Description("Database ID of the second decklist")]
    int Deck2Id,
    
    [property: Description("Name of the second decklist")]
    string Deck2Name,
    
    [property: Description("Total number of hands evaluated for deck 1 (sample size for statistics)")]
    int Deck1TotalHandsEvaluated,
    
    [property: Description("Total number of hands evaluated for deck 2 (sample size for statistics)")]
    int Deck2TotalHandsEvaluated,
    
    [property: Description("Delta statistics for each hand size (3-7 cards)")]
    HandSizeDeltaStatistics[] HandSizeDeltaStatistics,
    
    [property: Description("Difference in overall mulligan probability (Deck1 - Deck2)")]
    double OverallProbabilityDifference,
    
    [property: Description("Lower bound of 90% CI for overall difference")]
    double OverallConfidenceIntervalLower,
    
    [property: Description("Upper bound of 90% CI for overall difference")]
    double OverallConfidenceIntervalUpper
);
