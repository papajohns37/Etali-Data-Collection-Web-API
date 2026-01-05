using System.ComponentModel;

namespace Etali_Data_Collection_Web_API.Models;

/// <summary>
/// Represents the differences in cards between two decklists in a comparison.
/// </summary>
/// <param name="CardsOnlyInDeck1">Cards that appear in deck 1 but not in deck 2.</param>
/// <param name="CardsOnlyInDeck2">Cards that appear in deck 2 but not in deck 1.</param>
/// <param name="SharedCards">Cards that appear in both deck 1 and deck 2.</param>
/// <example>
/// {
///   "cardsOnlyInDeck1": [
///     { "name": "Mana Vault", "type": "Artifact" },
///     { "name": "Grim Monolith", "type": "Artifact" }
///   ],
///   "cardsOnlyInDeck2": [
///     { "name": "Worn Powerstone", "type": "Artifact" },
///     { "name": "Thran Dynamo", "type": "Artifact" }
///   ],
///   "sharedCards": [
///     { "name": "Sol Ring", "type": "Artifact" },
///     { "name": "Command Tower", "type": "Land" }
///   ]
/// }
/// </example>
public record CardDifferences(
    [property: Description("Cards that appear only in deck 1")]
    Card[] CardsOnlyInDeck1,
    
    [property: Description("Cards that appear only in deck 2")]
    Card[] CardsOnlyInDeck2,
    
    [property: Description("Cards that appear in both decks")]
    Card[] SharedCards
);
