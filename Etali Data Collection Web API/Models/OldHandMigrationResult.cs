namespace Etali_Data_Collection_Web_API.Models;

/// <summary>
/// Result of migrating old hands to evaluated hands.
/// </summary>
public record OldHandMigrationResult
{
    /// <summary>
    /// Number of hands successfully migrated.
    /// </summary>
    public int Migrated { get; set; }
    
    /// <summary>
    /// Number of hands skipped due to containing problematic cards.
    /// </summary>
    public int Skipped { get; set; }
    
    /// <summary>
    /// Number of hands that failed to migrate due to errors.
    /// </summary>
    public int Failed { get; set; }
    
    /// <summary>
    /// Total number of hands processed.
    /// </summary>
    public int Total => Migrated + Skipped + Failed;
}
