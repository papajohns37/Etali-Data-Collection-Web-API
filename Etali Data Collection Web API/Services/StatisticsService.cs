using Etali_Data_Collection_Web_API.Models;
using Microsoft.EntityFrameworkCore;

namespace Etali_Data_Collection_Web_API.Services;

/// <summary>
/// Service for calculating statistical analysis of decklist hand testing data.
/// </summary>
public class StatisticsService(EtaliDatabase database) : IStatisticsService
{
    private const double ZScore90Percent = 1.645; // Z-score for 90% confidence interval
    private static readonly int[] HandSizes = [7, 6, 5, 4, 3];

    /// <inheritdoc/>
    public async Task<DecklistStatistics?> GetDecklistStatistics(int decklistId)
    {
        // Verify decklist exists
        var decklist = await database.Decklists.FirstOrDefaultAsync(d => d.Id == decklistId);
        if (decklist == null)
        {
            return null;
        }

        // Get all evaluated hands for this decklist
        var hands = await database.EvaluatedHands
            .Where(h => h.DecklistId == decklistId)
            .ToListAsync();

        int totalHandsTested = hands.Count;

        // If no hands tested, return empty statistics
        if (totalHandsTested == 0)
        {
            return new DecklistStatistics(
                DecklistId: decklistId,
                DecklistName: decklist.Name,
                TotalHandsTested: 0,
                HandSizeStatistics: [],
                OverallMulliganProbability: 0,
                OverallMulliganConfidenceIntervalLower: 0,
                OverallMulliganConfidenceIntervalUpper: 0
            );
        }

        // Calculate statistics for each hand size (7, 6, 5, 4, 3)
        var handSizeStats = new List<HandSizeStatistics>();
        var probabilities = new Dictionary<int, (double probability, double ciLower, double ciUpper)>();

        foreach (int handSize in HandSizes)
        {
            var stats = CalculateHandSizeStatistics(hands, handSize);
            handSizeStats.Add(stats);
            probabilities[handSize] = (stats.Probability, stats.ConfidenceIntervalLower, stats.ConfidenceIntervalUpper);
        }

        // Calculate overall mulligan probability
        // Formula: 1 - (1 - p7)^2 * (1 - p6) * (1 - p5) * (1 - p4) * (1 - p3)
        var (overallProb, overallCILower, overallCIUpper) = CalculateOverallMulliganProbability(probabilities);

        return new DecklistStatistics(
            DecklistId: decklistId,
            DecklistName: decklist.Name,
            TotalHandsTested: totalHandsTested,
            HandSizeStatistics: [.. handSizeStats],
            OverallMulliganProbability: overallProb,
            OverallMulliganConfidenceIntervalLower: overallCILower,
            OverallMulliganConfidenceIntervalUpper: overallCIUpper
        );
    }

    /// <summary>
    /// Calculates statistics for a specific hand size.
    /// </summary>
    /// <param name="hands">All evaluated hands for the decklist.</param>
    /// <param name="handSize">The hand size to calculate statistics for (3-7).</param>
    /// <returns>Statistics for the specified hand size.</returns>
    private static HandSizeStatistics CalculateHandSizeStatistics(List<EvaluatedHand> hands, int handSize)
    {
        // A hand is successful at size N if minimumCardsToCastEtali <= N
        // This means the hand can cast Etali with N or fewer cards
        int totalHands = hands.Count;
        int successfulHands = hands.Count(h => h.CanCastEtali && h.MinimumCardsToCastEtali <= handSize);

        double probability = totalHands > 0 ? (double)successfulHands / totalHands : 0;

        // Calculate Wilson score confidence interval
        var (ciLower, ciUpper) = CalculateWilsonScoreInterval(successfulHands, totalHands, ZScore90Percent);

        return new HandSizeStatistics(
            HandSize: handSize,
            TotalHands: totalHands,
            SuccessfulHands: successfulHands,
            Probability: probability,
            ConfidenceIntervalLower: ciLower,
            ConfidenceIntervalUpper: ciUpper
        );
    }

