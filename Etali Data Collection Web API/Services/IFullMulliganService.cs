using Etali_Data_Collection_Web_API.Models;

namespace Etali_Data_Collection_Web_API.Services;

/// <summary>
/// Service interface for managing full mulligan sessions and statistics.
/// </summary>
public interface IFullMulliganService
{
    /// <summary>
    /// Starts a new mulligan session for the specified decklist.
    /// </summary>
    /// <param name="decklistId">The ID of the decklist to test.</param>
    /// <param name="userId">The ID of the user starting the session.</param>
    /// <returns>Response containing the session ID and first hand.</returns>
    Task<FullMulliganStartResponse> StartSessionAsync(int decklistId, string userId);
    
    /// <summary>
    /// Evaluates the current hand and progresses the mulligan session.
    /// </summary>
    /// <param name="request">The evaluation request containing user's decision.</param>
    /// <param name="userId">The ID of the user evaluating the hand.</param>
    /// <returns>Response containing either the next hand or session summary.</returns>
    Task<FullMulliganEvaluationResponse> EvaluateHandAsync(FullMulliganEvaluationRequest request, string userId);
    
    /// <summary>
    /// Gets mulligan statistics for a specific decklist.
    /// </summary>
    /// <param name="decklistId">The ID of the decklist.</param>
    /// <returns>Statistics for the specified decklist.</returns>
    Task<FullMulliganStatistics> GetStatisticsAsync(int decklistId);
    
    /// <summary>
    /// Gets mulligan statistics for all decklists.
    /// </summary>
    /// <returns>List of statistics for all decklists with mulligan data.</returns>
    Task<List<FullMulliganStatistics>> GetAllStatisticsAsync();
    
    /// <summary>
    /// Compares mulligan statistics between two decklists using a two-proportion z-test.
    /// </summary>
    /// <param name="decklistId1">The ID of the first decklist.</param>
    /// <param name="decklistId2">The ID of the second decklist.</param>
    /// <returns>Comparison result with statistical test.</returns>
    Task<FullMulliganComparisonResult> CompareDecksAsync(int decklistId1, int decklistId2);
}
