using Etali_Data_Collection_Web_API;
using Etali_Data_Collection_Web_API.Configuration;
using Etali_Data_Collection_Web_API.Models;
using Etali_Data_Collection_Web_API.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

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

// Optional: configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var app = builder.Build();

// Enable CORS
app.UseCors();

// API Endpoints

/// <summary>
/// Generates a new random hand of cards.
/// </summary>
app.MapGet("/hand", (IHandGenerationService handGenerationService, IHandEvaluationService handEvaluationService) =>
{
    var handId = handEvaluationService.GetNextHandId();
    return handGenerationService.GenerateHand(handId);
})
.WithName("GenerateHand");

/// <summary>
/// Records an evaluation of a hand.
/// </summary>
app.MapPost("/hand/evaluate", (EvaluatedHand evaluation, IHandEvaluationService handEvaluationService) =>
{
    handEvaluationService.RecordEvaluation(evaluation);
    
    return Results.Ok(new { message = "Hand evaluation recorded", handId = evaluation.Id });
})
.WithName("EvaluateHand");

/// <summary>
/// Adds a new decklist to the configuration.
/// </summary>
app.MapPost("/decklist", async (string link, string name, IDecklistAddingService decklistAddingService) =>
{
    await decklistAddingService.AddDecklist(link, name);
    return Results.Ok(new { message = "Decklist added successfully" });
})
.WithName("AddDecklist");

/// <summary>
/// Retrieves all evaluated hands.
/// </summary>
app.MapGet("/hands/evaluated", (IHandEvaluationService handEvaluationService) =>
{
    return handEvaluationService.GetAllEvaluations();
})
.WithName("GetEvaluatedHands");

app.Run();