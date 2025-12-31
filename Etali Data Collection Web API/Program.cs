using System.Reflection;
using Etali_Data_Collection_Web_API;
using Etali_Data_Collection_Web_API.Configuration;
using Etali_Data_Collection_Web_API.Models;
using Etali_Data_Collection_Web_API.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Configure URLs explicitly
builder.WebHost.UseUrls("https://localhost:7194", "http://localhost:5135");

// Add CORS services
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Register application services
builder.Services.AddSingleton<GameConfiguration>();
builder.Services.AddScoped<IHandEvaluationService, HandEvaluationService>();
builder.Services.AddScoped<IHandGenerationService, HandGenerationService>();
builder.Services.AddScoped<IDecklistAddingService, DecklistAddingService>();

builder.Services.AddHttpClient("MoxfieldClient", client =>
{
    client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
    client.DefaultRequestHeaders.Add("Accept", "application/json, text/plain, */*");
    client.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9");
    client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    AutomaticDecompression = System.Net.DecompressionMethods.All,
    UseCookies = false
});

//Add db context
builder.Services.AddDbContext<EtaliDatabase>(options => options.UseInMemoryDatabase("EtaliDatabase"));

// Add Swagger services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Etali Data Collection API",
        Version = "v1",
        Description = @"API for generating and evaluating Magic: The Gathering opening hands with Etali, Primal Conqueror.

This API helps collect data on how quickly Etali can be cast in Commander games by:
1. Generating random opening hands from uploaded decklists
2. Recording user evaluations of those hands
3. Tracking statistics on minimum cards needed to cast Etali

Key Features:
- Generate random 7-card opening hands with stickers
- Evaluate hands for Etali castability
- Import decklists from Moxfield
- Track evaluation statistics",
        Contact = new OpenApiContact
        {
            Name = "API Support",
            Url = new Uri("https://github.com/papajohns37/Etali-Data-Collection-Web-API")
        }
    });

    // Include XML comments
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }

    // Configure schema for records
    options.UseAllOfToExtendReferenceSchemas();
    
    // Add examples and annotations
    options.EnableAnnotations();
});

// Optional: configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var app = builder.Build();

// Enable CORS
app.UseCors();

// Enable Swagger middleware
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Etali API v1");
    options.RoutePrefix = string.Empty; // Serve Swagger UI at root
    options.DocumentTitle = "Etali Data Collection API";
    options.EnableDeepLinking();
    options.DisplayRequestDuration();
    options.EnableTryItOutByDefault();
});

// API Endpoints

/// <summary>
/// Generates a new random hand of cards from a specific decklist.
/// </summary>
/// <param name="decklistName">The name of the decklist to generate a hand from (case-insensitive).</param>
/// <returns>A randomly generated hand with 7 cards, seat order, and stickers.</returns>
/// <response code="200">Returns the generated hand with all metadata.</response>
/// <response code="404">The specified decklist was not found.</response>
/// <response code="400">The decklistName parameter was not provided.</response>
app.MapGet("/hand", (string decklistName, IHandGenerationService handGenerationService, IHandEvaluationService handEvaluationService) =>
{
    try
    {
        var handId = handEvaluationService.GetNextHandId();
        return Results.Ok(handGenerationService.GenerateHand(handId, decklistName));
    }
    catch (InvalidOperationException ex)
    {
        return Results.NotFound(new { error = ex.Message });
    }
})
.WithName("GenerateHand")
.WithTags("Hands")
.WithOpenApi(operation => new(operation)
{
    Summary = "Generate a random hand",
    Description = "Generates a new random hand of 7 cards from the specified decklist, including seat order (1-4) and 3 random stickers. The hand is assigned a unique ID for later evaluation."
})
.Produces<Hand>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound)
.Produces(StatusCodes.Status400BadRequest);

/// <summary>
/// Records an evaluation of a hand for its ability to cast Etali.
/// </summary>
/// <param name="evaluation">The evaluated hand data including whether Etali can be cast and minimum cards needed.</param>
/// <returns>Confirmation of the recorded evaluation.</returns>
/// <response code="200">The hand evaluation was successfully recorded.</response>
/// <response code="400">The evaluation data was invalid or incomplete.</response>
app.MapPost("/hand/evaluate", (EvaluatedHand evaluation, IHandEvaluationService handEvaluationService) =>
{
    handEvaluationService.RecordEvaluation(evaluation);
    
    return Results.Ok(new { message = "Hand evaluation recorded", handId = evaluation.Id });
})
.WithName("EvaluateHand")
.WithTags("Hands")
.WithOpenApi(operation => new(operation)
{
    Summary = "Evaluate a hand",
    Description = "Records the user's evaluation of whether a hand can cast Etali and the minimum number of cards required. If CanCastEtali is false, MinimumCardsToCastEtali should be 8."
})
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status400BadRequest);

/// <summary>
/// Adds a new decklist from a Moxfield URL.
/// </summary>
/// <param name="link">The Moxfield URL for the decklist (e.g., https://www.moxfield.com/decks/...).</param>
/// <param name="name">A friendly name to assign to this decklist for easy reference.</param>
/// <returns>Confirmation that the decklist was added.</returns>
/// <response code="200">The decklist was successfully imported and saved.</response>
/// <response code="400">Required parameters were missing or the URL was invalid.</response>
/// <response code="500">An error occurred while fetching or parsing the decklist.</response>
app.MapPost("/decklist", async (string link, string name, IDecklistAddingService decklistAddingService) =>
{
    await decklistAddingService.AddDecklist(link, name);
    return Results.Ok(new { message = "Decklist added successfully" });
})
.WithName("AddDecklist")
.WithTags("Decklists")
.WithOpenApi(operation => new(operation)
{
    Summary = "Import a decklist from Moxfield",
    Description = "Fetches a decklist from Moxfield using PuppeteerSharp and stores it in the database. The decklist can then be used to generate random hands."
})
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status400BadRequest)
.Produces(StatusCodes.Status500InternalServerError);

/// <summary>
/// Retrieves all stored decklists.
/// </summary>
/// <returns>A list of all decklists in the database.</returns>
/// <response code="200">Returns the list of all decklists with their names and card counts.</response>
app.MapGet("/decklists", async (EtaliDatabase database) =>
{
    return await database.Decklists.ToListAsync();
})
.WithName("GetDecklists")
.WithTags("Decklists")
.WithOpenApi(operation => new(operation)
{
    Summary = "Get all decklists",
    Description = "Returns all decklists currently stored in the database, including their IDs, names, and card lists."
})
.Produces<List<Decklist>>(StatusCodes.Status200OK);

/// <summary>
/// Retrieves all evaluated hands from the database.
/// </summary>
/// <returns>A list of all hand evaluations that have been recorded.</returns>
/// <response code="200">Returns the complete collection of all evaluated hands.</response>
app.MapGet("/hands/evaluated", (IHandEvaluationService handEvaluationService) =>
{
    return handEvaluationService.GetAllEvaluations();
})
.WithName("GetEvaluatedHands")
.WithTags("Hands")
.WithOpenApi(operation => new(operation)
{
    Summary = "Get all evaluated hands",
    Description = "Returns all hand evaluations that have been recorded, including whether Etali could be cast and the minimum cards needed."
})
.Produces<IReadOnlyList<EvaluatedHand>>(StatusCodes.Status200OK);

app.Run();

// Make Program class accessible for testing
public partial class Program { }