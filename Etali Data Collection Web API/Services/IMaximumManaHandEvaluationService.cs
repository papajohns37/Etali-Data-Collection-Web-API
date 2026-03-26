using Etali_Data_Collection_Web_API.Models;

namespace Etali_Data_Collection_Web_API.Services;

public interface IMaximumManaHandEvaluationService
{
    Task RecordEvaluation(MaximumManaEvaluatedHand evaluation);

    Task<IReadOnlyList<MaximumManaEvaluatedHand>> GetAllEvaluations();

    int GetNextHandId();
}
