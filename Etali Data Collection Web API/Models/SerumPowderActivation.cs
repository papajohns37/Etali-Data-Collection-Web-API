using System.ComponentModel;

namespace Etali_Data_Collection_Web_API.Models;

/// <summary>
/// Represents a Serum Powder activation during a mulligan session.
/// Records which cards were exiled and which were bottomed.
/// </summary>
public class SerumPowderActivation
{
    /// <summary>
    /// Unique identifier for this activation.
    /// </summary>
    [Description("Unique identifier for the Serum Powder activation")]
    public int Id { get; set; }
    
    /// <summary>
    /// The ID of the mulligan session this activation belongs to.
    /// </summary>
    [Description("ID of the parent mulligan session")]
    public int SessionId { get; set; }
    
    /// <summary>
    /// The hand number when Serum Powder was activated (1-6).
    /// </summary>
    [Description("Hand number when activated (1-6)")]
    public int HandNumber { get; set; }
    
    /// <summary>
    /// The target hand size at the time of activation (7, 6, 5, 4, or 3).
    /// </summary>
    [Description("Target hand size at activation")]
    public int TargetHandSize { get; set; }
    
    /// <summary>
    /// Array of cards that were exiled (removed from game for rest of session).
    /// These are the cards that were in hand after bottoming.
    /// </summary>
    [Description("Cards that were exiled")]
    public Card[] CardsExiled { get; set; } = [];
    
    /// <summary>
    /// Array of cards that were put on the bottom of the library before exile.
    /// Count = 7 - TargetHandSize
    /// </summary>
    [Description("Cards that were bottomed")]
    public Card[] CardsBottomed { get; set; } = [];
    
    /// <summary>
    /// The UTC timestamp when Serum Powder was activated.
    /// </summary>
    [Description("UTC timestamp when activated")]
    public DateTime ActivatedAt { get; set; } = DateTime.UtcNow;
}
