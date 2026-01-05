namespace Etali_Data_Collection_Web_API.Endpoints;

using Etali_Data_Collection_Web_API.Models;
using Etali_Data_Collection_Web_API.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

public static class DecklistEndpoints
{
    public static void MapDecklistEndpoints(this WebApplication app)
    {
        // POST /decklist - Add a new decklist
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
- name: Friendly name for the decklist"
            })
            .Produces(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        // GET /decklists - Get all decklists
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

Typically contains 99 cards for Commander format."
            })
            .Produces<List<Decklist>>(StatusCodes.Status200OK);
    }

    private static async Task<Ok<object>> AddDecklist(
        string link,
        string name,
        IDecklistAddingService decklistAddingService)
    {
        await decklistAddingService.AddDecklist(link, name);
        return TypedResults.Ok<object>(new { message = "Decklist added successfully" });
    }

    private static async Task<Ok<List<Decklist>>> GetDecklists(
        EtaliDatabase database)
    {
        return TypedResults.Ok(await database.Decklists.ToListAsync());
    }
}
