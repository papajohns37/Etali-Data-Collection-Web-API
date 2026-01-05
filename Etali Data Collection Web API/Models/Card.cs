namespace Etali_Data_Collection_Web_API.Models;

using System.ComponentModel;

/// <summary>
/// Represents a Magic: The Gathering card with its basic properties.
/// </summary>
/// <example>
/// {
///   "name": "Sol Ring",
///   "type": "Artifact"
/// }
/// </example>
public record Card
{
    /// <summary>
    /// The name of the card.
    /// </summary>
    /// <example>Sol Ring</example>
    [Description("Name of the card")]
    public required string Name { get; init; } = string.Empty;
    
    /// <summary>
    /// The type line of the card (e.g., "Artifact", "Creature — Human Wizard", "Instant").
    /// </summary>
    /// <example>Artifact</example>
    [Description("Type line of the card")]
    public required string Type { get; init; } = string.Empty;
}
