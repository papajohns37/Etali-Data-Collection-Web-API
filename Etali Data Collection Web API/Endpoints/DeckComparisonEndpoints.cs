namespace Etali_Data_Collection_Web_API.Endpoints;

using Etali_Data_Collection_Web_API.Models;
using Etali_Data_Collection_Web_API.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

public static class DeckComparisonEndpoints
{
    public static void MapDeckComparisonEndpoints(this WebApplication app)
    {
        var comparisonGroup = app.MapGroup("/deck-comparisons").WithTags("Deck Comparisons");

        // POST /deck-comparisons - Create a new deck comparison (Requires Authentication)
        comparisonGroup.MapPost("", CreateDeckComparison)
            .WithName("CreateDeckComparison")
            .WithOpenApi(operation => new(operation)
            {
                Summary = "Create a new deck comparison",
                Description = @"Creates a comparison between two decklists for statistical analysis.

Request body:
- deck1Id: ID of the first decklist
- deck2Id: ID of the second decklist

Order does not matter - a comparison between deck A and B is the same as B and A.
Duplicate comparisons are not allowed.

Both N values start at 0 and increment as hands are generated.

**Authentication:** Required (Contributor or Admin role)
**Authorization:** Bearer token in Authorization header"
            })
            .Produces<DeckComparison>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .RequireAuthorization("ContributorOrAdmin");

        // GET /deck-comparisons - Get all comparisons (Public)
        comparisonGroup.MapGet("", GetAllComparisons)
            .WithName("GetAllComparisons")
            .WithOpenApi(operation => new(operation)
            {
                Summary = "Get all deck comparisons",
                Description = @"Returns all deck comparisons in the system with their current N values.

**Authentication:** Not required (public endpoint)"
            })
            .Produces<DeckComparison[]>(StatusCodes.Status200OK)
            .AllowAnonymous();

        // GET /deck-comparisons/{id} - Get a specific comparison (Public)
        comparisonGroup.MapGet("{id:int}", GetComparisonById)
            .WithName("GetComparisonById")
            .WithOpenApi(operation => new(operation)
            {
                Summary = "Get a specific deck comparison",
                Description = @"Returns details of a specific deck comparison including N values.

**Authentication:** Not required (public endpoint)"
            })
            .Produces<DeckComparison>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .AllowAnonymous();

        // GET /deck-comparisons/{id}/hand - Generate a hand for comparison (Requires Authentication)
        comparisonGroup.MapGet("{id:int}/hand", GenerateComparisonHand)
            .WithName("GenerateComparisonHand")
            .WithOpenApi(operation => new(operation)
            {
                Summary = "Generate a hand for comparison evaluation",
                Description = @"Generates a hand from the deck with fewer evaluated comparison hands.

The system automatically balances sampling between decks by always generating from
the deck with the lower count of evaluated comparison hands.

The hand will contain at least one card unique to that deck.
Multiple generation attempts may be made to find such a hand.

Returns:
- The hand with 7 cards, stickers, seat order, etc.
- comparisonId: The comparison this hand belongs to
- whichDeck: Which deck (1 or 2) the hand came from
- generationAttempts: Number of hands generated (for N tracking)

The generationAttempts value should be passed back when submitting the evaluation.

**Authentication:** Required (Contributor or Admin role)
**Authorization:** Bearer token in Authorization header"
            })
            .Produces<ComparisonHand>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
            .RequireAuthorization("ContributorOrAdmin");

        // POST /deck-comparisons/{id}/evaluate - Submit evaluation (Requires Authentication)
        comparisonGroup.MapPost("{id:int}/evaluate", SubmitComparisonEvaluation)
            .WithName("SubmitComparisonEvaluation")
            .WithOpenApi(operation => new(operation)
            {
                Summary = "Submit a comparison hand evaluation",
                Description = @"Records the evaluation of a comparison hand and updates the N value.

Request body must include:
- All standard EvaluatedHand fields (from the generated hand)
- whichDeck: Which deck (1 or 2) the hand came from
- generationAttempts: The attempt count from the hand generation

The N value for the appropriate deck will be incremented by generationAttempts.

**Authentication:** Required (Contributor or Admin role)
**Authorization:** Bearer token in Authorization header"
            })
            .Produces<ComparisonEvaluatedHand>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .RequireAuthorization("ContributorOrAdmin");

        // GET /deck-comparisons/{id}/statistics - Get comparison statistics (Public)
        comparisonGroup.MapGet("{id:int}/statistics", GetComparisonStatistics)
            .WithName("GetComparisonStatistics")
            .WithOpenApi(operation => new(operation)
            {
                Summary = "Get statistical comparison between decks",
                Description = @"Returns statistical analysis comparing the two decks.

All differences are calculated as (Deck1 - Deck2):
- Positive values mean Deck1 performs better
- Negative values mean Deck2 performs better

ENHANCED DATA SOURCES:
Statistics combine two data sources for improved precision:
1. Comparison-specific evaluations (hands with unique cards)
2. Filtered main dataset evaluations (existing evaluations containing unique cards)

This approach provides:
- Larger sample sizes for more precise confidence intervals
- Faster convergence to statistically significant results
- Valid inference (both sources are conditionally sampled on unique cards)

SAMPLE SIZES:
Sample sizes used for statistical calculations are based on the count of evaluated hands,
not generation attempts. Only hands containing unique cards provide information about
performance differences, so rejected hands (those without unique cards) are not included
in the sample size for statistical inference.

Includes:
- Per-hand-size probability differences with 90% confidence intervals
- Overall mulligan probability difference
- Sample sizes (count of evaluated hands with unique cards)

Note: Main dataset hands are filtered per-comparison to only include those
with at least one card unique to the respective deck in this comparison.

**Authentication:** Not required (public endpoint)"
            })
            .Produces<DeckComparisonStatistics>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .AllowAnonymous();

        // GET /decklists/{id}/comparisons - Get comparisons for a deck (Public)
        var decklistGroup = app.MapGroup("/decklists/{decklistId:int}").WithTags("Decklists");
        decklistGroup.MapGet("comparisons", GetComparisonsForDeck)
            .WithName("GetComparisonsForDeck")
            .WithOpenApi(operation => new(operation)
            {
                Summary = "Get all comparisons for a decklist",
                Description = @"Returns all comparisons that include the specified decklist (as either deck 1 or deck 2).

**Authentication:** Not required (public endpoint)"
            })
            .Produces<DeckComparison[]>(StatusCodes.Status200OK)
            .AllowAnonymous();

        // GET /decklists/compare?deck1Id={id}&deck2Id={id} - Get card differences between any two decks (Public)
        var decklistsGroup = app.MapGroup("/decklists").WithTags("Decklists");
        decklistsGroup.MapGet("compare", GetCardDifferencesBetweenDecks)
            .WithName("GetCardDifferencesBetweenDecks")
            .WithOpenApi(operation => new(operation)
            {
                Summary = "Get card differences between two decklists",
                Description = @"Returns the card differences between any two decklists without requiring a comparison to exist.

Query parameters:
- deck1Id: ID of the first decklist
- deck2Id: ID of the second decklist

Returns three lists of cards:
- cardsOnlyInDeck1: Cards unique to deck 1
- cardsOnlyInDeck2: Cards unique to deck 2
- sharedCards: Cards in both decks

Card comparison is done by card name. This endpoint is useful for:
- Previewing differences before creating a comparison
- Analyzing any two decks without creating a formal comparison
- Building deck comparison tools

**Authentication:** Not required (public endpoint)"
            })
            .Produces<CardDifferences>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .AllowAnonymous();
    }

