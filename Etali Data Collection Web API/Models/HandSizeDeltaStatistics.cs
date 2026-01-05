using System.ComponentModel;

namespace Etali_Data_Collection_Web_API.Models;

/// <summary>
/// Represents the difference in statistics for a specific hand size between two decks.
/// All values represent (Deck1 - Deck2), so positive values favor Deck1.
/// </summary>
/// <param name="HandSize">The number of cards in hand (3, 4, 5, 6, or 7).</param>
/// <param name="Deck1SuccessfulHands">Number of successful hands for deck 1 at this hand size.</param>
/// <param name="Deck2SuccessfulHands">Number of successful hands for deck 2 at this hand size.</param>
/// <param name="Deck1Probability">The success rate for deck 1 (0.0 to 1.0).</param>
/// <param name="Deck2Probability">The success rate for deck 2 (0.0 to 1.0).</param>
/// <param name="ProbabilityDifference">The difference in success rates (Deck1 - Deck2). Positive means Deck1 performs better.</param>
/// <param name="ConfidenceIntervalLower">Lower bound of the 90% confidence interval for the difference.</param>
/// <param name="ConfidenceIntervalUpper">Upper bound of the 90% confidence interval for the difference.</param>
/// <example>
/// {
///   "handSize": 7,
///   "deck1SuccessfulHands": 175,
///   "deck2SuccessfulHands": 150,
///   "deck1Probability": 0.70,
///   "deck2Probability": 0.60,
///   "probabilityDifference": 0.10,
///   "confidenceIntervalLower": 0.02,
///   "confidenceIntervalUpper": 0.18
/// }
/// </example>
public record HandSizeDeltaStatistics(
    [property: Description("Number of cards in hand (3-7)")]
    int HandSize,
    
    [property: Description("Number of successful hands for deck 1")]
    int Deck1SuccessfulHands,
    
    [property: Description("Number of successful hands for deck 2")]
    int Deck2SuccessfulHands,
    
    [property: Description("Success rate for deck 1 (0.0 to 1.0)")]
    double Deck1Probability,
    
    [property: Description("Success rate for deck 2 (0.0 to 1.0)")]
    double Deck2Probability,
    
    [property: Description("Difference in success rates (Deck1 - Deck2)")]
    double ProbabilityDifference,
    
    [property: Description("Lower bound of 90% confidence interval for the difference")]
    double ConfidenceIntervalLower,
    
    [property: Description("Upper bound of 90% confidence interval for the difference")]
    double ConfidenceIntervalUpper
);
