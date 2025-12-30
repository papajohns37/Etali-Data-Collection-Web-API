using Etali_Data_Collection_Web_API.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

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
    private readonly EtaliDatabase _database;

    /// <summary>
    /// Initializes a new instance of the <see cref="HandEvaluationService"/> class.
    /// </summary>
    public HandEvaluationService(EtaliDatabase etaliDatabase)
    {
        _database = etaliDatabase;
    }

    /// <inheritdoc/>
    public void RecordEvaluation(EvaluatedHand evaluation)
    {
        _database.EvaluatedHands.Add(evaluation);
        _database.SaveChanges();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<EvaluatedHand>> GetAllEvaluations()
    {
        return await _database.EvaluatedHands.ToListAsync();
    }

    /// <inheritdoc/>
    public int GetNextHandId()
    {
        return _database.EvaluatedHands.Any() ? _database.EvaluatedHands.Max(h => h.Id) + 1 : 1;
    }
}
