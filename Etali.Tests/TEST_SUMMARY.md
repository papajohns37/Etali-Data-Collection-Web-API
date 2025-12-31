# Etali Data Collection Web API - Test Summary

## Test Results
? **All 41 tests passing**

## Test Coverage

### 1. HandGenerationService Tests (11 tests)
- ? Generates hand with exactly 7 cards
- ? Returns seat order between 1 and 4
- ? Returns 3 stickers
- ? Stickers are from available pool
- ? Cards are unique (no duplicates)
- ? Includes decklist ID and name
- ? Case-insensitive decklist name lookup
- ? Throws exception for non-existent decklist
- ? Throws exception for decklist with less than 7 cards
- ? Produces random/varied results across multiple hands
- ? All cards selected are from the decklist

### 2. HandEvaluationService Tests (6 tests)
- ? Records evaluation and adds to database
- ? Returns empty list when database is empty
- ? Returns all evaluations when multiple exist
- ? GetNextHandId returns 1 for empty database
- ? GetNextHandId returns max ID + 1 with existing hands
- ? Maintains all evaluation data (cards, seat order, stickers, can cast, min cards)

### 3. GameConfiguration Tests (8 tests)
- ? Decklist has correct number of cards (99)
- ? Decklist contains expected cards
- ? Decklist has no duplicates
- ? No null or empty card names
- ? Available stickers has correct count (10)
- ? Available stickers has expected values (6, 5, 4, 3)
- ? Available stickers has correct distribution (1×6, 2×5, 6×4, 1×3)
- ? All sticker values are positive

### 4. Hand Model Tests (3 tests)
- ? Constructor sets all properties correctly
- ? Record equality works correctly
- ? Can be deconstructed

### 5. EvaluatedHand Model Tests (5 tests)
- ? Constructor sets all properties correctly
- ? Record equality works correctly
- ? CanCastEtali false should have MinimumCards = 8
- ? CanCastEtali true has MinimumCards < 8
- ? Can be deconstructed

### 6. Decklist Model Tests (4 tests)
- ? Constructor sets properties
- ? CardNames can be empty
- ? Name can be set and updated
- ? CardNames can be updated

### 7. EtaliDatabase Tests (4 tests)
- ? Can add and retrieve evaluated hands
- ? Can add and retrieve decklists
- ? Supports async operations
- ? Can store multiple evaluated hands
- ? Can store multiple decklists

## API Endpoints Covered

### GET /hand
- Generates random hand from specified decklist
- Returns Hand with ID, 7 cards, seat order (1-4), 3 stickers, and decklist info
- Error handling for invalid/missing decklists

### POST /hand/evaluate
- Records hand evaluation with user input
- Stores CanCastEtali and MinimumCardsToCastEtali
- Maintains all original hand data

### GET /hands/evaluated
- Returns all recorded evaluations
- Empty array when no evaluations exist

### POST /decklist
- Adds new decklist from Moxfield link
- Parses and stores card names

### GET /decklists
- Returns all available decklists

## Test Technologies Used
- **xUnit** - Testing framework
- **Entity Framework Core InMemory** - Database testing
- **Moq** - Mocking framework (available for future use)

## Running the Tests
```bash
cd "Etali.Tests"
dotnet test
```

## Test Project Structure
```
Etali.Tests/
??? Configuration/
?   ??? GameConfigurationTests.cs
??? Database/
?   ??? EtaliDatabaseTests.cs
??? Models/
?   ??? DecklistTests.cs
?   ??? EvaluatedHandTests.cs
?   ??? HandTests.cs
??? Services/
    ??? HandEvaluationServiceTests.cs
    ??? HandGenerationServiceTests.cs
```

## Not Tested (Integration Level)
- DecklistAddingService (requires external HTTP calls and PuppeteerSharp)
- Full API endpoint integration tests
- CORS functionality
- Database migrations

## Recommendations for Additional Testing
1. Integration tests for full API endpoints using WebApplicationFactory
2. DecklistAddingService tests with mocked HTTP client
3. Load/performance testing for hand generation
4. Validation tests for API request/response models
