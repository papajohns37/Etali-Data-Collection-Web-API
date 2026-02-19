using System.ComponentModel;

namespace Etali_Data_Collection_Web_API.Models;

/// <summary>
/// Represents a complete mulligan session where a user tests the full mulligan process
/// for a decklist to determine if they can find a hand that casts Etali by turn 2.
/// </summary>
/// <remarks>
/// The mulligan process follows the London Mulligan rules:
/// - First hand is a free 7-card mulligan
/// - Subsequent mulligans: 7 → 6 → 5 → 4 → 3
/// - At 3 cards, the process stops (must evaluate if it works)
/// - Full deck is reshuffled between each mulligan
/// </remarks>
public class FullMulliganSession
{
    /// <summary>
    /// Unique identifier for this mulligan session.
    /// </summary>
    [Description("Unique identifier for the mulligan session")]
    public int Id { get; set; }
    
    /// <summary>
    /// The ID of the decklist being tested.
    /// </summary>
    [Description("ID of the decklist being tested")]
    public int DecklistId { get; set; }
    
    /// <summary>
    /// The name of the decklist being tested.
    /// </summary>
    [Description("Name of the decklist being tested")]
    public required string DecklistName { get; set; }
    
    /// <summary>
    /// The player's seat position in the game (1-4).
    /// </summary>
    [Description("Player's seat position (1-4)")]
    public int SeatOrder { get; set; }

    /// <summary>
    /// Array of exactly 3 sticker values applied to Etali.
    /// These stay consistent throughout the mulligan session.
    /// </summary>
    [Description("Array of 3 sticker values applied to Etali")]
    public int[] Stickers { get; set; } = [];

    /// <summary>
    /// Indicates whether the mulligan process resulted in finding a hand
    /// that can cast Etali by turn 2.
    /// </summary>
    [Description("Whether a successful hand was found during the mulligan process")]
    public bool FoundSuccessfulHand { get; set; }
    
    /// <summary>
    /// The size of the final kept hand (3-7 cards).
    /// </summary>
    [Description("Size of the final kept hand (3-7)")]
    public int FinalHandSize { get; set; }
    
    /// <summary>
    /// The total number of times the user mulliganed during this session (0-5).
    /// 0 = kept first 7-card hand
    /// 1 = kept second 7-card hand
    /// 2 = kept 6-card hand
    /// 3 = kept 5-card hand
    /// 4 = kept 4-card hand
    /// 5 = kept 3-card hand
    /// </summary>
    [Description("Total number of mulligans taken (0-5)")]
    public int TotalMulligans { get; set; }

    /// <summary>
    /// The total number of times Serum Powder was activated during this session.
    /// </summary>
    [Description("Total number of Serum Powder activations (0-N)")]
    public int TotalSerumPowderActivations { get; set; }
    
    /// <summary>
    /// Indicates whether this session is still active or has been completed.
    /// </summary>
    [Description("Whether the session is still active")]
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// The ID of the user who created this mulligan session.
    /// </summary>
    [Description("ID of the user who created this session")]
    public required string CreatedByUserId { get; set; }
    
    /// <summary>
    /// The UTC timestamp when this session was created.
    /// </summary>
    [Description("UTC timestamp when the session was created")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// The UTC timestamp when this session was completed (null if still active).
    /// </summary>
    [Description("UTC timestamp when the session was completed")]
    public DateTime? CompletedAt { get; set; }
}
