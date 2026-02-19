using System.ComponentModel;

namespace Etali_Data_Collection_Web_API.Models;

/// <summary>
/// Result of comparing mulligan statistics between two decklists using statistical tests.
/// </summary>
/// <param name="Decklist1Stats">Statistics for the first decklist.</param>
/// <param name="Decklist2Stats">Statistics for the second decklist.</param>
/// <param name="SuccessRateDifference">Difference in success rates (Deck1 - Deck2) in percentage points.</param>
/// <param name="StatisticalTest">Result of the two-proportion z-test.</param>
/// <example>
/// {
///   "decklist1Stats": { ... },
///   "decklist2Stats": { ... },
///   "successRateDifference": 8.5,
///   "statisticalTest": {
///     "zScore": 2.34,
///     "pValue": 0.019,
///     "isSignificant": true,
///     "interpretation": "Deck 'Fast Etali' has a statistically significantly higher success rate than 'Slow Etali' (p = 0.019)"
///   }
/// }
/// </example>
public record FullMulliganComparisonResult(
    [property: Description("Statistics for the first decklist")]
    FullMulliganStatistics Decklist1Stats,
    
    [property: Description("Statistics for the second decklist")]
    FullMulliganStatistics Decklist2Stats,
    
    [property: Description("Difference in success rates (percentage points)")]
    double SuccessRateDifference,
    
    [property: Description("Result of the two-proportion z-test")]
    TwoProportionTestResult StatisticalTest
);
