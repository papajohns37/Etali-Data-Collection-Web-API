namespace Etali_Data_Collection_Web_API.Endpoints;

using Etali_Data_Collection_Web_API.Models;
using Etali_Data_Collection_Web_API.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

public static class HandEndpoints
{
    public static void MapHandEndpoints(this WebApplication app)
    {
        // GET /hand - Generate a random hand
        app.MapGet("/hand", GenerateHand)
            .WithName("GenerateHand")
            .WithTags("Hands")
            .WithOpenApi(operation => new(operation)
            {
                Summary = "Generate a random hand",
                Description = @"Generates a new random hand of 7 cards from the specified decklist.

Each card in the hand includes:
- name: The card name
- type: The card type (e.g., Artifact, Creature, Land, Instant, Sorcery)

Additional hand properties:
- Seat order (1-4) representing turn position
- 3 random stickers applied to Etali
- Unique hand ID for later evaluation
- Lowest Gamble odds value for probability calculations

The decklistName parameter is case-insensitive."
            })
            .Produces<Hand>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        // POST /hand/evaluate - Evaluate a hand
        app.MapPost("/hand/evaluate", EvaluateHand)
            .WithName("EvaluateHand")
            .WithTags("Hands")
            .WithOpenApi(operation => new(operation)
            {
                Summary = "Evaluate a hand",
                Description = @"Records the user's evaluation of whether a hand can cast Etali.

Request body must include:
- id: Hand identifier (from GET /hand)
- cards: Array of 7 Card objects with name and type
- seatOrder: Player position (1-4)
- stickers: Array of 3 sticker values
- canCastEtali: true if castable, false otherwise
- minimumCardsToCastEtali: 1-7 if castable, 8 if not
- decklistId: Source decklist ID
- decklistName: Source decklist name
- lowestGambleOdds: Gamble probability value

Evaluation rules:
- If CanCastEtali is true, MinimumCardsToCastEtali should be 1-7
- If CanCastEtali is false, MinimumCardsToCastEtali should be 8"
            })
            .Produces(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        // GET /hands/evaluated - Get all evaluated hands
        app.MapGet("/hands/evaluated", GetEvaluatedHands)
            .WithName("GetEvaluatedHands")
            .WithTags("Hands")
            .WithOpenApi(operation => new(operation)
            {
                Summary = "Get all evaluated hands",
                Description = @"Returns all hand evaluations that have been recorded.

Each evaluation includes:
- All original hand data (cards, stickers, seat order)
- canCastEtali: Whether Etali could be cast
- minimumCardsToCastEtali: Minimum cards needed (1-7 if yes, 8 if no)
- decklistId and decklistName: Source information
- lowestGambleOdds: Gamble probability value

Used for statistical analysis and data export."
            })
            .Produces<IReadOnlyList<EvaluatedHand>>(StatusCodes.Status200OK);
    }

    private static Results<Ok<Hand>, NotFound<ProblemDetails>> GenerateHand(
        string decklistName,
        IHandGenerationService handGenerationService,
        IHandEvaluationService handEvaluationService)
    {
        try
        {
            var handId = handEvaluationService.GetNextHandId();
            var hand = handGenerationService.GenerateHand(handId, decklistName);
            return TypedResults.Ok(hand);
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Decklist not found",
                Detail = ex.Message
            });
        }
    }

    private static async Task<Ok<object>> EvaluateHand(
        EvaluatedHand evaluation,
        IHandEvaluationService handEvaluationService)
    {
        await handEvaluationService.RecordEvaluation(evaluation);
        return TypedResults.Ok<object>(new { message = "Hand evaluation recorded", handId = evaluation.Id });
    }

    private static async Task<Ok<IReadOnlyList<EvaluatedHand>>> GetEvaluatedHands(
        IHandEvaluationService handEvaluationService)
    {
        var evaluations = await handEvaluationService.GetAllEvaluations();
        return TypedResults.Ok(evaluations);
    }
}
