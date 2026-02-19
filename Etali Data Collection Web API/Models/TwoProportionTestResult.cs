using System.ComponentModel;

namespace Etali_Data_Collection_Web_API.Models;

/// <summary>
/// Result of a two-proportion z-test comparing success rates between two groups.
/// </summary>
/// <param name="ZScore">The calculated z-score from the test.</param>
/// <param name="PValue">The two-tailed p-value (0-1).</param>
/// <param name="IsSignificant">Whether the result is statistically significant at α = 0.05.</param>
/// <param name="Interpretation">Human-readable interpretation of the test results.</param>
/// <example>
/// {
///   "zScore": 2.34,
///   "pValue": 0.019,
///   "isSignificant": true,
///   "interpretation": "Deck 'Fast Etali' has a statistically significantly higher success rate than 'Slow Etali' (p = 0.019)"
/// }
/// </example>
public record TwoProportionTestResult(
    [property: Description("The calculated z-score from the test")]
    double ZScore,
    
    [property: Description("The two-tailed p-value (0-1)")]
    double PValue,
    
    [property: Description("Whether the result is statistically significant at α = 0.05")]
    bool IsSignificant,
    
    [property: Description("Human-readable interpretation of the results")]
    string Interpretation
);
