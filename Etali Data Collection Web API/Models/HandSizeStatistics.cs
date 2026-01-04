using System.ComponentModel;

namespace Etali_Data_Collection_Web_API.Models;

/// <summary>
/// Represents statistical data for a specific hand size (number of cards in hand).
/// </summary>
/// <param name="HandSize">The number of cards in hand (3, 4, 5, 6, or 7).</param>
/// <param name="TotalHands">Total number of hands tested that could potentially cast Etali with this many cards.</param>
/// <param name="SuccessfulHands">Number of hands where Etali can be cast by turn 2 with this many cards.</param>
/// <param name="Probability">The success rate: SuccessfulHands / TotalHands. Range: 0.0 to 1.0.</param>
/// <param name="ConfidenceIntervalLower">Lower bound of the 90% confidence interval for the probability.</param>
/// <param name="ConfidenceIntervalUpper">Upper bound of the 90% confidence interval for the probability.</param>
/// <example>
/// {
///   "handSize": 7,
///   "totalHands": 250,
///   "successfulHands": 175,
///   "probability": 0.70,
///   "confidenceIntervalLower": 0.6421,
///   "confidenceIntervalUpper": 0.7521
/// }
/// </example>
public record HandSizeStatistics(
    [property: Description("Number of cards in hand (3-7)")]
    int HandSize,
    
    [property: Description("Total number of hands tested at this hand size")]
    int TotalHands,
    
    [property: Description("Number of successful hands (can cast Etali by turn 2)")]
    int SuccessfulHands,
    
    [property: Description("Success rate (0.0 to 1.0)")]
    double Probability,
    
    [property: Description("Lower bound of 90% confidence interval")]
    double ConfidenceIntervalLower,
    
    [property: Description("Upper bound of 90% confidence interval")]
    double ConfidenceIntervalUpper
);
