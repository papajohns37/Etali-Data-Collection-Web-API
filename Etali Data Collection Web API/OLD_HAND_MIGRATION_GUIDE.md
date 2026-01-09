# Old Hand Migration to Evaluated Hands Guide

This guide explains how to automatically migrate eligible old hand data from the `old_evaluated_hands` table to the current `evaluated_hands` format.

## Overview

The migration system processes historical hand evaluations and converts them to the current data format. It automatically:
- ? Converts eligible hands to the evaluated_hands table
- ?? Skips hands with problematic cards that require manual review
- ? Reports hands that fail validation
- ??? Removes successfully migrated records from old_evaluated_hands

## System User: excel_importer

Migrated hands are assigned to a special system user called `excel_importer`:
- **Username**: `excel_importer`
- **User ID**: Retrieved dynamically from the database (GUID)
- **Purpose**: Tracks all data migrated from CSV/Excel sources
- **Creation**: Automatically created during application startup (database seeding)
- **Login**: Cannot login (no password set)
- **Benefits**:
  - Clear audit trail for migrated data
  - Easy filtering: Query by username or ID
  - Distinguishes legacy data from new evaluations
  - Maintains referential integrity

**Important**: 
1. The `excel_importer` user is created automatically when the application starts
2. The migration service looks up the user's ID (GUID) at runtime
3. If you get foreign key errors, verify the user exists in `asp_net_users` table
4. Restart the application if the user doesn't exist

## Eligibility Criteria

### Eligible Hands (Will Be Migrated)
Hands that do **NOT** contain any of these cards:
- `_____ Goblin` (variable name affects evaluation)
- `Worldly Tutor` (variable tutoring affects hand outcomes)
- `Sylvan Tutor` (variable tutoring affects hand outcomes)
- `Gamble` (success depends on generated odds)
- `Gemstone Caverns` (exile mechanic affects gameplay)

### Ineligible Hands (Will Be Skipped)
Hands containing any of the above cards will be skipped and remain in `old_evaluated_hands` for manual review.

## What Gets Migrated

### Copied Directly
- ? `CanCastEtali` - Whether Etali can be cast
- ? `MinimumCardsToCastEtali` - Minimum cards needed (1-7 or 8)
- ? `DecklistName` - Name of the decklist
- ? Card names (converted to Card objects with types)

### Generated/Set Automatically
- ?? `SeatOrder` - Random value (1-4)
- ?? `Stickers` - 3 random stickers from available pool
- ?? `LowestGambleOdds` - Random float (0.0-1.0)
- ?? `CreatedByUserId` - Set to `"excel_importer"` (system user for migrated data)
- ?? `Id` - Sequential ID starting from max(evaluated_hands.id) + 1
- ?? `DecklistId` - From old_evaluated_hands.decklist_id

> **Note:** Since eligible hands don't contain Gamble or variable cards, the random generation of seat order, stickers, and gamble odds doesn't affect the evaluation outcome.

## Migration Process

### Prerequisites
1. Old hand data imported to `old_evaluated_hands` table
2. Decklists matched (have valid `decklist_id`)
3. Admin authentication credentials

### Step 1: Check Current Status

Get statistics on old hands:
```bash
curl http://localhost:5000/migration/old-hands/stats
```

Response:
```json
{
  "totalRecords": 1250,
  "uniqueDecklists": 15,
  "uniqueUsers": 8,
  "recordsMatchedToDecklist": 1180,
  "recordsUnmatchedToDecklist": 70
}
```

### Step 2: Run Migration

#### Using Swagger UI (Recommended)

1. Navigate to `http://localhost:5000`
2. Authenticate as Admin
   - Use `/auth/login` endpoint
   - Copy JWT token
   - Click "Authorize" ? Enter `Bearer {token}`
3. Find **Migration** section
4. Execute `POST /migration/migrate-old-hands`
5. Review results

#### Using curl

```bash
# Get admin token
TOKEN=$(curl -X POST http://localhost:5000/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"your-password"}' \
  | jq -r '.token')

# Run migration
curl -X POST http://localhost:5000/migration/migrate-old-hands \
  -H "Authorization: Bearer $TOKEN"
```

### Step 3: Review Results

Response structure:
```json
{
  "migrated": 980,
  "skipped": 45,
  "failed": 5,
  "total": 1030
}
```

Where:
- **migrated**: Hands successfully converted to evaluated_hands
- **skipped**: Hands with problematic cards (remain in old_evaluated_hands)
- **failed**: Hands that encountered errors during migration
- **total**: Sum of all processed hands

### Step 4: Verify Migration

Check migrated hands:
```bash
curl http://localhost:5000/hands/evaluated | jq 'map(select(.createdByUserId == "excel_importer"))'
```

Check remaining old hands:
```bash
curl http://localhost:5000/migration/old-hands
```

## Important Notes

