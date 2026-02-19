using Etali_Data_Collection_Web_API.Models;
using Etali_Data_Collection_Web_API.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Etali_Data_Collection_Web_API.Endpoints;

public static class FullMulliganEndpoints
{
    public static void MapFullMulliganEndpoints(this WebApplication app)
    {
        // POST /mulligan/start - Start a new mulligan session (Requires Authentication)
        app.MapPost("/mulligan/start", StartMulliganSession)
            .WithName("StartMulliganSession")
            .WithTags("Mulligan")
            .WithOpenApi(operation => new(operation)
            {
                Summary = "Start a new mulligan session",
                Description = @"Starts a new full mulligan session to test the complete mulligan process.

**Mulligan Process:**
- You will be shown 7 cards and evaluate if it can cast Etali by turn 2
- If you mulligan, you'll get a new 7 cards to evaluate
- Hand sizes: 7 (free) → 7 → 6 → 5 → 4 → 3
- At 3 cards, you must evaluate but cannot mulligan further
- The deck is fully reshuffled between each mulligan

**Request Body:**
- decklistId: The ID of the decklist to test

**Response:**
- sessionId: Unique identifier for this mulligan session
- currentHand: The first 7-card hand to evaluate
- handNumber: Current hand number (1 for first hand)
- targetHandSize: What size this hand represents (7 for first)
- canMulligan: Whether you can mulligan (true for first hand)

**Authentication:** Required (Contributor or Admin role)
**Authorization:** Bearer token in Authorization header"
            })
            .Produces<FullMulliganStartResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .RequireAuthorization("ContributorOrAdmin");

        // POST /mulligan/evaluate - Evaluate current hand and progress session (Requires Authentication)
        app.MapPost("/mulligan/evaluate", EvaluateMulliganHand)
            .WithName("EvaluateMulliganHand")
            .WithTags("Mulligan")
            .WithOpenApi(operation => new(operation)
            {
                Summary = "Evaluate a mulligan hand",
                Description = @"Evaluates the current hand and progresses the mulligan session.

**Request Body:**
- sessionId: The ID of the mulligan session
- handNumber: Current hand number being evaluated
- canCastEtaliByTurn2: Whether this hand can cast Etali by turn 2
- keepHand: true to keep this hand, false to mulligan

**Response (if session continues):**
- sessionComplete: false
- nextHand: The next 7-card hand to evaluate
- nextHandNumber: Number of the next hand
- nextTargetHandSize: Size this hand represents (7, 6, 5, 4, or 3)
- canMulligan: Whether you can mulligan (false if at 3 cards)
- summary: null

**Response (if session completes):**
- sessionComplete: true
- nextHand: null
- nextHandNumber: null
- nextTargetHandSize: null
- canMulligan: false
- summary: { sessionId, foundSuccessfulHand, finalHandSize, totalMulligans }

**Authentication:** Required (Contributor or Admin role)
**Authorization:** Bearer token in Authorization header"
            })
            .Produces<FullMulliganEvaluationResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .RequireAuthorization("ContributorOrAdmin");

        // GET /mulligan/statistics/{decklistId} - Get statistics for a decklist (Public)
        app.MapGet("/mulligan/statistics/{decklistId}", GetMulliganStatistics)
            .WithName("GetMulliganStatistics")
            .WithTags("Mulligan")
            .WithOpenApi(operation => new(operation)
            {
                Summary = "Get mulligan statistics for a decklist",
                Description = @"Retrieves mulligan session statistics for a specific decklist.

**Returns:**
- Total sessions completed
- Success rate (% of sessions that found a castable hand)
- Average number of mulligans per session
- Breakdown of successful sessions by final hand size
- Distribution of sessions by number of mulligans taken

**Authentication:** Not required (Public endpoint)"
            })
            .Produces<FullMulliganStatistics>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .AllowAnonymous();

        // GET /mulligan/statistics - Get statistics for all decklists (Public)
        app.MapGet("/mulligan/statistics", GetAllMulliganStatistics)
            .WithName("GetAllMulliganStatistics")
            .WithTags("Mulligan")
            .WithOpenApi(operation => new(operation)
            {
                Summary = "Get mulligan statistics for all decklists",
                Description = @"Retrieves mulligan session statistics for all decklists with mulligan data.

Returns an array of statistics objects, ordered by total sessions (descending).

**Authentication:** Not required (Public endpoint)"
            })
            .Produces<List<FullMulliganStatistics>>(StatusCodes.Status200OK)
            .AllowAnonymous();

        // POST /mulligan/statistics/compare - Compare two decklists (Public)
        app.MapPost("/mulligan/statistics/compare", CompareMulliganStatistics)
            .WithName("CompareMulliganStatistics")
            .WithTags("Mulligan")
            .WithOpenApi(operation => new(operation)
            {
                Summary = "Compare mulligan statistics between two decklists",
                Description = @"Compares mulligan success rates between two decklists using a two-proportion z-test.

**Request Body:**
- decklistId1: First decklist ID
- decklistId2: Second decklist ID

**Returns:**
- Statistics for both decklists
- Success rate difference (percentage points)
- Two-proportion z-test results:
  - Z-score
  - P-value (two-tailed)
  - Whether difference is statistically significant (α = 0.05)
  - Human-readable interpretation

**Statistical Test:**
The two-proportion z-test determines if the difference in success rates
between the two decklists is statistically significant or could be due
to random chance.

**Authentication:** Not required (Public endpoint)"
            })
            .Produces<FullMulliganComparisonResult>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .AllowAnonymous();
    }

