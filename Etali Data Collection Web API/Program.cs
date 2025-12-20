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

var app = builder.Build();

// Enable CORS
app.UseCors();

int currentHandId = 0;
List<EvaluatedHand> evaluatedHands = new();

string[] decklist = [
    "_____ Goblin",
    "Ancient Tomb",
    "Arid Mesa",
    "Autumn\'s Veil",
    "Birds of Paradise",
    "Birgi, God of Storytelling // Harnfel, Horn of Bounty",
    "Bloodstained Mire",
    "Boseiju, Who Endures",
    "Chandra, Flameshaper",
    "Chrome Mox",
    "City of Brass",
    "City of Traitors",
    "Command Tower",
    "Copperline Gorge",
    "Cragcrown Pathway // Timbercrown Pathway",
    "Crop Rotation",
    "Crystal Vein",
    "Cursed Mirror",
    "Deflecting Swat",
    "Desperate Ritual",
    "Devastating Onslaught",
    "Dualcaster Mage",
    "Dwarven Ruins",
    "Eldrazi Confluence",
    "Eldritch Evolution",
    "Electroduplicate",
    "Elvish Spirit Guide",
    "Emerald Charm",
    "Emrakul, the Promised End",
    "Eternal Witness",
    "Evendo Brushrazer",
    "Flare of Duplication",
    "Food Chain",
    "Forbidden Orchard",
    "Fury",
    "Gamble",
    "Gemstone Caverns",
    "Geosurge",
    "Great Furnace",
    "Grim Monolith",
    "Grove of the Burnwillows",
    "Guttural Response",
    "Heat Shimmer",
    "Hellkite Courser",
    "Hunting Velociraptor",
    "Imperial Recruiter",
    "Infernal Plunge",
    "Jaxis, the Troublemaker",
    "Jeska\'s Will",
    "Karplusan Forest",
    "Lion\'s Eye Diamond",
    "Lotus Petal",
    "Mana Confluence",
    "Mana Vault",
    "Metamorphosis",
    "Misty Rainforest",
    "Molten Duplication",
    "Mountain",
    "Mox Diamond",
    "Multiversal Passage",
    "Natural State",
    "Nature\'s Claim",
    "Noxious Revival",
    "Open the Omenpaths",
    "Orcish Lumberjack",
    "Pick Your Poison",
    "Prismatic Vista",
    "Pyretic Ritual",
    "Pyroblast",
    "Pyrokinesis",
    "Ragavan, Nimble Pilferer",
    "Red Elemental Blast",
    "Rionya, Fire Dancer",
    "Rite of Flame",
    "Sandstone Needle",
    "Scalding Tarn",
    "Seething Song",
    "Simian Spirit Guide",
    "Snow-Covered Forest",
    "Snow-Covered Mountain",
    "Sol Ring",
    "Spire Garden",
    "Squee, the Immortal",
    "Starting Town",
    "Stomping Ground",
    "Strike It Rich",
    "Sylvan Tutor",
    "Taiga",
    "Thornspire Verge",
    "Tinder Wall",
    "Treasonous Ogre",
    "Twinflame",
    "Underworld Breach",
    "Veil of Summer",
    "Verdant Catacombs",
    "Wheel of Fortune",
    "Windswept Heath",
    "Wooded Foothills",
    "Worldly Tutor"
  ];

app.MapGet("/hand", () =>
{
    currentHandId++;
    return generateHand(currentHandId, decklist);
});

app.MapPost("/hand/evaluate", (EvaluatedHand evaluation) =>
{
    evaluatedHands.Add(evaluation);
    
    return Results.Ok(new { message = "Hand evaluation recorded", handId = evaluation.Id });
});

app.MapGet("/hands/evaluated", () => evaluatedHands);

app.Run();

static Hand generateHand(int id, string[] decklist)
{
    var random = new Random();
    var selectedCards = decklist
        .OrderBy(_ => random.Next())
        .Take(7)
        .ToArray();
    
    var seatOrder = random.Next(1, 5);
    var stickers = generateStickers();
    
    return new Hand(id, selectedCards, seatOrder, stickers);
}

static int[] generateStickers()
{
    int[] availableStickers = [6, 5, 5, 4, 4, 4, 4, 4, 4, 3];
    var random = new Random();
    
    var selectedStickers = availableStickers
        .OrderBy(_ => random.Next())
        .Take(3)
        .ToArray();
    
    return selectedStickers;
}

public record Hand(int Id, string[] Cards, int SeatOrder, int[] Stickers);

public record EvaluatedHand(
    int Id,
    string[] Cards,
    int SeatOrder,
    int[] Stickers,
    bool CanCastEtali,
    int MinimumCardsToCastEtali
);