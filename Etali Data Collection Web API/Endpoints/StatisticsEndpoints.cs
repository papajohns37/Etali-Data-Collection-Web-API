namespace Etali_Data_Collection_Web_API.Endpoints;

using Etali_Data_Collection_Web_API.Models;
using Etali_Data_Collection_Web_API.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

public static class StatisticsEndpoints
{
    public static void MapStatisticsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/decklists/{decklistId:int}/statistics").WithTags("Statistics");

        group.MapGet("", GetDecklistStatistics)
            .WithName("GetDecklistStatistics")
            .WithOpenApi(operation => new(operation)
            {
                Summary = "Get decklist statistics",
                Description = @"Returns comprehensive statistical analysis for a specific decklist.

Statistics include:
- Success rates for each hand size (3-7 cards)
- 90% confidence intervals using Wilson score interval method
- Overall mulligan probability calculation
- Confidence intervals for overall probability

Hand size logic:
A hand with minimumCardsToCastEtali=4 counts as success for hand sizes 4, 5, 6, and 7.

Mulligan scenario:
Simulates drawing two 7-card hands, then 6, 5, 4, and 3-card hands if needed.

If no hands have been tested yet, returns:
- totalHandsTested: 0
- handSizeStatistics: []
- overallMulliganProbability: 0"
            })
            .Produces<DecklistStatistics>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);
    }

    private static async Task<Results<Ok<DecklistStatistics>, NotFound<ProblemDetails>>> GetDecklistStatistics(
        int decklistId,
        IStatisticsService statisticsService)
    {
        var statistics = await statisticsService.GetDecklistStatistics(decklistId);

        if (statistics == null)
        {
            return TypedResults.NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Decklist not found",
                Detail = $"Decklist with ID {decklistId} not found."
            });
        }

        return TypedResults.Ok(statistics);
    }
}
