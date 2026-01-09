using Etali_Data_Collection_Web_API.Models;
using Etali_Data_Collection_Web_API.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Etali_Data_Collection_Web_API.Endpoints;

/// <summary>
/// Endpoints for migrating old CSV data into the database.
/// </summary>
public static class MigrationEndpoints
{
    public static void MapMigrationEndpoints(this WebApplication app)
    {
        // POST /migration/import-old-hands - Import old hand data from CSV (Admin only)
        app.MapPost("/migration/import-old-hands", ImportOldHands)
            .WithName("ImportOldHandData")
            .WithTags("Migration")
            .WithOpenApi(operation => new(operation)
            {
                Summary = "Import old hand evaluation data from CSV",
                Description = @"Imports historical hand evaluation data from the OldHandData.csv file.

This endpoint:
- Reads the CSV file from the configured location
- Parses each row into an OldEvaluatedHand record
- Attempts to match decklist names with existing decklists in the database
- Stores all records in the old_evaluated_hands table

CSV Format Expected:
Decklist Name,User,Can Cast Etali,Minimum Cards,Card 1,Card 2,Card 3,Card 4,Card 5,Card 6,Card 7

**Authentication:** Required (Admin role only)
**Authorization:** Bearer token in Authorization header"
            })
            .Produces<ImportResult>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .RequireAuthorization("AdminOnly");

        // POST /migration/migrate-old-hands - Migrate old hands to evaluated hands (Admin only)
        app.MapPost("/migration/migrate-old-hands", MigrateOldHands)
            .WithName("MigrateOldHandsToEvaluated")
            .WithTags("Migration")
            .WithOpenApi(operation => new(operation)
            {
                Summary = "Migrate eligible old hands to evaluated hands",
                Description = @"Migrates old hand data to the current evaluated hands format.

This endpoint:
- Processes all records in the old_evaluated_hands table
- Skips hands containing problematic cards (_____ Goblin, Worldly Tutor, Sylvan Tutor, Gamble, Gemstone Caverns)
- For eligible hands:
  - Converts card names to Card objects with types from the decklist
  - Generates random values for seat order, stickers, and gamble odds (not tracked in old data)
  - Sets CreatedByUserId to 'excel_importer'
  - Creates new evaluated hand with sequential ID
  - Removes record from old_evaluated_hands table
- Returns counts of migrated, skipped, and failed records

**Note:** This is a destructive operation that removes migrated records from old_evaluated_hands.
Consider backing up your data before running.

**Authentication:** Required (Admin role only)
**Authorization:** Bearer token in Authorization header"
            })
            .Produces<OldHandMigrationResult>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .RequireAuthorization("AdminOnly");

        // GET /migration/old-hands - Get all imported old hand data (Public)
        app.MapGet("/migration/old-hands", GetOldHands)
            .WithName("GetOldHandData")
            .WithTags("Migration")
            .WithOpenApi(operation => new(operation)
            {
                Summary = "Get all imported old hand evaluation data",
                Description = @"Returns all historical hand evaluation records that have been imported from the CSV file.

Use this endpoint to:
- Verify the imported data
- Analyze historical evaluation patterns
- Compare old data with new evaluations

**Authentication:** Not required (public endpoint)"
            })
            .Produces<IReadOnlyList<OldEvaluatedHand>>(StatusCodes.Status200OK)
            .AllowAnonymous();

        // GET /migration/old-hands/stats - Get statistics about old hand data (Public)
        app.MapGet("/migration/old-hands/stats", GetOldHandStats)
            .WithName("GetOldHandStats")
            .WithTags("Migration")
            .WithOpenApi(operation => new(operation)
            {
                Summary = "Get statistics about imported old hand data",
                Description = @"Returns summary statistics about the historical hand evaluation data.

Statistics include:
- Total number of imported records
- Number of unique decklists
- Number of unique users
- Records matched vs unmatched to current decklists

**Authentication:** Not required (public endpoint)"
            })
            .Produces<OldHandStats>(StatusCodes.Status200OK)
            .AllowAnonymous();
    }

