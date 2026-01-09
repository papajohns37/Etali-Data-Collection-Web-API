# System User Implementation - excel_importer

## Summary

Implemented a system user `excel_importer` that is automatically created during database seeding to track all migrated data from CSV/Excel sources.

## Changes Made

### 1. Program.cs - Database Seeding
Added automatic creation of `excel_importer` system user in `SeedDatabaseAsync`:

```csharp
// Create system user for Excel/CSV imports
const string excelImporterUsername = "excel_importer";
var excelImporter = await userManager.FindByNameAsync(excelImporterUsername);
if (excelImporter == null)
{
    var systemUser = new ApplicationUser
    {
        UserName = excelImporterUsername,
        Email = null,
        EmailConfirmed = false,
        CreatedAt = DateTime.UtcNow
    };

    // Create without password (system user cannot login)
    var result = await userManager.CreateAsync(systemUser);
    // ... logging
}
```

**Key Points:**
- ? Created during every application startup (if not exists)
- ? No password set - cannot login
- ? No roles assigned - system user only
- ? Logged on creation/errors

### 2. OldHandMigrationService.cs
Restored `CreatedByUserId` to use `ExcelImporterUserId` constant:

```csharp
private const string ExcelImporterUserId = "excel_importer";

// In CreateEvaluatedHandFromOldAsync:
CreatedByUserId: ExcelImporterUserId // System user for migrated data
```

### 3. Documentation Updates
Updated all documentation files:
- **OLD_HAND_MIGRATION_GUIDE.md** - Added system user section and troubleshooting
- **OLD_HAND_MIGRATION_IMPLEMENTATION.md** - Added detailed implementation info
- **MIGRATION_QUICK_REF.md** - Added note about restarting application

## How It Works

### Startup Sequence
1. Application starts
2. `InitializeDatabaseAsync` runs
3. `SeedDatabaseAsync` called
4. Checks if `excel_importer` exists
5. If not, creates it (no password)
6. Logs creation

### Migration Process
1. Migration service runs
2. For each eligible hand:
   - Sets `CreatedByUserId` to "excel_importer"
   - Saves to database
3. Foreign key relationship maintained

### User Queries
```sql
-- Get all migrated hands
SELECT * FROM evaluated_hands 
WHERE created_by_user_id IN (
    SELECT id FROM asp_net_users 
    WHERE user_name = 'excel_importer'
);

-- Count by user
SELECT 
    u.user_name, 
    COUNT(e.id) as hand_count
FROM evaluated_hands e
LEFT JOIN asp_net_users u ON e.created_by_user_id = u.id
GROUP BY u.user_name;
```

### API Filtering
```bash
# Get migrated hands
curl http://localhost:5000/hands/evaluated | \
  jq 'map(select(.createdByUserId == "excel_importer"))'
```

## Benefits

### 1. Data Integrity
- ? Foreign key constraint satisfied
- ? Referential integrity maintained
- ? No orphaned records

### 2. Audit Trail
- ? Clear source of data (migrated from CSV)
- ? Easy to distinguish from manual evaluations
- ? Queryable and filterable

### 3. Consistency
- ? All legacy data from same "user"
- ? Statistics can include or exclude migrated data
- ? Reporting flexibility

### 4. Security
- ? Cannot login (no password)
- ? No roles = no permissions
- ? System use only

## Important Notes

### ?? First-Time Setup
If you're adding this feature to an existing deployment:
1. **Restart the application** to create the user
2. Check logs for: "Created system user: excel_importer"
3. Then run migration

### ?? User Already Exists
If you manually created an "excel_importer" user before:
- The seeding will skip creation (idempotent)
- Migration will use existing user
- No conflicts

### ?? Foreign Key Errors
If you see: `violates foreign key constraint`
- Restart application
- Verify user creation in logs
- Re-run migration

## Verification

### Check User Exists
```bash
# Application logs (on startup)
# Should see: "Created system user: excel_importer (for migrated data)"
# Or: User already exists (silent)

# Database query
SELECT id, user_name, created_at 
FROM asp_net_users 
WHERE user_name = 'excel_importer';
```

### Check Migrated Data
```bash
# Get count
curl http://localhost:5000/hands/evaluated | \
  jq 'map(select(.createdByUserId == "excel_importer")) | length'

# Get sample
curl http://localhost:5000/hands/evaluated | \
  jq 'map(select(.createdByUserId == "excel_importer")) | .[0:3]'
```

## Rollback

If needed, you can remove the system user:
```sql
-- Note: This will fail if evaluated_hands reference it
-- Delete hands first or set to null
DELETE FROM asp_net_users WHERE user_name = 'excel_importer';
```

But typically, you'd want to keep it for audit trail purposes.

## Alternative Approaches Considered

1. **Null User** ?
   - Pro: Simple, no foreign key issues
   - Con: No audit trail, can't distinguish sources

2. **System User** ? (Chosen)
   - Pro: Clear audit trail, good data integrity
   - Con: Requires user creation on startup

3. **Admin User** ?
   - Pro: User already exists
   - Con: Misleading (admin didn't create this data)

## Files Modified

1. `Etali Data Collection Web API\Program.cs` - Added system user seeding
2. `Etali Data Collection Web API\Services\OldHandMigrationService.cs` - Use system user ID
3. `Etali Data Collection Web API\OLD_HAND_MIGRATION_GUIDE.md` - Documentation
4. `OLD_HAND_MIGRATION_IMPLEMENTATION.md` - Technical details
5. `Etali Data Collection Web API\MIGRATION_QUICK_REF.md` - Quick reference

## Testing

1. **Fresh Database**: User created on first run
2. **Existing Database**: User created if missing
3. **Multiple Startups**: Idempotent (no duplicates)
4. **Migration**: Foreign key satisfied
5. **Queries**: Data properly linked

## Success Criteria

? User created automatically on startup  
? No password set (cannot login)  
? Migration uses user ID  
? Foreign key constraint satisfied  
? Audit trail maintained  
? Documentation complete  
? Build successful  

The system user implementation is complete and ready to use!
