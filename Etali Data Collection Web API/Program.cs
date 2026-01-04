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

// Configure Moxfield API settings
var moxfieldConfig = builder.Configuration.GetSection("MoxfieldApi").Get<MoxfieldConfiguration>() 
    ?? new MoxfieldConfiguration();
builder.Services.AddSingleton(moxfieldConfig);

// Register rate limiter for Moxfield API (1 request per second)
builder.Services.AddSingleton<IRateLimiter>(new RateLimiter(TimeSpan.FromSeconds(1)));

builder.Services.AddScoped<IHandEvaluationService, HandEvaluationService>();
builder.Services.AddScoped<IHandGenerationService, HandGenerationService>();
builder.Services.AddScoped<IDecklistAddingService, DecklistAddingService>();
builder.Services.AddScoped<IStatisticsService, StatisticsService>();

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
.Produces(StatusCodes.Status404NotFound)
.Produces(StatusCodes.Status400BadRequest);

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
.Produces(StatusCodes.Status400BadRequest);

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
.Produces(StatusCodes.Status400BadRequest)
.Produces(StatusCodes.Status500InternalServerError);

app.MapGet("/decklists", async (EtaliDatabase database) =>
{
    return await database.Decklists.ToListAsync();
})
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

app.MapGet("/hands/evaluated", (IHandEvaluationService handEvaluationService) =>
{
    return handEvaluationService.GetAllEvaluations();
})
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

app.MapGet("/decklists/{decklistId}/statistics", async (int decklistId, IStatisticsService statisticsService) =>
{
    var statistics = await statisticsService.GetDecklistStatistics(decklistId);
    
    if (statistics == null)
    {
        return Results.NotFound(new { error = $"Decklist with ID {decklistId} not found." });
    }
    
    return Results.Ok(statistics);
})
.WithName("GetDecklistStatistics")
.WithTags("Statistics")
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
.Produces(StatusCodes.Status404NotFound);

app.Run();

// Make Program class accessible for testing
public partial class Program { }