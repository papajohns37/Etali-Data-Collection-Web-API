using Etali_Data_Collection_Web_API.Models;

namespace Etali_Data_Collection_Web_API.Services;

/// <summary>
/// Defines the contract for managing hand evaluations.
/// </summary>
public interface IHandEvaluationService
{
    /// <summary>
    /// Records an evaluated hand in the collection.
    /// </summary>
    /// <param name="evaluation">The evaluated hand to record.</param>
    void RecordEvaluation(EvaluatedHand evaluation);

    /// <summary>
    /// Retrieves all recorded hand evaluations.
    /// </summary>
    /// <returns>A read-only collection of all evaluated hands.</returns>
    Task<IReadOnlyList<EvaluatedHand>> GetAllEvaluations();

    /// <summary>
    /// Gets the next available hand ID.
    /// </summary>
    /// <returns>The next hand ID to be assigned.</returns>
    int GetNextHandId();
}
