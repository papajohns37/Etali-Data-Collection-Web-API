using Etali_Data_Collection_Web_API.Models;

namespace Etali_Data_Collection_Web_API.Services;

/// <summary>
/// Service responsible for managing hand evaluations and tracking hand IDs.
/// </summary>
/// <remarks>
/// This service maintains an in-memory collection of evaluated hands and manages
/// the sequential assignment of hand IDs. In a production environment, this should
/// be replaced with a persistent storage solution.
/// </remarks>
public class HandEvaluationService : IHandEvaluationService
{
    private readonly List<EvaluatedHand> _evaluatedHands;
    private int _currentHandId;

    /// <summary>
    /// Initializes a new instance of the <see cref="HandEvaluationService"/> class.
    /// </summary>
    public HandEvaluationService()
    {
        _evaluatedHands = new List<EvaluatedHand>();
        _currentHandId = 0;
    }

    /// <inheritdoc/>
    public void RecordEvaluation(EvaluatedHand evaluation)
    {
        _evaluatedHands.Add(evaluation);
    }

    /// <inheritdoc/>
    public IReadOnlyList<EvaluatedHand> GetAllEvaluations()
    {
        return _evaluatedHands.AsReadOnly();
    }

    /// <inheritdoc/>
    public int GetNextHandId()
    {
        return ++_currentHandId;
    }
}
