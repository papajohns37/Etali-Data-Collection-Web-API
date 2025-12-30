namespace Etali_Data_Collection_Web_API.Models;

/// <summary>
/// Represents a hand that has been evaluated for its ability to cast Etali.
/// </summary>
/// <param name="Id">Unique identifier matching the original hand.</param>
/// <param name="Cards">Array of card names in the evaluated hand.</param>
/// <param name="SeatOrder">The player's seat position in the game (1-4).</param>
/// <param name="Stickers">Array of sticker values applied to the hand.</param>
/// <param name="CanCastEtali">Indicates whether Etali can be cast with this hand.</param>
/// <param name="MinimumCardsToCastEtali">The minimum number of cards required to cast Etali.</param>
/// <param name="DecklistId">The ID of the decklist this hand was generated from.</param>
/// <param name="DecklistName">The name of the decklist this hand was generated from.</param>
public record EvaluatedHand(
    int Id,
    string[] Cards,
    int SeatOrder,
    int[] Stickers,
    bool CanCastEtali,
    int MinimumCardsToCastEtali,
    int DecklistId,
    string DecklistName
);
