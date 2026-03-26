namespace Etali_Data_Collection_Web_API.Endpoints;

using Etali_Data_Collection_Web_API.Models;
using Etali_Data_Collection_Web_API.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

public static class MaximumManaHandEndpoints
{
    public static void MapMaximumManaHandEndpoints(this WebApplication app)
    {
        app.MapGet("/maximum-mana-hand", GenerateHand)
            .WithName("GenerateMaximumManaHand")
            .WithTags("Maximum Mana Hands")
            .WithOpenApi(operation => new(operation)
            {
                Summary = "Generate a random hand for maximum mana dataset",
                Description = @"Generates a new random hand of 7 cards from the specified decklist for the maximum mana dataset flow.

The generated hand includes all standard metadata:
- Cards
- Seat order
- Stickers
- Unique hand ID
- Lowest Gamble odds

**Authentication:** Required (Contributor or Admin role)
**Authorization:** Bearer token in Authorization header"
            })
            .Produces<MaximumManaHand>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .RequireAuthorization("ContributorOrAdmin");

        app.MapPost("/maximum-mana-hand/evaluate", EvaluateHand)
            .WithName("EvaluateMaximumManaHand")
            .WithTags("Maximum Mana Hands")
            .WithOpenApi(operation => new(operation)
            {
                Summary = "Evaluate a maximum mana hand",
                Description = @"Records the user's evaluation for the maximum mana hand dataset.

Request body must include all fields from the generated hand plus:
- maximumManaIn7CardHand
- maximumManaIn6CardHand
- maximumManaIn5CardHand
- maximumManaIn4CardHand
- maximumManaIn3CardHand

**Authentication:** Required (Contributor or Admin role)
**Authorization:** Bearer token in Authorization header"
            })
            .Produces(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .RequireAuthorization("ContributorOrAdmin");

        app.MapGet("/maximum-mana-hands/evaluated", GetEvaluatedHands)
            .WithName("GetMaximumManaEvaluatedHands")
            .WithTags("Maximum Mana Hands")
            .WithOpenApi(operation => new(operation)
            {
                Summary = "Get all maximum mana evaluated hands",
                Description = @"Returns all recorded evaluations for the maximum mana hand dataset.

**Authentication:** Not required (public endpoint)"
            })
            .Produces<IReadOnlyList<MaximumManaEvaluatedHand>>(StatusCodes.Status200OK)
            .AllowAnonymous();
    }

    private static Results<Ok<MaximumManaHand>, NotFound<ProblemDetails>> GenerateHand(
        string decklistName,
        IMaximumManaHandGenerationService handGenerationService,
        IMaximumManaHandEvaluationService handEvaluationService,
        ILogger<Program> logger)
    {
        logger.LogInformation("Generating maximum mana hand for decklist: '{DecklistName}'", decklistName);

        try
        {
            var handId = handEvaluationService.GetNextHandId();
            var hand = handGenerationService.GenerateHand(handId, decklistName);
            logger.LogInformation("Successfully generated maximum mana hand {HandId} for decklist '{DecklistName}'", handId, decklistName);
            return TypedResults.Ok(hand);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning("Decklist not found for maximum mana hand: '{DecklistName}'. Error: {Message}", decklistName, ex.Message);
            return TypedResults.NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Decklist not found",
                Detail = ex.Message
            });
        }
    }

    private static async Task<Ok<object>> EvaluateHand(
        MaximumManaEvaluatedHand evaluation,
        IMaximumManaHandEvaluationService handEvaluationService,
        ClaimsPrincipal user)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        var evaluationWithUser = evaluation with { CreatedByUserId = userId };

        await handEvaluationService.RecordEvaluation(evaluationWithUser);
        return TypedResults.Ok<object>(new { message = "Maximum mana hand evaluation recorded", handId = evaluation.Id });
    }

    private static async Task<Ok<IReadOnlyList<MaximumManaEvaluatedHand>>> GetEvaluatedHands(
        IMaximumManaHandEvaluationService handEvaluationService)
    {
        var evaluations = await handEvaluationService.GetAllEvaluations();
        return TypedResults.Ok(evaluations);
    }
}
