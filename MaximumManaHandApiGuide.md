# Maximum Mana Hand API Guide

This guide outlines the new "Maximum Mana Hand" API endpoints that have been added for the new data collection workflow. This dataset operates independently of the existing evaluated hand dataset.

## 1. Generate a Maximum Mana Hand

**Endpoint:** `GET /maximum-mana-hand`

**Description:**
Generates a random 7-card opening hand from a specified decklist. This is the first step in the workflow to give the user a hand to evaluate.

**Query Parameters:**
- `decklistName` (string, required): The name of the decklist to generate the hand from (case-insensitive).

**Headers:**
- `Authorization`: `Bearer <jwt_token>` (Required: User must have `Contributor` or `Admin` role)

**Example Request:**
```http
GET /maximum-mana-hand?decklistName=Fast%20Etali
Authorization: Bearer eyJhb...
```

**Example Response (200 OK):**
```json
{
  "id": 1234,
  "cards": [
    { "name": "Sol Ring", "type": "Artifact" },
    { "name": "Mana Vault", "type": "Artifact" },
    { "name": "Ancient Tomb", "type": "Land" },
    ... 4 more cards ...
  ],
  "seatOrder": 2,
  "stickers": [6, 5, 4],
  "decklistId": 1,
  "decklistName": "Fast Etali",
  "lowestGambleOdds": 0.75
}
```

## 2. Evaluate a Maximum Mana Hand

**Endpoint:** `POST /maximum-mana-hand/evaluate`

**Description:**
Records the user's evaluation of the generated hand. This endpoint includes hand metadata plus the 5 maximum mana columns for hand sizes 7 through 3.

**Headers:**
- `Authorization`: `Bearer <jwt_token>` (Required: User must have `Contributor` or `Admin` role)
- `Content-Type`: `application/json`

**Request Body Schema:**
Include all fields from the hand generation response and the 5 integer fields for maximum mana possible at varying hand sizes.

**Example Request:**
```http
POST /maximum-mana-hand/evaluate
Content-Type: application/json
Authorization: Bearer eyJhb...

{
  "id": 1234,
  "cards": [
    { "name": "Sol Ring", "type": "Artifact" },
    { "name": "Mana Vault", "type": "Artifact" },
    { "name": "Ancient Tomb", "type": "Land" },
    { "name": "Mountain", "type": "Land" },
    { "name": "Simian Spirit Guide", "type": "Artifact" },
    { "name": "Dark Ritual", "type": "Instant" },
    { "name": "Crimson Kobolds", "type": "Creature" }
  ],
  "seatOrder": 2,
  "stickers": [6, 5, 4],
  "decklistId": 1,
  "decklistName": "Fast Etali",
  "lowestGambleOdds": 0.75,
  "maximumManaIn7CardHand": 9,
  "maximumManaIn6CardHand": 8,
  "maximumManaIn5CardHand": 7,
  "maximumManaIn4CardHand": 5,
  "maximumManaIn3CardHand": 4
}
```

## 3. Retrieve All Maximum Mana Evaluated Hands

**Endpoint:** `GET /maximum-mana-hands/evaluated`

**Description:**
Fetches all recorded evaluations for the maximum mana dataset. Useful for data export or statistical analysis.

**Headers:**
- *None* (This is a public endpoint).

**Example Request:**
```http
GET /maximum-mana-hands/evaluated
```

**Example Response (200 OK):**
```json
[
  {
    "id": 1234,
    "cards": [ ... ],
    "seatOrder": 2,
    "stickers": [6, 5, 4],
    ...
    "maximumManaIn7CardHand": 9,
    "maximumManaIn6CardHand": 8,
    "maximumManaIn5CardHand": 7,
    "maximumManaIn4CardHand": 5,
    "maximumManaIn3CardHand": 4,
    "createdByUserId": "user-guid-here"
  }
]
```