    private static async Task<Results<Ok<ImportResult>, NotFound<ProblemDetails>, ProblemHttpResult>> ImportOldHands(
        string? csvFilePath,
        ICsvMigrationService migrationService,
        ILogger<Program> logger)
    {
        try
        {
            // Default to OldHandData.csv in the application directory if not specified
            csvFilePath ??= Path.Combine(AppContext.BaseDirectory, "OldHandData.csv");
            
            logger.LogInformation("Starting import of old hand data from: {FilePath}", csvFilePath);

            if (!File.Exists(csvFilePath))
            {
                logger.LogWarning("CSV file not found at: {FilePath}", csvFilePath);
                return TypedResults.NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "CSV file not found",
                    Detail = $"The CSV file was not found at: {csvFilePath}"
                });
            }

            var recordsImported = await migrationService.ImportOldHandDataAsync(csvFilePath);

            logger.LogInformation("Successfully imported {Count} records", recordsImported);

            return TypedResults.Ok(new ImportResult
            {
                Success = true,
                RecordsImported = recordsImported,
                Message = $"Successfully imported {recordsImported} old hand evaluation records"
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error importing old hand data");
            return TypedResults.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Import failed",
                detail: ex.Message
            );
        }
    }

    private static async Task<Results<Ok<OldHandMigrationResult>, ProblemHttpResult>> MigrateOldHands(
        IOldHandMigrationService migrationService,
        ILogger<Program> logger)
    {
        try
        {
            logger.LogInformation("Starting migration of old hands to evaluated hands");
            
            var result = await migrationService.MigrateEligibleHandsAsync();
            
            logger.LogInformation(
                "Migration completed: {Migrated} migrated, {Skipped} skipped, {Failed} failed",
                result.Migrated, result.Skipped, result.Failed);

            return TypedResults.Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error migrating old hands");
            return TypedResults.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Migration failed",
                detail: ex.Message
            );
        }
    }

    private static async Task<Ok<IReadOnlyList<OldEvaluatedHand>>> GetOldHands(
        EtaliDatabase db)
    {
        var oldHands = await db.OldEvaluatedHands.ToListAsync();
        return TypedResults.Ok<IReadOnlyList<OldEvaluatedHand>>(oldHands);
    }

    private static async Task<Ok<OldHandStats>> GetOldHandStats(
        EtaliDatabase db)
    {
        var totalRecords = await db.OldEvaluatedHands.CountAsync();
        var uniqueDecklists = await db.OldEvaluatedHands
            .Select(h => h.DecklistName)
            .Distinct()
            .CountAsync();
        var uniqueUsers = await db.OldEvaluatedHands
            .Select(h => h.User)
            .Distinct()
            .CountAsync();
        var matchedToDecklist = await db.OldEvaluatedHands
            .CountAsync(h => h.DecklistId != null);
        var unmatchedToDecklist = totalRecords - matchedToDecklist;

        var stats = new OldHandStats
        {
            TotalRecords = totalRecords,
            UniqueDecklists = uniqueDecklists,
            UniqueUsers = uniqueUsers,
            RecordsMatchedToDecklist = matchedToDecklist,
            RecordsUnmatchedToDecklist = unmatchedToDecklist
        };

        return TypedResults.Ok(stats);
    }
}

/// <summary>
/// Result of importing old hand data.
/// </summary>
public record ImportResult
{
    public bool Success { get; init; }
    public int RecordsImported { get; init; }
    public string Message { get; init; } = string.Empty;
}

/// <summary>
/// Statistics about imported old hand data.
/// </summary>
public record OldHandStats
{
    public int TotalRecords { get; init; }
    public int UniqueDecklists { get; init; }
    public int UniqueUsers { get; init; }
    public int RecordsMatchedToDecklist { get; init; }
    public int RecordsUnmatchedToDecklist { get; init; }
}
