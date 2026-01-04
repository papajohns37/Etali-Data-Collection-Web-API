using Etali_Data_Collection_Web_API.Models;

namespace Etali_Data_Collection_Web_API.Services;

/// <summary>
/// Service for calculating statistical analysis of decklist hand testing data.
/// </summary>
public interface IStatisticsService
{
    /// <summary>
    /// Calculates comprehensive statistics for a specific decklist.
    /// </summary>
    /// <param name="decklistId">The unique identifier of the decklist.</param>
    /// <returns>
    /// A <see cref="DecklistStatistics"/> object containing success rates, confidence intervals,
    /// and overall mulligan probability. Returns null if the decklist is not found.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Statistics are calculated based on all evaluated hands for the specified decklist.
    /// For each hand size (3-7 cards), the service calculates:
    /// </para>
    /// <list type="bullet">
    /// <item>Total hands that could cast Etali with N or fewer cards</item>
    /// <item>Success rate (probability)</item>
    /// <item>90% confidence intervals using Wilson score interval method</item>
    /// </list>
    /// <para>
    /// The overall mulligan probability represents the chance of casting Etali by turn 2
    /// after going through the full mulligan sequence: two 7-card hands, then 6, 5, 4, and 3-card hands.
    /// Formula: 1 - (1 - p7)² × (1 - p6) × (1 - p5) × (1 - p4) × (1 - p3)
    /// </para>
    /// <para>
    /// If no hands have been tested for the decklist, returns a statistics object with
    /// totalHandsTested = 0 and empty handSizeStatistics array.
    /// </para>
    /// </remarks>
    Task<DecklistStatistics?> GetDecklistStatistics(int decklistId);
}
