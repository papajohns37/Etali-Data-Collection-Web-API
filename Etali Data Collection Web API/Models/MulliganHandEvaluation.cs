using System.ComponentModel;

namespace Etali_Data_Collection_Web_API.Models;

/// <summary>
/// Represents a single hand evaluation during a mulligan session.
/// Each hand shown to the user is recorded with their evaluation.
/// </summary>
public class MulliganHandEvaluation
{
    /// <summary>
    /// Unique identifier for this hand evaluation.
    /// </summary>
    [Description("Unique identifier for the hand evaluation")]
    public int Id { get; set; }
    
    /// <summary>
    /// The ID of the mulligan session this hand belongs to.
    /// </summary>
    [Description("ID of the parent mulligan session")]
    public int SessionId { get; set; }
    
    /// <summary>
    /// The sequential hand number in this session (1-6).
    /// 1 = first 7-card hand
    /// 2 = second 7-card hand (after first mulligan)
    /// 3 = 6-card hand
    /// 4 = 5-card hand
    /// 5 = 4-card hand
    /// 6 = 3-card hand (final)
    /// </summary>
    [Description("Sequential hand number in the session (1-6)")]
    public int HandNumber { get; set; }
    
    /// <summary>
    /// The target hand size this hand represents (7, 6, 5, 4, or 3).
    /// Always shown 7 cards, but this indicates what size hand the player
    /// would keep if they decide to keep this hand.
    /// </summary>
    [Description("Target hand size (7, 6, 5, 4, or 3)")]
    public int TargetHandSize { get; set; }
    
    /// <summary>
    /// Array of exactly 7 cards shown to the user for evaluation.
    /// </summary>
    [Description("Array of 7 cards shown for this hand")]
    public Card[] Cards { get; set; } = [];
    
    /// <summary>
    /// User's evaluation: can this hand cast Etali by turn 2?
    /// </summary>
    [Description("Whether this hand can cast Etali by turn 2")]
    public bool CanCastEtaliByTurn2 { get; set; }
    
    /// <summary>
    /// Whether the user decided to keep this hand.
    /// </summary>
    [Description("Whether the user kept this hand")]
    public bool KeptHand { get; set; }
    
    /// <summary>
    /// The UTC timestamp when this hand was evaluated.
    /// </summary>
    [Description("UTC timestamp when the hand was evaluated")]
    public DateTime EvaluatedAt { get; set; } = DateTime.UtcNow;
}
