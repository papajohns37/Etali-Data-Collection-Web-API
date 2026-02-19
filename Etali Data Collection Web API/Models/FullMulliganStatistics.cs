using System.ComponentModel;

namespace Etali_Data_Collection_Web_API.Models;

/// <summary>
/// Statistics for full mulligan sessions for a specific decklist.
/// </summary>
/// <param name="DecklistId">The ID of the decklist.</param>
/// <param name="DecklistName">The name of the decklist.</param>
/// <param name="TotalSessions">Total number of completed mulligan sessions.</param>
/// <param name="SuccessfulSessions">Number of sessions where a castable hand was found.</param>
/// <param name="SuccessRate">Percentage of sessions that found a castable hand (0-100).</param>
/// <param name="AverageMulligans">Average number of mulligans per session.</param>
/// <param name="SuccessByHandSize">Breakdown of successful sessions by final hand size kept.</param>
/// <param name="MulliganDistribution">Distribution of sessions by number of mulligans taken.</param>
/// <param name="TotalSerumPowderActivations">Total number of Serum Powder activations across all sessions.</param>
/// <param name="AverageSerumPowderActivations">Average number of Serum Powder activations per session.</param>
/// <param name="SessionsWithSerumPowder">Number of sessions where Serum Powder was activated at least once.</param>
/// <param name="SuccessRateWithSerumPowder">Success rate for sessions that used Serum Powder.</param>
/// <param name="SuccessRateWithoutSerumPowder">Success rate for sessions that did not use Serum Powder.</param>
/// <example>
/// {
///   "decklistId": 1,
///   "decklistName": "Fast Etali",
///   "totalSessions": 150,
///   "successfulSessions": 98,
///   "successRate": 65.33,
///   "averageMulligans": 1.8,
///   "successByHandSize": {
///     "7": 45,
///     "6": 28,
///     "5": 18,
///     "4": 5,
///     "3": 2
///   },
///   "mulliganDistribution": {
///     "0": 45,
///     "1": 35,
///     "2": 28,
///     "3": 20,
///     "4": 15,
///     "5": 7
///   },
///   "totalSerumPowderActivations": 23,
///   "averageSerumPowderActivations": 0.15,
///   "sessionsWithSerumPowder": 23,
///   "successRateWithSerumPowder": 73.91,
///   "successRateWithoutSerumPowder": 63.78
/// }
/// </example>
public record FullMulliganStatistics(
    [property: Description("The ID of the decklist")]
    int DecklistId,

    [property: Description("The name of the decklist")]
    string DecklistName,

    [property: Description("Total number of completed mulligan sessions")]
    int TotalSessions,

    [property: Description("Number of sessions where a castable hand was found")]
    int SuccessfulSessions,

    [property: Description("Percentage of sessions that found a castable hand (0-100)")]
    double SuccessRate,

    [property: Description("Average number of mulligans per session")]
    double AverageMulligans,

    [property: Description("Breakdown of successful sessions by final hand size")]
    Dictionary<int, int> SuccessByHandSize,

    [property: Description("Distribution of sessions by number of mulligans taken")]
    Dictionary<int, int> MulliganDistribution,

    [property: Description("Total Serum Powder activations across all sessions")]
    int TotalSerumPowderActivations,

    [property: Description("Average Serum Powder activations per session")]
    double AverageSerumPowderActivations,

    [property: Description("Number of sessions with at least one Serum Powder activation")]
    int SessionsWithSerumPowder,

    [property: Description("Success rate for sessions that used Serum Powder")]
    double SuccessRateWithSerumPowder,

    [property: Description("Success rate for sessions without Serum Powder")]
    double SuccessRateWithoutSerumPowder
);