### ?? Destructive Operation
- Successfully migrated records are **deleted** from `old_evaluated_hands`
- This operation cannot be undone
- **Backup your database** before running

### Card Type Resolution
The migration service attempts to match card names from old hands with cards in the decklist to get proper types:
- ? Match found ? Uses decklist card type
- ? No match ? Uses "Unknown" type and logs warning

### Failure Handling
Hands fail to migrate if:
- Decklist ID is null or invalid
- Decklist not found in database
- Card name is missing/empty
- Database constraint violation

Failed hands remain in `old_evaluated_hands` and are logged with error details.

## Common Scenarios

### All Hands Skipped
If all hands are skipped, they likely contain problematic cards. Review the hands manually:
```bash
curl http://localhost:5000/migration/old-hands | jq '.[] | {id, decklistName, card1, card2, card3, card4, card5, card6, card7}'
```

### Some Hands Failed
Check application logs for detailed error messages:
```bash
# In your application logs, look for:
# "Failed to migrate old hand {Id} from '{DecklistName}'"
```

Common causes:
- Decklist was deleted
- Card not in decklist
- Database connection issues

### Records Unmatched to Decklist
Hands without `decklist_id` will fail. Match them first:
1. Import missing decklists
2. Re-run CSV import (updates decklist_id)
3. Run migration again

## Post-Migration

### Verify Data Integrity
```sql
-- Count by source
SELECT created_by_user_id, COUNT(*) 
FROM evaluated_hands 
GROUP BY created_by_user_id;

-- Check for excel_importer records
SELECT COUNT(*) FROM evaluated_hands 
WHERE created_by_user_id = 'excel_importer';
```

### Handle Remaining Old Hands
Hands left in `old_evaluated_hands` require manual review:
1. Export to CSV for analysis
2. Manually evaluate problematic cards
3. Create new evaluated_hands entries if appropriate
4. Or keep as historical reference

## Rollback

Since this is a destructive operation, rollback requires:
1. Database backup restored
2. Or re-import from original CSV

**Always backup before migration!**

## API Reference

### POST /migration/migrate-old-hands
**Auth:** Admin only  
**Description:** Migrate eligible old hands to evaluated hands  
**Returns:** `OldHandMigrationResult`

```typescript
interface OldHandMigrationResult {
  migrated: number;    // Successfully migrated
  skipped: number;     // Contains problematic cards
  failed: number;      // Encountered errors
  total: number;       // All processed
}
```

## Troubleshooting

### Foreign Key Constraint Error
If you see: `violates foreign key constraint "FK_evaluated_hands_asp_net_users_created_by_user_id"`

**Solution**:
1. Verify the `excel_importer` user exists:
```sql
SELECT id, user_name, created_at 
FROM asp_net_users 
WHERE user_name = 'excel_importer';
```

2. If user doesn't exist, restart the application to create it

3. Check application logs for:
```
info: Program[0]
      Created system user: excel_importer (for migrated data)
```

4. The migration service automatically looks up the user ID, so if the user exists, the migration should work

**Technical Details**:
- The foreign key requires the user **ID** (GUID), not the username
- The migration service queries the database for: `SELECT id FROM asp_net_users WHERE user_name = 'excel_importer'`
- This ID is then used for all migrated hands

### Migration returns 0 migrated
- Check if old_evaluated_hands has records
- Verify decklists exist and match
- Ensure none contain problematic cards

### High failure rate
- Check database logs for constraint violations
- Verify decklist_id is set for old hands
- Ensure decklists haven't been deleted

### Cards show "Unknown" type
- Card name doesn't match any in decklist
- Check for typos in card names
- Verify decklist is complete

## Need Help?

1. Check application logs for detailed error messages
2. Verify old hand data structure
3. Ensure all prerequisite data is loaded
4. Test with a small subset first

## Example Workflow

```bash
# 1. Check status
curl http://localhost:5000/migration/old-hands/stats

# 2. Login
TOKEN=$(curl -X POST http://localhost:5000/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"pass"}' \
  | jq -r '.token')

# 3. Backup database (PostgreSQL example)
pg_dump -U postgres etali_db > backup_before_migration.sql

# 4. Run migration
curl -X POST http://localhost:5000/migration/migrate-old-hands \
  -H "Authorization: Bearer $TOKEN" | jq '.'

# 5. Verify results
curl http://localhost:5000/hands/evaluated \
  | jq 'map(select(.createdByUserId == "excel_importer")) | length'

# 6. Check remaining
curl http://localhost:5000/migration/old-hands | jq 'length'
```

## Success!

Once migration is complete:
- ? Eligible hands are in `evaluated_hands` table
- ? Available for statistics and analysis
- ? Integrated with existing evaluation data
- ? Problematic hands preserved for manual review

### ?? Statistics Impact
- Migrated hands count toward decklist statistics
- CreatedByUserId = "excel_importer" allows filtering for migrated data
- All legacy evaluations from same system user for consistency
