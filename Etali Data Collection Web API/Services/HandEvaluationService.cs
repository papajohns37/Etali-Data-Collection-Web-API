using Etali_Data_Collection_Web_API.Models;
using Microsoft.EntityFrameworkCore;

namespace Etali_Data_Collection_Web_API.Services;

/// <summary>
/// Service responsible for managing hand evaluations and tracking hand IDs.
/// </summary>
/// <remarks>
/// This service maintains evaluations in the database.
/// Old hands are removed when served (in HandGenerationService), not when evaluated.
/// </remarks>
public class HandEvaluationService(EtaliDatabase database, ILogger<HandEvaluationService> logger) : IHandEvaluationService
{
    /// <inheritdoc/>
    public async Task RecordEvaluation(EvaluatedHand evaluation)
    {
        // Old hands are already removed when served (see HandGenerationService)
        // Just save the evaluation
        database.EvaluatedHands.Add(evaluation);
        await database.SaveChangesAsync();
        
        logger.LogDebug("Recorded evaluation for hand {HandId} from decklist '{DecklistName}'",
            evaluation.Id, evaluation.DecklistName);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<EvaluatedHand>> GetAllEvaluations()
    {
        return await database.EvaluatedHands.ToListAsync();
    }

    /// <inheritdoc/>
    public int GetNextHandId()
    {
        return database.EvaluatedHands.Any() ? database.EvaluatedHands.Max(h => h.Id) + 1 : 1;
    }
}