    private static async Task<Results<Ok<FullMulliganStartResponse>, NotFound<ProblemDetails>, BadRequest<ProblemDetails>>>
        StartMulliganSession(
            [FromBody] FullMulliganStartRequest request,
            IFullMulliganService mulliganService,
            ClaimsPrincipal user)
    {
        try
        {
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return TypedResults.BadRequest(new ProblemDetails
                {
                    Title = "Invalid user",
                    Detail = "User ID could not be determined from authentication token.",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            var response = await mulliganService.StartSessionAsync(request.DecklistId, userId);
            return TypedResults.Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.NotFound(new ProblemDetails
            {
                Title = "Decklist not found or invalid",
                Detail = ex.Message,
                Status = StatusCodes.Status404NotFound
            });
        }
    }

    private static async Task<Results<Ok<FullMulliganEvaluationResponse>, NotFound<ProblemDetails>, BadRequest<ProblemDetails>, UnauthorizedHttpResult>>
        EvaluateMulliganHand(
            [FromBody] FullMulliganEvaluationRequest request,
            IFullMulliganService mulliganService,
            ClaimsPrincipal user)
    {
        try
        {
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return TypedResults.Unauthorized();
            }

            var response = await mulliganService.EvaluateHandAsync(request, userId);
            return TypedResults.Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return TypedResults.BadRequest(new ProblemDetails
            {
                Title = "Unauthorized",
                Detail = ex.Message,
                Status = StatusCodes.Status403Forbidden
            });
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.BadRequest(new ProblemDetails
            {
                Title = "Invalid request",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    private static async Task<Results<Ok<FullMulliganStatistics>, NotFound<ProblemDetails>>>
        GetMulliganStatistics(
            int decklistId,
            IFullMulliganService mulliganService)
    {
        try
        {
            var statistics = await mulliganService.GetStatisticsAsync(decklistId);
            return TypedResults.Ok(statistics);
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.NotFound(new ProblemDetails
            {
                Title = "Decklist not found",
                Detail = ex.Message,
                Status = StatusCodes.Status404NotFound
            });
        }
    }

    private static async Task<Ok<List<FullMulliganStatistics>>>
        GetAllMulliganStatistics(IFullMulliganService mulliganService)
    {
        var statistics = await mulliganService.GetAllStatisticsAsync();
        return TypedResults.Ok(statistics);
    }

    private static async Task<Results<Ok<FullMulliganComparisonResult>, NotFound<ProblemDetails>, BadRequest<ProblemDetails>>>
        CompareMulliganStatistics(
            [FromBody] FullMulliganComparisonRequest request,
            IFullMulliganService mulliganService)
    {
        try
        {
            var result = await mulliganService.CompareDecksAsync(request.DecklistId1, request.DecklistId2);
            return TypedResults.Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            if (ex.Message.Contains("not found"))
            {
                return TypedResults.NotFound(new ProblemDetails
                {
                    Title = "Decklist not found",
                    Detail = ex.Message,
                    Status = StatusCodes.Status404NotFound
                });
            }

            return TypedResults.BadRequest(new ProblemDetails
            {
                Title = "Invalid comparison",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }
}
