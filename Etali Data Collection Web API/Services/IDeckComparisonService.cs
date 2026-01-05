namespace Etali_Data_Collection_Web_API.Services;

using Etali_Data_Collection_Web_API.Models;

/// <summary>
/// Service for managing deck comparisons and their associated evaluations.
/// </summary>
public interface IDeckComparisonService
{
    /// <summary>
    /// Creates a new deck comparison between two decklists.
    /// </summary>
    /// <param name="deck1Id">The ID of the first decklist.</param>
    /// <param name="deck2Id">The ID of the second decklist.</param>
    /// <returns>The created deck comparison.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when either decklist does not exist or when a comparison between these decks already exists.
    /// </exception>
    Task<DeckComparison> AddDeckComparisonAsync(int deck1Id, int deck2Id);

    /// <summary>
    /// Gets all deck comparisons in the system.
    /// </summary>
    /// <returns>Array of all deck comparisons.</returns>
    Task<DeckComparison[]> GetAllComparisonsAsync();

    /// <summary>
    /// Gets a specific deck comparison by ID.
    /// </summary>
    /// <param name="comparisonId">The ID of the comparison to retrieve.</param>
    /// <returns>The deck comparison, or null if not found.</returns>
    Task<DeckComparison?> GetComparisonByIdAsync(int comparisonId);

    /// <summary>
    /// Gets all comparisons that include a specific decklist.
    /// </summary>
    /// <param name="decklistId">The ID of the decklist.</param>
    /// <returns>Array of deck comparisons that include the specified decklist.</returns>
    Task<DeckComparison[]> GetComparisonsForDeckAsync(int decklistId);

    /// <summary>
    /// Gets the card differences between any two decks by their IDs.
    /// </summary>
    /// <param name="deck1Id">The ID of the first decklist.</param>
    /// <param name="deck2Id">The ID of the second decklist.</param>
    /// <returns>The card differences, or null if either deck not found.</returns>
    Task<CardDifferences?> GetCardDifferencesByDeckIdsAsync(int deck1Id, int deck2Id);

    /// <summary>
    /// Generates a hand for evaluation from the deck comparison.
    /// The hand will be from the deck with the lower N value and will contain at least one card unique to that deck.
    /// </summary>
    /// <param name="comparisonId">The ID of the comparison.</param>
    /// <returns>A hand for evaluation with generation attempt count, or null if comparison not found.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no unique cards can be found after reasonable attempts.
    /// </exception>
    Task<ComparisonHand?> GenerateHandForComparisonAsync(int comparisonId);

    /// <summary>
    /// Submits an evaluated hand for a comparison and updates the N value.
    /// </summary>
    /// <param name="comparisonId">The ID of the comparison.</param>
    /// <param name="whichDeck">Which deck the hand came from (1 or 2).</param>
    /// <param name="evaluatedHand">The evaluated hand data.</param>
    /// <param name="generationAttempts">The number of generation attempts to add to N.</param>
    /// <returns>The stored comparison evaluated hand, or null if comparison not found.</returns>
    Task<ComparisonEvaluatedHand?> SubmitComparisonEvaluationAsync(
        int comparisonId,
        int whichDeck,
        EvaluatedHand evaluatedHand,
        int generationAttempts);

    /// <summary>
    /// Gets statistical comparison between two decks.
    /// </summary>
    /// <param name="comparisonId">The ID of the comparison.</param>
    /// <returns>The comparison statistics, or null if comparison not found or insufficient data.</returns>
    Task<DeckComparisonStatistics?> GetComparisonStatisticsAsync(int comparisonId);
}
