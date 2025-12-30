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

//Add db context
builder.Services.AddDbContext<EtaliDatabase>(options => options.UseInMemoryDatabase("EtaliDatabase"));

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
/// Retrieves all evaluated hands.
/// </summary>
app.MapGet("/hands/evaluated", (IHandEvaluationService handEvaluationService) =>
{
    return handEvaluationService.GetAllEvaluations();
})
.WithName("GetEvaluatedHands");

app.Run();