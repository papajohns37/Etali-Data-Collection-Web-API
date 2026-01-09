# Old CSV Data Migration - Implementation Summary

## What Was Built

A complete system to migrate historical hand evaluation data from a CSV spreadsheet into the database.

## Components Created

### 1. Database Model (`Models/OldEvaluatedHand.cs`)
- Represents the old CSV data structure
- Fields: DecklistName, User, CanCastEtali, MinimumCardsToCastEtali, Card1-Card7
- Includes ImportedAt timestamp and optional DecklistId foreign key
- Automatically applies snake_case naming for PostgreSQL

### 2. Database Table Configuration (`EtaliDatabase.cs`)
- Added `DbSet<OldEvaluatedHand> OldEvaluatedHands`
- Configured entity with:
  - Proper column types and max lengths
  - Indexes on DecklistName, DecklistId, and User
  - Optional foreign key to Decklist table
  - Check constraint for MinimumCardsToCastEtali (1-8)

### 3. Migration Service (`Services/CsvMigrationService.cs`)
- **Interface**: `ICsvMigrationService`
- **Implementation**: `CsvMigrationService`
- Features:
  - CSV parsing with proper quote handling
  - Boolean parsing (supports true/false, yes/no, y/n, 1/0)
  - Automatic decklist name matching (case-insensitive)
  - Batch import (100 records at a time)
  - Comprehensive error handling and logging

### 4. API Endpoints (`Endpoints/MigrationEndpoints.cs`)
Three new endpoints under `/migration`:

#### `POST /migration/import-old-hands` (Admin Only)
- Imports CSV data from file
- Default location: `OldHandData.csv` in application directory
- Optional parameter: `csvFilePath` for custom location
- Returns: Number of records imported

#### `GET /migration/old-hands` (Public)
- Returns all imported old hand data
- Useful for verification and analysis

#### `GET /migration/old-hands/stats` (Public)
- Returns statistics:
  - Total records
  - Unique decklists
  - Unique users
  - Records matched/unmatched to decklists

### 5. Database Migration
- Entity Framework migration: `AddOldEvaluatedHandsTable`
- Creates `old_evaluated_hands` table in PostgreSQL
- Includes all indexes and constraints

### 6. Documentation
- **MIGRATION_GUIDE.md**: Complete step-by-step guide
- **OldHandData.sample.csv**: Example CSV file for testing

## Service Registration

Added to `Program.cs`:
```csharp
builder.Services.AddScoped<ICsvMigrationService, CsvMigrationService>();
```

Endpoints mapped:
```csharp
app.MapMigrationEndpoints();
```

## Database Schema

Table: `old_evaluated_hands`

| Column | Type | Constraints |
|--------|------|-------------|
| id | integer | Primary Key, Auto-increment |
| decklist_name | varchar(500) | Required, Indexed |
| decklist_id | integer | Nullable, FK to decklists, Indexed |
| user | varchar(100) | Required, Indexed |
| can_cast_etali | boolean | Required |
| minimum_cards_to_cast_etali | integer | Required, CHECK (1-8) |
| card1 | varchar(200) | Nullable |
| card2 | varchar(200) | Nullable |
| card3 | varchar(200) | Nullable |
| card4 | varchar(200) | Nullable |
| card5 | varchar(200) | Nullable |
| card6 | varchar(200) | Nullable |
| card7 | varchar(200) | Nullable |
| imported_at | timestamp with time zone | Required |

## CSV Format Required

```csv
Decklist Name,User,Can Cast Etali,Minimum Cards,Card 1,Card 2,Card 3,Card 4,Card 5,Card 6,Card 7
```

## How to Use

1. **Apply Migration**:
   ```bash
   dotnet ef database update --project "Etali Data Collection Web API"
   ```

2. **Prepare CSV File**:
   - Place `OldHandData.csv` in the project directory
   - Or use a custom path

3. **Run API and Import**:
   - Start the application
   - Authenticate as Admin
   - POST to `/migration/import-old-hands`

4. **Verify**:
   - GET `/migration/old-hands/stats` to see statistics
   - GET `/migration/old-hands` to view all data

## Key Features

### Intelligent Decklist Matching
- Automatically matches CSV decklist names to existing database records
- Case-insensitive comparison
- Sets foreign key relationship when match found
- Tracks unmatched records for review

### Robust CSV Parsing
- Handles quoted fields
- Supports various boolean formats
- Batch processing for performance
- Detailed error logging

### Security
- Import endpoint requires Admin role
- Read endpoints are public for data analysis
- Follows existing authentication patterns

### Data Integrity
- All constraints enforced
- Timestamps automatically set
- Database relationships preserved
- Original data format maintained

## Testing

Sample data provided in `OldHandData.sample.csv`:
- 6 sample records
- Multiple users and decklists
- Mix of castable and non-castable hands
- Various minimum card counts

## Integration with Existing System

- Uses existing `EtaliDatabase` context
- Follows established naming conventions (snake_case)
- Integrates with authentication/authorization
- Documented in Swagger UI
- Consistent with other endpoints

## What's Different from Current Data

Old data does NOT include:
- ? Stickers (3 random values)
- ? Seat order (1-4)
- ? Lowest Gamble odds
- ? Card types (only names)

This keeps historical data separate while preserving its original structure.

## Future Enhancements (Optional)

Could add:
1. Data transformation to convert old records to new format
2. Bulk export functionality
3. Duplicate detection
4. Data validation before import
5. Update existing records by ID

## Files Modified

1. `EtaliDatabase.cs` - Added DbSet and entity configuration
2. `Program.cs` - Registered service and endpoints

## Files Created

1. `Models/OldEvaluatedHand.cs` - Data model
2. `Services/CsvMigrationService.cs` - Import logic
3. `Endpoints/MigrationEndpoints.cs` - API endpoints
4. `Migrations/[timestamp]_AddOldEvaluatedHandsTable.cs` - DB migration
5. `MIGRATION_GUIDE.md` - User documentation
6. `OldHandData.sample.csv` - Sample data

## Build Status

? All changes compile successfully
? Migration generated without errors
? Follows .NET 8 and C# 12 conventions
