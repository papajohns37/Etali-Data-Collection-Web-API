# Quick Reference - Old Hand Migration

## One Command Migration

```bash
# Login and migrate in one go
curl -X POST http://localhost:5000/migration/migrate-old-hands \
  -H "Authorization: Bearer $(curl -X POST http://localhost:5000/auth/login \
    -H 'Content-Type: application/json' \
    -d '{"username":"admin","password":"your-password"}' \
    | jq -r '.token')"
```

## What It Does

| Action | Result |
|--------|--------|
| ? Eligible hands | Migrated to `evaluated_hands` |
| ?? Problematic cards | Skipped, stays in `old_evaluated_hands` |
| ? Errors | Failed, stays in `old_evaluated_hands` + logged |

## Problematic Cards (Auto-Skipped)

- `_____ Goblin`
- `Worldly Tutor`
- `Sylvan Tutor`
- `Gamble`
- `Gemstone Caverns`

## What Gets Generated

- **Seat Order**: Random (1-4)
- **Stickers**: 3 random values
- **Gamble Odds**: Random (0.0-1.0)
- **User**: "excel_importer"
- **Hand ID**: Sequential

## Before You Run

```bash
# 1. Backup database
pg_dump -U postgres etali_db > backup.sql

# 2. Check what you have
curl http://localhost:5000/migration/old-hands/stats

# 3. IMPORTANT: Restart the application to ensure excel_importer user is created
# The system user is created automatically during database seeding on startup
```

## After Migration

```bash
# View migrated hands
curl http://localhost:5000/hands/evaluated | jq 'map(select(.createdByUserId == "excel_importer"))'

# View remaining old hands
curl http://localhost:5000/migration/old-hands
```

## Response Format

```json
{
  "migrated": 980,    // ? Success
  "skipped": 45,      // ?? Has problematic cards
  "failed": 5,        // ? Errors
  "total": 1030       // All processed
}
```

## Swagger UI

1. Open `http://localhost:5000`
2. Login ? Get token
3. Authorize with token
4. **Migration** section
5. `POST /migration/migrate-old-hands`
6. Execute

## ?? Important

- **Destructive**: Deletes migrated records from old_evaluated_hands
- **Admin Only**: Requires Admin role
- **Backup First**: Cannot undo easily

## See Also

- `OLD_HAND_MIGRATION_GUIDE.md` - Detailed guide
- `OLD_HAND_MIGRATION_IMPLEMENTATION.md` - Technical details
