using System.ComponentModel;

namespace Etali_Data_Collection_Web_API.Models;

/// <summary>
/// Represents a hand evaluation in the maximum mana dataset workflow.
/// This dataset is independent from the standard evaluated hand dataset.
/// </summary>
public record MaximumManaEvaluatedHand(
    [property: Description("Unique identifier matching the original generated hand")]
    int Id,

    [property: Description("Array of 7 cards in the opening hand")]
    Card[] Cards,

    [property: Description("Player's seat position (1-4)")]
    int SeatOrder,

    [property: Description("Array of 3 sticker values applied to Etali")]
    int[] Stickers,

    [property: Description("Database ID of the source decklist")]
    int DecklistId,

    [property: Description("Name of the source decklist")]
    string DecklistName,

    [property: Description("Lowest Gamble odds value for this hand")]
    float LowestGambleOdds,

    [property: Description("Maximum mana in a 7 card hand")]
    int MaximumManaIn7CardHand,

    [property: Description("Maximum mana in a 6 card hand")]
    int MaximumManaIn6CardHand,

    [property: Description("Maximum mana in a 5 card hand")]
    int MaximumManaIn5CardHand,

    [property: Description("Maximum mana in a 4 card hand")]
    int MaximumManaIn4CardHand,

    [property: Description("Maximum mana in a 3 card hand")]
    int MaximumManaIn3CardHand,

    [property: Description("ID of the user who evaluated this hand")]
    string? CreatedByUserId = null
);
