namespace Etali.Tests.Fixtures;

/// <summary>
/// Test data fixtures for Moxfield API responses.
/// Contains representative JSON payloads for testing DecklistAddingService.
/// </summary>
public static class MoxfieldTestData
{
    /// <summary>
    /// Simple single-faced card with type_line at card level.
    /// </summary>
    public const string SingleFacedCardJson = """
    {
      "boards": {
        "mainboard": {
          "cards": {
            "card1": {
              "quantity": 1,
              "card": {
                "id": "abc123",
                "name": "Sol Ring",
                "type_line": "Artifact",
                "cmc": 1
              }
            }
          }
        }
      },
      "publicUrl": "https://www.moxfield.com/decks/test123",
      "publicId": "test123"
    }
    """;

    /// <summary>
    /// Double-faced card with type_line in card_faces array.
    /// </summary>
    public const string DoubleFacedCardJson = """
    {
      "boards": {
        "mainboard": {
          "cards": {
            "card1": {
              "quantity": 1,
              "card": {
                "id": "mountain1",
                "name": "Mountain",
                "type": 8,
                "card_faces": [
                  {
                    "id": "face1",
                    "name": "Mountain",
                    "type_line": "Basic Land — Mountain",
                    "oracle_text": "({T}: Add {R}.)"
                  }
                ]
              }
            }
          }
        }
      },
      "publicUrl": "https://www.moxfield.com/decks/test456",
      "publicId": "test456"
    }
    """;

    /// <summary>
    /// Card with multiple quantities (basic lands).
    /// </summary>
    public const string MultipleQuantityJson = """
    {
      "boards": {
        "mainboard": {
          "cards": {
            "card1": {
              "quantity": 15,
              "card": {
                "id": "forest1",
                "name": "Forest",
                "type": 8,
                "card_faces": [
                  {
                    "id": "face1",
                    "name": "Forest",
                    "type_line": "Basic Land — Forest",
                    "oracle_text": "({T}: Add {G}.)"
                  }
                ]
              }
            }
          }
        }
      },
      "publicUrl": "https://www.moxfield.com/decks/test789",
      "publicId": "test789"
    }
    """;

    /// <summary>
    /// Card with Unicode em dash in type_line.
    /// </summary>
    public const string UnicodeTypeLineJson = """
    {
      "boards": {
        "mainboard": {
          "cards": {
            "card1": {
              "quantity": 1,
              "card": {
                "id": "llanowar1",
                "name": "Llanowar Elves",
                "type_line": "Creature \u2014 Elf Druid",
                "cmc": 1
              }
            }
          }
        }
      },
      "publicUrl": "https://www.moxfield.com/decks/unicode123",
      "publicId": "unicode123"
    }
    """;

    /// <summary>
    /// Mix of single-faced and double-faced cards.
    /// </summary>
    public const string MixedCardsJson = """
    {
      "boards": {
        "mainboard": {
          "cards": {
            "card1": {
              "quantity": 1,
              "card": {
                "id": "ring1",
                "name": "Sol Ring",
                "type_line": "Artifact",
                "cmc": 1
              }
            },
            "card2": {
              "quantity": 3,
              "card": {
                "id": "mountain1",
                "name": "Mountain",
                "type": 8,
                "card_faces": [
                  {
                    "id": "face1",
                    "name": "Mountain",
                    "type_line": "Basic Land \u2014 Mountain",
                    "oracle_text": "({T}: Add {R}.)"
                  }
                ]
              }
            },
            "card3": {
              "quantity": 1,
              "card": {
                "id": "tower1",
                "name": "Command Tower",
                "type": 8,
                "card_faces": [
                  {
                    "id": "face1",
                    "name": "Command Tower",
                    "type_line": "Land",
                    "oracle_text": "{T}: Add one mana of any color in your commander's color identity."
                  }
                ]
              }
            }
          }
        }
      },
      "publicUrl": "https://www.moxfield.com/decks/mixed123",
      "publicId": "mixed123"
    }
    """;

    /// <summary>
    /// Empty mainboard (edge case).
    /// </summary>
    public const string EmptyMainboardJson = """
    {
      "boards": {
        "mainboard": {
          "cards": {}
        }
      },
      "publicUrl": "https://www.moxfield.com/decks/empty123",
      "publicId": "empty123"
    }
    """;

    /// <summary>
    /// Missing boards property (malformed JSON).
    /// </summary>
    public const string MissingBoardsJson = """
    {
      "publicUrl": "https://www.moxfield.com/decks/invalid123",
      "publicId": "invalid123"
    }
    """;

    /// <summary>
    /// Card with missing name property.
    /// </summary>
    public const string MissingNameJson = """
    {
      "boards": {
        "mainboard": {
          "cards": {
            "card1": {
              "quantity": 1,
              "card": {
                "id": "noname1",
                "type_line": "Creature"
              }
            }
          }
        }
      },
      "publicUrl": "https://www.moxfield.com/decks/noname123",
      "publicId": "noname123"
    }
    """;

    /// <summary>
    /// Card with no type_line or card_faces (fallback to type as number).
    /// </summary>
    public const string NoTypeLineJson = """
    {
      "boards": {
        "mainboard": {
          "cards": {
            "card1": {
              "quantity": 1,
              "card": {
                "id": "notype1",
                "name": "Mystery Card",
                "type": 5
              }
            }
          }
        }
      },
      "publicUrl": "https://www.moxfield.com/decks/notype123",
      "publicId": "notype123"
    }
    """;
}