    /// <summary>
    /// Calculates the Wilson score confidence interval for a binomial proportion.
    /// </summary>
    /// <param name="successes">Number of successes.</param>
    /// <param name="total">Total number of trials.</param>
    /// <param name="zScore">Z-score for desired confidence level (1.645 for 90%).</param>
    /// <returns>Tuple of (lower bound, upper bound) for the confidence interval.</returns>
    /// <remarks>
    /// The Wilson score interval is more accurate than the normal approximation,
    /// especially for small sample sizes or extreme probabilities (near 0 or 1).
    /// Formula: https://en.wikipedia.org/wiki/Binomial_proportion_confidence_interval#Wilson_score_interval
    /// </remarks>
    private static (double lower, double upper) CalculateWilsonScoreInterval(int successes, int total, double zScore)
    {
        if (total == 0)
        {
            return (0, 0);
        }

        double n = total;
        double p = successes / n;
        double z2 = zScore * zScore;

        // Wilson score interval formula
        double denominator = 1 + z2 / n;
        double centerAdjustment = p + z2 / (2 * n);
        double marginOfError = zScore * Math.Sqrt((p * (1 - p) + z2 / (4 * n)) / n);

        double lower = (centerAdjustment - marginOfError) / denominator;
        double upper = (centerAdjustment + marginOfError) / denominator;

        // Clamp to [0, 1] range
        lower = Math.Max(0, Math.Min(1, lower));
        upper = Math.Max(0, Math.Min(1, upper));

        return (lower, upper);
    }

    /// <summary>
    /// Calculates the overall mulligan probability and its confidence interval.
    /// </summary>
    /// <param name="probabilities">Dictionary mapping hand size to (probability, CI lower, CI upper).</param>
    /// <returns>Tuple of (overall probability, CI lower, CI upper).</returns>
    /// <remarks>
    /// Formula: 1 - (1 - p7)^2 * (1 - p6) * (1 - p5) * (1 - p4) * (1 - p3)
    /// This represents seeing two 7-card hands, then one each of 6, 5, 4, and 3 card hands.
    /// 
    /// Confidence interval uses propagation of uncertainty:
    /// - Uses lower bounds to calculate lower CI (worst case)
    /// - Uses upper bounds to calculate upper CI (best case)
    /// </remarks>
    private static (double probability, double ciLower, double ciUpper) CalculateOverallMulliganProbability(
        Dictionary<int, (double probability, double ciLower, double ciUpper)> probabilities)
    {
        // Extract probabilities
        var p7 = probabilities.TryGetValue(7, out var p7Data) ? p7Data.probability : 0;
        var p6 = probabilities.TryGetValue(6, out var p6Data) ? p6Data.probability : 0;
        var p5 = probabilities.TryGetValue(5, out var p5Data) ? p5Data.probability : 0;
        var p4 = probabilities.TryGetValue(4, out var p4Data) ? p4Data.probability : 0;
        var p3 = probabilities.TryGetValue(3, out var p3Data) ? p3Data.probability : 0;

        // Calculate overall probability
        double overallProb = 1 - Math.Pow(1 - p7, 2) * (1 - p6) * (1 - p5) * (1 - p4) * (1 - p3);

        // Calculate confidence interval using propagation of uncertainty
        // For lower bound: use lower CI values (worst case scenario)
        var p7Lower = p7Data.ciLower;
        var p6Lower = p6Data.ciLower;
        var p5Lower = p5Data.ciLower;
        var p4Lower = p4Data.ciLower;
        var p3Lower = p3Data.ciLower;

        double ciLower = 1 - Math.Pow(1 - p7Lower, 2) * (1 - p6Lower) * (1 - p5Lower) * (1 - p4Lower) * (1 - p3Lower);

        // For upper bound: use upper CI values (best case scenario)
        var p7Upper = p7Data.ciUpper;
        var p6Upper = p6Data.ciUpper;
        var p5Upper = p5Data.ciUpper;
        var p4Upper = p4Data.ciUpper;
        var p3Upper = p3Data.ciUpper;

        double ciUpper = 1 - Math.Pow(1 - p7Upper, 2) * (1 - p6Upper) * (1 - p5Upper) * (1 - p4Upper) * (1 - p3Upper);

        // Clamp to [0, 1] range
        ciLower = Math.Max(0, Math.Min(1, ciLower));
        ciUpper = Math.Max(0, Math.Min(1, ciUpper));

        return (overallProb, ciLower, ciUpper);
    }
}
