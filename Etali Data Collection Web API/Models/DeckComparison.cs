namespace Etali_Data_Collection_Web_API.Models;

using System.ComponentModel;

/// <summary>
/// Represents a comparison between two decklists for statistical analysis.
/// Tracks the number of hands generated (N) for each deck during the comparison process.
/// </summary>
/// <example>
/// {
///   "id": 1,
///   "deck1Id": 5,
///   "deck1Name": "Fast Etali",
///   "deck2Id": 8,
///   "deck2Name": "Budget Etali",
///   "deck1N": 42,
///   "deck2N": 38,
///   "createdAt": "2024-01-15T10:30:00Z"
/// }
/// </example>
public class DeckComparison
{
    /// <summary>
    /// Unique identifier for this deck comparison.
    /// </summary>
    [Description("Unique identifier for the deck comparison")]
    public int Id { get; set; }
    
    /// <summary>
    /// The ID of the first decklist in the comparison.
    /// </summary>
    [Description("Database ID of the first decklist")]
    public int Deck1Id { get; set; }
    
    /// <summary>
    /// The name of the first decklist in the comparison.
    /// </summary>
    [Description("Name of the first decklist")]
    public required string Deck1Name { get; set; } = string.Empty;
    
    /// <summary>
    /// The ID of the second decklist in the comparison.
    /// </summary>
    [Description("Database ID of the second decklist")]
    public int Deck2Id { get; set; }
    
    /// <summary>
    /// The name of the second decklist in the comparison.
    /// </summary>
    [Description("Name of the second decklist")]
    public required string Deck2Name { get; set; } = string.Empty;
    
    /// <summary>
    /// The number of hands generated for deck 1 during this comparison.
    /// This includes both evaluated hands and rejected hands (those without unique cards).
    /// </summary>
    [Description("Number of hands generated for deck 1 (includes rejected hands)")]
    public int Deck1N { get; set; } = 0;
    
    /// <summary>
    /// The number of hands generated for deck 2 during this comparison.
    /// This includes both evaluated hands and rejected hands (those without unique cards).
    /// </summary>
    [Description("Number of hands generated for deck 2 (includes rejected hands)")]
    public int Deck2N { get; set; } = 0;
    
    /// <summary>
    /// The UTC timestamp when this comparison was created.
    /// </summary>
    [Description("UTC timestamp when the comparison was created")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Checks if this comparison is equivalent to another, treating order as insignificant.
    /// (Deck1=A, Deck2=B) is considered equal to (Deck1=B, Deck2=A).
    /// </summary>
    /// <param name="deck1Id">First deck ID to compare.</param>
    /// <param name="deck2Id">Second deck ID to compare.</param>
    /// <returns>True if this comparison matches the given deck IDs in either order.</returns>
    public bool IsSameComparison(int deck1Id, int deck2Id)
    {
        return (Deck1Id == deck1Id && Deck2Id == deck2Id) ||
               (Deck1Id == deck2Id && Deck2Id == deck1Id);
    }
}
