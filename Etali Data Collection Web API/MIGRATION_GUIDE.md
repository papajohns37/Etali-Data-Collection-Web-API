# CSV Data Migration Guide

This guide explains how to migrate old hand evaluation data from the CSV spreadsheet into the database.

## Overview

The migration system allows you to import historical hand evaluation data from a CSV file into the `old_evaluated_hands` table. This preserves the original data while allowing you to work with it in the database.

## CSV Format

The CSV file should have the following columns (header row required):

```
Decklist Name,User,Can Cast Etali,Minimum Cards,Card 1,Card 2,Card 3,Card 4,Card 5,Card 6,Card 7
```

### Example CSV Row:
```csv
Fast Etali,John,true,3,Sol Ring,Mana Vault,Ancient Tomb,Forest,Mountain,Plains,Island
```

## File Location

Place your CSV file at: `OldHandData.csv` in the project root directory (same directory as the `.csproj` file).

Alternatively, you can specify a custom path when calling the import endpoint.

## Migration Process

### Step 1: Apply Database Migration

First, ensure the database has the new `old_evaluated_hands` table by running the migration:

```bash
dotnet ef database update --project "Etali Data Collection Web API"
```

This will create the table with the following structure:
- `id` (auto-generated)
- `decklist_name`
- `decklist_id` (FK to decklists, nullable)
- `user`
- `can_cast_etali`
- `minimum_cards_to_cast_etali`
- `card1` through `card7`
- `imported_at` (timestamp)

### Step 2: Prepare Your CSV File

1. Export your data from the spreadsheet to CSV format
2. Ensure the header row matches the expected format exactly
3. Verify Boolean values are formatted as: `true`/`false`, `yes`/`no`, `y`/`n`, or `1`/`0`
4. Place the file in the project directory as `OldHandData.csv`

### Step 3: Run the API

Start the application:

```bash
dotnet run --project "Etali Data Collection Web API"
```

### Step 4: Import the Data

#### Option A: Using Swagger UI (Recommended)

1. Navigate to the Swagger UI (typically at `http://localhost:5000` or `https://localhost:5001`)
2. Authenticate with an Admin account:
   - Use the `/auth/login` endpoint
   - Copy the JWT token from the response
   - Click "Authorize" button at the top of Swagger
   - Enter `Bearer {your-token}` and click "Authorize"
3. Find the **Migration** section
4. Use the `POST /migration/import-old-hands` endpoint
5. Leave the `csvFilePath` parameter empty to use the default location
6. Click "Execute"

#### Option B: Using curl

```bash
# First, get an admin token
curl -X POST http://localhost:5000/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"your-password"}'

# Then use the token to import
curl -X POST "http://localhost:5000/migration/import-old-hands" \
  -H "Authorization: Bearer {your-token}"
```

#### Option C: Custom File Path

If your CSV is in a different location:

```bash
curl -X POST "http://localhost:5000/migration/import-old-hands?csvFilePath=C:/path/to/your/file.csv" \
  -H "Authorization: Bearer {your-token}"
```

### Step 5: Verify the Import

Check the import results:

```bash
# Get statistics
curl http://localhost:5000/migration/old-hands/stats

# View all imported data
curl http://localhost:5000/migration/old-hands
```

## Data Mapping

The migration service will:

1. **Parse each CSV row** into an `OldEvaluatedHand` record
2. **Match decklist names** to existing decklists in the database
   - Case-insensitive matching
   - Sets `decklist_id` if a match is found
   - Leaves `decklist_id` as `null` if no match
3. **Store card names** as-is (note: card types are not available in old data)
4. **Set import timestamp** to current UTC time

## Limitations & Notes

### Data Not Available in Old Format:
- **Stickers**: The original spreadsheet didn't track sticker values
- **Seat Order**: Turn order wasn't recorded
- **Lowest Gamble Odds**: Probability calculations weren't tracked
- **Card Types**: Only card names were recorded, not their types
- **Decklist ID**: Will be `null` if no matching decklist exists

### Recommendations:
1. Import decklists **before** importing old hand data for better matching
2. Review unmatched records (where `decklist_id` is null) after import
3. Consider this data as historical/reference only
4. New evaluations should use the current `evaluated_hands` table

## Troubleshooting

### Import Fails with "CSV file not found"
- Verify the file path is correct
- Ensure the file has read permissions
- Try using an absolute path

### Import Fails with "Expected 11 fields"
- Check that your CSV has all columns
- Verify there are no extra commas in card names
- Ensure quoted fields are properly escaped

### Some Records Not Matched to Decklists
- Use the stats endpoint to see how many matched
- Check that decklist names in CSV match exactly (case-insensitive)
- Consider updating decklist names or re-importing decklists

### Authentication Failed
- Ensure you have an Admin account
- Verify the JWT token is valid and not expired
- Check that you're including "Bearer " prefix in Authorization header

## API Endpoints Reference

| Method | Endpoint | Auth Required | Description |
|--------|----------|---------------|-------------|
| POST | `/migration/import-old-hands` | Admin | Import CSV data |
| GET | `/migration/old-hands` | None | Get all imported records |
| GET | `/migration/old-hands/stats` | None | Get import statistics |

## Example Statistics Response

```json
{
  "totalRecords": 1250,
  "uniqueDecklists": 15,
  "uniqueUsers": 8,
  "recordsMatchedToDecklist": 1180,
  "recordsUnmatchedToDecklist": 70
}
```

## Need Help?

If you encounter issues:
1. Check the application logs for detailed error messages
2. Verify your CSV format matches the expected structure
3. Ensure the database migration has been applied
4. Confirm you have Admin authentication
