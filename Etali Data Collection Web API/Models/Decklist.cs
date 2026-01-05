namespace Etali_Data_Collection_Web_API.Models;

using System.ComponentModel;

/// <summary>
/// Represents a Magic: The Gathering decklist stored in the system.
/// Decklists are used as the source for generating random hands.
/// </summary>
/// <example>
/// {
///   "id": 1,
///   "name": "Fast Etali",
///   "cards": [
///     { "name": "Sol Ring", "type": "Artifact" },
///     { "name": "Mana Vault", "type": "Artifact" },
///     { "name": "Ancient Tomb", "type": "Land" }
///   ],
///   "moxfieldUrl": "https://www.moxfield.com/decks/abc123xyz",
///   "moxfieldId": "abc123xyz",
///   "createdAt": "2024-01-15T10:30:00Z"
/// }
/// </example>
public class Decklist
{
    /// <summary>
    /// Unique identifier for this decklist.
    /// </summary>
    [Description("Unique identifier for the decklist")]
    public int Id { get; set; }
    
    /// <summary>
    /// Human-readable name for the decklist.
    /// </summary>
    [Description("Name of the decklist")]
    public required string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Array of all cards in the decklist with their properties.
    /// Typically contains 99 cards for Commander format.
    /// </summary>
    /// <remarks>
    /// The decklist must contain at least 7 cards to generate a hand.
    /// </remarks>
    [Description("Array of all cards in the decklist")]
    public Card[] Cards { get; set; } = [];
    
    /// <summary>
    /// The public URL to view this decklist on Moxfield.
    /// </summary>
    /// <example>https://www.moxfield.com/decks/abc123xyz</example>
    [Description("Public URL to view the decklist on Moxfield")]
    public string? MoxfieldUrl { get; set; }
    
    /// <summary>
    /// The unique Moxfield deck identifier.
    /// </summary>
    /// <example>abc123xyz</example>
    [Description("Moxfield deck ID")]
    public string? MoxfieldId { get; set; }
    
    /// <summary>
    /// The UTC timestamp when this decklist was added to the system.
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    [Description("UTC timestamp when the decklist was created")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
