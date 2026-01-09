using System.ComponentModel;

namespace Etali_Data_Collection_Web_API.Models;

/// <summary>
/// Represents historical hand evaluation data imported from the original CSV spreadsheet.
/// This model preserves the original data structure before the current database schema was implemented.
/// </summary>
/// <remarks>
/// This table is used to migrate legacy data from the spreadsheet format:
/// Decklist Name,User,Can Cast Etali,Minimum Cards,Card 1,Card 2,Card 3,Card 4,Card 5,Card 6,Card 7
/// 
/// Note: This historical data does not include:
/// - Stickers (not tracked in original spreadsheet)
/// - Seat order (not tracked in original spreadsheet)
/// - LowestGambleOdds (not tracked in original spreadsheet)
/// - DecklistId (assigned during import based on name matching)
/// - Card types (only card names were recorded)
/// </remarks>
public class OldEvaluatedHand
{
    /// <summary>
    /// Auto-generated unique identifier for this old evaluation record.
    /// </summary>
    [Description("Unique identifier for the old evaluation record")]
    public int Id { get; set; }
    
    /// <summary>
    /// The name of the decklist this hand was evaluated from.
    /// </summary>
    [Description("Name of the decklist")]
    public required string DecklistName { get; set; }
    
    /// <summary>
    /// The database ID of the matching decklist, if found.
    /// Null if no matching decklist exists in the database.
    /// </summary>
    [Description("Foreign key to Decklist, if matched")]
    public int? DecklistId { get; set; }
    
    /// <summary>
    /// The user who evaluated this hand in the original spreadsheet.
    /// </summary>
    [Description("Username of the person who evaluated the hand")]
    public required string User { get; set; }
    
    /// <summary>
    /// Whether Etali could be cast with this hand.
    /// </summary>
    [Description("Whether Etali can be cast with this hand")]
    public bool CanCastEtali { get; set; }
    
    /// <summary>
    /// The minimum number of cards required to cast Etali.
    /// 1-7 if castable, 8 if not castable.
    /// </summary>
    [Description("Minimum number of cards needed to cast Etali (1-7 if possible, 8 if impossible)")]
    public int MinimumCardsToCastEtali { get; set; }
    
    /// <summary>
    /// Name of the first card in the hand.
    /// </summary>
    [Description("Card 1 name")]
    public string? Card1 { get; set; }
    
    /// <summary>
    /// Name of the second card in the hand.
    /// </summary>
    [Description("Card 2 name")]
    public string? Card2 { get; set; }
    
    /// <summary>
    /// Name of the third card in the hand.
    /// </summary>
    [Description("Card 3 name")]
    public string? Card3 { get; set; }
    
    /// <summary>
    /// Name of the fourth card in the hand.
    /// </summary>
    [Description("Card 4 name")]
    public string? Card4 { get; set; }
    
    /// <summary>
    /// Name of the fifth card in the hand.
    /// </summary>
    [Description("Card 5 name")]
    public string? Card5 { get; set; }
    
    /// <summary>
    /// Name of the sixth card in the hand.
    /// </summary>
    [Description("Card 6 name")]
    public string? Card6 { get; set; }
    
    /// <summary>
    /// Name of the seventh card in the hand.
    /// </summary>
    [Description("Card 7 name")]
    public string? Card7 { get; set; }
    
    /// <summary>
    /// Timestamp when this record was imported into the database.
    /// </summary>
    [Description("UTC timestamp when the record was imported")]
    public DateTime ImportedAt { get; set; } = DateTime.UtcNow;
}
