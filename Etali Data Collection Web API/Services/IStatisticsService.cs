namespace Etali_Data_Collection_Web_API.Services;

using Etali_Data_Collection_Web_API.Models;

public interface IStatisticsService
{
    /// <summary>
    /// Gets comprehensive statistics for a specific decklist.
    /// </summary>
    /// <param name="decklistId">The ID of the decklist to analyze.</param>
    /// <returns>Statistics for the decklist, or null if the decklist doesn't exist.</returns>
    Task<DecklistStatistics?> GetDecklistStatistics(int decklistId);
}
