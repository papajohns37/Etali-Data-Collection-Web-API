using System.ComponentModel;

namespace Etali_Data_Collection_Web_API.Models
{
    /// <summary>
    /// Represents a Magic: The Gathering decklist stored in the system.
    /// Decklists are used as the source for generating random hands.
    /// </summary>
    /// <example>
    /// {
    ///   "id": 1,
    ///   "name": "Fast Etali",
    ///   "cardNames": ["Sol Ring", "Mana Vault", "Ancient Tomb", "Chrome Mox", ...]
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
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// Array of all card names in the decklist.
        /// Typically contains 99 cards for Commander format.
        /// </summary>
        /// <remarks>
        /// Cards are stored as simple names. The decklist must contain at least 7 cards to generate a hand.
        /// </remarks>
        [Description("Array of all card names in the decklist")]
        public string[] CardNames { get; set; } = Array.Empty<string>();
    }
}