    private static async Task<Results<Created<DeckComparison>, NotFound<ProblemDetails>, Conflict<ProblemDetails>, BadRequest<ProblemDetails>>> CreateDeckComparison(
        [FromBody] CreateDeckComparisonRequest request,
        IDeckComparisonService comparisonService)
    {
        try
        {
            var comparison = await comparisonService.AddDeckComparisonAsync(request.Deck1Id, request.Deck2Id);
            return TypedResults.Created($"/deck-comparisons/{comparison.Id}", comparison);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return TypedResults.NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Decklist not found",
                Detail = ex.Message
            });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
        {
            return TypedResults.Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Duplicate comparison",
                Detail = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid request",
                Detail = ex.Message
            });
        }
    }

    private static async Task<Ok<DeckComparison[]>> GetAllComparisons(
        IDeckComparisonService comparisonService)
    {
        var comparisons = await comparisonService.GetAllComparisonsAsync();
        return TypedResults.Ok(comparisons);
    }

    private static async Task<Results<Ok<DeckComparison>, NotFound<ProblemDetails>>> GetComparisonById(
        int id,
        IDeckComparisonService comparisonService)
    {
        var comparison = await comparisonService.GetComparisonByIdAsync(id);
        
        if (comparison == null)
        {
            return TypedResults.NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Comparison not found",
                Detail = $"Deck comparison with ID {id} not found."
            });
        }

        return TypedResults.Ok(comparison);
    }

    private static async Task<Results<Ok<ComparisonHand>, NotFound<ProblemDetails>, ProblemHttpResult>> GenerateComparisonHand(
        int id,
        IDeckComparisonService comparisonService)
    {
        try
        {
            var hand = await comparisonService.GenerateHandForComparisonAsync(id);
            
            if (hand == null)
            {
                return TypedResults.NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Comparison not found",
                    Detail = $"Deck comparison with ID {id} not found."
                });
            }

            return TypedResults.Ok(hand);
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Hand generation failed");
        }
    }

    private static async Task<Results<Ok<ComparisonEvaluatedHand>, NotFound<ProblemDetails>, BadRequest<ProblemDetails>>> SubmitComparisonEvaluation(
        int id,
        [FromBody] SubmitComparisonEvaluationRequest request,
        IDeckComparisonService comparisonService,
        ClaimsPrincipal user)
    {
        try
        {
            // Get user ID from claims
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            
            // Add user ID to the evaluated hand
            var evaluatedHandWithUser = request.EvaluatedHand with { CreatedByUserId = userId };
            
            var result = await comparisonService.SubmitComparisonEvaluationAsync(
                id,
                request.WhichDeck,
                evaluatedHandWithUser,
                request.GenerationAttempts);
            
            if (result == null)
            {
                return TypedResults.NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Comparison not found",
                    Detail = $"Deck comparison with ID {id} not found."
                });
            }

            return TypedResults.Ok(result);
        }
        catch (ArgumentException ex)
        {
            return TypedResults.BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid request",
                Detail = ex.Message
            });
        }
    }

    private static async Task<Results<Ok<DeckComparisonStatistics>, NotFound<ProblemDetails>>> GetComparisonStatistics(
        int id,
        IDeckComparisonService comparisonService)
    {
        var statistics = await comparisonService.GetComparisonStatisticsAsync(id);
        
        if (statistics == null)
        {
            return TypedResults.NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Comparison not found",
                Detail = $"Deck comparison with ID {id} not found or insufficient data."
            });
        }

        return TypedResults.Ok(statistics);
    }

    private static async Task<Ok<DeckComparison[]>> GetComparisonsForDeck(
        int decklistId,
        IDeckComparisonService comparisonService)
    {
        var comparisons = await comparisonService.GetComparisonsForDeckAsync(decklistId);
        return TypedResults.Ok(comparisons);
    }

    private static async Task<Results<Ok<CardDifferences>, NotFound<ProblemDetails>, BadRequest<ProblemDetails>>> GetCardDifferencesBetweenDecks(
        [FromQuery] int deck1Id,
        [FromQuery] int deck2Id,
        IDeckComparisonService comparisonService)
    {
        if (deck1Id == deck2Id)
        {
            return TypedResults.BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid request",
                Detail = "Cannot compare a deck to itself. deck1Id and deck2Id must be different."
            });
        }

        var differences = await comparisonService.GetCardDifferencesByDeckIdsAsync(deck1Id, deck2Id);
        
        if (differences == null)
        {
            return TypedResults.NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Decklist not found",
                Detail = $"One or both decklists not found. Deck1Id: {deck1Id}, Deck2Id: {deck2Id}"
            });
        }

        return TypedResults.Ok(differences);
    }
}

/// <summary>
/// Request model for creating a deck comparison.
/// </summary>
public record CreateDeckComparisonRequest(int Deck1Id, int Deck2Id);

/// <summary>
/// Request model for submitting a comparison evaluation.
/// </summary>
public record SubmitComparisonEvaluationRequest(
    int WhichDeck,
    EvaluatedHand EvaluatedHand,
    int GenerationAttempts);
