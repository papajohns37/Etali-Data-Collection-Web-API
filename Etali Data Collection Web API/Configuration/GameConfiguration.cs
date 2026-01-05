namespace Etali_Data_Collection_Web_API.Configuration;

/// <summary>
/// Contains game configuration constants including available stickers.
/// </summary>
/// <remarks>
/// This configuration class centralizes game-related constants for easy maintenance.
/// Sticker values are fixed based on game rules and should not change.
/// Sticker distribution: 1×6, 2×5, 6×4, 1×3
/// </remarks>
public class GameConfiguration
{
    /// <summary>
    /// Gets the pool of available sticker values that can be randomly assigned.
    /// </summary>
    /// <remarks>
    /// Sticker distribution: 1×6, 2×5, 6×4, 1×3
    /// </remarks>
    public int[] AvailableStickers { get; } = [6, 5, 5, 4, 4, 4, 4, 4, 4, 3];
}
