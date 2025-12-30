namespace Etali_Data_Collection_Web_API.Configuration;

/// <summary>
/// Contains game configuration constants including the decklist and available stickers.
/// </summary>
/// <remarks>
/// This configuration class centralizes all game-related constants for easy maintenance.
/// In a production environment, these values could be loaded from appsettings.json or a database.
/// </remarks>
public class GameConfiguration
{
    /// <summary>
    /// Gets the complete decklist of cards available for hand generation.
    /// </summary>
    public string[] Decklist { get; } = 
    [
        "_____ Goblin",
        "Ancient Tomb",
        "Arid Mesa",
        "Autumn's Veil",
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
        "Jeska's Will",
        "Karplusan Forest",
        "Lion's Eye Diamond",
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
        "Nature's Claim",
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

    /// <summary>
    /// Gets the pool of available sticker values that can be randomly assigned.
    /// </summary>
    public int[] AvailableStickers { get; } = [6, 5, 5, 4, 4, 4, 4, 4, 4, 3];
}
