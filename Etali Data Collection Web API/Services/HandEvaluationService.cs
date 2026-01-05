using Etali_Data_Collection_Web_API.Models;
using Microsoft.EntityFrameworkCore;

namespace Etali_Data_Collection_Web_API.Services;

/// <summary>
/// Service responsible for managing hand evaluations and tracking hand IDs.
/// </summary>
/// <remarks>
/// This service maintains evaluations in the database (currently using in-memory DB for development).
/// In a production environment, configure a persistent database provider.
/// </remarks>
public class HandEvaluationService(EtaliDatabase database) : IHandEvaluationService
{
    /// <inheritdoc/>
    public async Task RecordEvaluation(EvaluatedHand evaluation)
    {
        database.EvaluatedHands.Add(evaluation);
        await database.SaveChangesAsync();
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
