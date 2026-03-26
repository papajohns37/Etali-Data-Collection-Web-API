using Etali_Data_Collection_Web_API.Models;
using Microsoft.EntityFrameworkCore;

namespace Etali_Data_Collection_Web_API.Services;

/// <summary>
/// Service responsible for managing maximum mana hand evaluations and tracking hand IDs.
/// </summary>
public class MaximumManaHandEvaluationService(EtaliDatabase database) : IMaximumManaHandEvaluationService
{
    /// <inheritdoc/>
    public async Task RecordEvaluation(MaximumManaEvaluatedHand evaluation)
    {
        database.MaximumManaEvaluatedHands.Add(evaluation);
        await database.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<MaximumManaEvaluatedHand>> GetAllEvaluations()
    {
        return await database.MaximumManaEvaluatedHands.ToListAsync();
    }

    /// <inheritdoc/>
    public int GetNextHandId()
    {
        return database.MaximumManaEvaluatedHands.Any() ? database.MaximumManaEvaluatedHands.Max(h => h.Id) + 1 : 1;
    }
}
