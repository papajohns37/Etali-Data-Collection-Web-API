namespace Etali_Data_Collection_Web_API.Endpoints;

using Etali_Data_Collection_Web_API.Models;
using Etali_Data_Collection_Web_API.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

public static class DecklistEndpoints
{
    public static void MapDecklistEndpoints(this WebApplication app)
    {
        // POST /decklist - Add a new decklist (Requires Authentication)
        app.MapPost("/decklist", AddDecklist)
            .WithName("AddDecklist")
            .WithTags("Decklists")
            .WithOpenApi(operation => new(operation)
            {
                Summary = "Import a decklist from Moxfield",
                Description = @"Fetches a decklist from the Moxfield API and stores it in the database.

The service extracts:
- Card names
- Card types (from Moxfield's type line)
- Moxfield metadata (URL and deck ID)

Only mainboard cards are imported. The decklist can then be used to generate random hands.

Rate limited to 1 request per second to respect Moxfield API limits.

Parameters:
- link: Full Moxfield URL (e.g., https://www.moxfield.com/decks/abc123)
- name: Friendly name for the decklist

**Authentication:** Required (Contributor or Admin role)
**Authorization:** Bearer token in Authorization header"
            })
            .Produces(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
            .RequireAuthorization("ContributorOrAdmin");

        // GET /decklists - Get all decklists (Public)
        app.MapGet("/decklists", GetDecklists)
            .WithName("GetDecklists")
            .WithTags("Decklists")
            .WithOpenApi(operation => new(operation)
            {
                Summary = "Get all decklists",
                Description = @"Returns all decklists currently stored in the database.

Each decklist includes:
- id: Unique identifier
- name: Decklist name
- cards: Array of Card objects with name and type
- moxfieldUrl: Source URL (if imported from Moxfield)
- moxfieldId: Moxfield deck ID (if imported)
- createdAt: UTC timestamp when added

Typically contains 99 cards for Commander format.

**Authentication:** Not required (public endpoint)"
            })
            .Produces<List<Decklist>>(StatusCodes.Status200OK)
            .AllowAnonymous();

        // PATCH /decklist/{id}/rename - Rename a decklist (Requires Authorization)
        app.MapPatch("/decklist/{id}/rename", RenameDecklist)
            .WithName("RenameDecklist")
            .WithTags("Decklists")
            .WithOpenApi(operation => new(operation)
            {
                Summary = "Rename a decklist",
                Description = @"Updates the name of an existing decklist.

Authorization rules:
- Admin users can rename any decklist
- Contributor users can only rename decklists they created

Parameters:
- id: The ID of the decklist to rename
- newName: The new name for the decklist

**Authentication:** Required (Contributor or Admin role)
**Authorization:** Bearer token in Authorization header"
            })
            .Produces(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .RequireAuthorization("ContributorOrAdmin");
    }

    private static async Task<Ok<object>> AddDecklist(
        string link,
        string name,
        IDecklistAddingService decklistAddingService,
        ClaimsPrincipal user)
    {
        // Get user ID from claims
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        
        // Add the decklist with user ID
        await decklistAddingService.AddDecklist(link, name, userId);
        
        return TypedResults.Ok<object>(new { message = "Decklist added successfully" });
    }

    private static async Task<Ok<List<Decklist>>> GetDecklists(
        EtaliDatabase database)
    {
        return TypedResults.Ok(await database.Decklists.ToListAsync());
    }

    private static async Task<Results<Ok<object>, BadRequest<ProblemDetails>, NotFound<ProblemDetails>, UnauthorizedHttpResult>> RenameDecklist(
        int id,
        string newName,
        EtaliDatabase database,
        ClaimsPrincipal user,
        ILogger<Program> logger)
    {
        // Validate new name
        if (string.IsNullOrWhiteSpace(newName))
        {
            return TypedResults.BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid name",
                Detail = "Decklist name cannot be empty or whitespace."
            });
        }

        if (newName.Length > 500)
        {
            return TypedResults.BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid name",
                Detail = "Decklist name cannot exceed 500 characters."
            });
        }

        // Find the decklist
        var decklist = await database.Decklists.FirstOrDefaultAsync(d => d.Id == id);
        if (decklist == null)
        {
            return TypedResults.NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Decklist not found",
                Detail = $"Decklist with ID {id} does not exist."
            });
        }

        // Get user information
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        var isAdmin = user.IsInRole("Admin");

        // Check authorization: user must be admin OR the creator of the decklist
        if (!isAdmin && decklist.CreatedByUserId != userId)
        {
            logger.LogWarning(
                "User {UserId} attempted to rename decklist {DecklistId} created by {CreatorId}",
                userId, id, decklist.CreatedByUserId ?? "anonymous");
            
            return TypedResults.Unauthorized();
        }

        // Store old name for logging
        var oldName = decklist.Name;

        // Update the decklist name
        decklist.Name = newName;

        // Update all related data that stores the decklist name
        
        // 1. Update EvaluatedHands that reference this decklist
        var evaluatedHands = await database.EvaluatedHands
            .Where(h => h.DecklistId == id)
            .ToListAsync();
        
        foreach (var hand in evaluatedHands)
        {
            // Create a new record with updated name (records are immutable)
            var updatedHand = hand with { DecklistName = newName };
            database.Entry(hand).CurrentValues.SetValues(updatedHand);
        }

        // 2. Update ComparisonEvaluatedHands that reference this decklist
        var comparisonHands = await database.ComparisonEvaluatedHands
            .Where(h => h.DecklistId == id)
            .ToListAsync();
        
        foreach (var hand in comparisonHands)
        {
            // Create a new record with updated name (records are immutable)
            var updatedHand = hand with { DecklistName = newName };
            database.Entry(hand).CurrentValues.SetValues(updatedHand);
        }

        // 3. Update DeckComparisons where this decklist is Deck1 or Deck2
        var comparisonsAsDeck1 = await database.DeckComparisons
            .Where(dc => dc.Deck1Id == id)
            .ToListAsync();
        
        foreach (var comparison in comparisonsAsDeck1)
        {
            comparison.Deck1Name = newName;
        }

        var comparisonsAsDeck2 = await database.DeckComparisons
            .Where(dc => dc.Deck2Id == id)
            .ToListAsync();
        
        foreach (var comparison in comparisonsAsDeck2)
        {
            comparison.Deck2Name = newName;
        }

        // Save all changes atomically
        await database.SaveChangesAsync();

        logger.LogInformation(
            "Decklist {DecklistId} renamed from '{OldName}' to '{NewName}' by user {UserId} (Admin: {IsAdmin}). " +
            "Updated {EvaluatedHandsCount} evaluated hands, {ComparisonHandsCount} comparison hands, " +
            "{Deck1ComparisonsCount} comparisons as deck1, {Deck2ComparisonsCount} comparisons as deck2",
            id, oldName, newName, userId, isAdmin,
            evaluatedHands.Count, comparisonHands.Count,
            comparisonsAsDeck1.Count, comparisonsAsDeck2.Count);

        return TypedResults.Ok<object>(new
        {
            message = "Decklist renamed successfully",
            id = id,
            oldName = oldName,
            newName = newName,
            updatedRecords = new
            {
                evaluatedHands = evaluatedHands.Count,
                comparisonEvaluatedHands = comparisonHands.Count,
                comparisonsAsDeck1 = comparisonsAsDeck1.Count,
                comparisonsAsDeck2 = comparisonsAsDeck2.Count
            }
        });
    }
}
