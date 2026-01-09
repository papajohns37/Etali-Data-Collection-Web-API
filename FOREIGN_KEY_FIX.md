# Bug Fix: Foreign Key Constraint Violation

## Issue
Migration was failing with:
```
violates foreign key constraint "FK_evaluated_hands_asp_net_users_created_by_user_id"
```

## Root Cause
The code was using the **username** `"excel_importer"` (a string) instead of the **user ID** (a GUID).

```csharp
// ? WRONG - This is the username, not the ID
CreatedByUserId: "excel_importer"

// ? CORRECT - This is the user's ID (GUID)
CreatedByUserId: "a3f5d8e2-4b1c-4d9e-8f2a-1c3d5e7f9b2a"
```

The `CreatedByUserId` field is a foreign key that references `asp_net_users.id`, which is a GUID, not the username.

## Solution
Updated `OldHandMigrationService` to look up the user ID at runtime:

### Changes Made

#### 1. Renamed Constant
```csharp
// Before
private const string ExcelImporterUserId = "excel_importer";

// After
private const string ExcelImporterUsername = "excel_importer";
```

#### 2. Added User ID Lookup
```csharp
public async Task<OldHandMigrationResult> MigrateEligibleHandsAsync()
{
    // Look up the excel_importer user to get their GUID
    var excelImporterUser = await _db.Users
        .FirstOrDefaultAsync(u => u.UserName == ExcelImporterUsername);
    
    if (excelImporterUser == null)
    {
        throw new InvalidOperationException(
            $"System user '{ExcelImporterUsername}' not found. " +
            "The application must be restarted to create this user.");
    }
    
    var excelImporterUserId = excelImporterUser.Id; // This is the GUID
    
    // Pass the ID to CreateEvaluatedHandFromOldAsync
    // ...
}
```

#### 3. Updated Method Signature
```csharp
private async Task<EvaluatedHand> CreateEvaluatedHandFromOldAsync(
    OldEvaluatedHand oldHand, 
    Decklist decklist,
    int newHandId,
    string excelImporterUserId)  // Added parameter
{
    // ...
    return new EvaluatedHand(
        // ...
        CreatedByUserId: excelImporterUserId  // Use the GUID, not username
    );
}
```

## How It Works Now

1. **Application Startup**
   - `SeedDatabaseAsync` creates `excel_importer` user
   - User gets a GUID as their `id` (e.g., `a3f5d8e2-...`)

2. **Migration Start**
   - `MigrateEligibleHandsAsync` queries: `SELECT id FROM asp_net_users WHERE user_name = 'excel_importer'`
   - Stores the GUID in `excelImporterUserId` variable

3. **Creating Evaluated Hands**
   - Passes the GUID to `CreateEvaluatedHandFromOldAsync`
   - Sets `CreatedByUserId` to the GUID
   - Foreign key constraint is satisfied

## Testing

### Verify User Exists
```sql
SELECT id, user_name, created_at 
FROM asp_net_users 
WHERE user_name = 'excel_importer';
```

Expected output:
```
id                                    | user_name      | created_at
--------------------------------------|----------------|---------------------------
a3f5d8e2-4b1c-4d9e-8f2a-1c3d5e7f9b2a | excel_importer | 2024-01-15 10:30:00+00
```

### Check Migration Logs
```
info: Etali_Data_Collection_Web_API.Services.OldHandMigrationService[0]
      Using system user 'excel_importer' (ID: a3f5d8e2-4b1c-4d9e-8f2a-1c3d5e7f9b2a) for migrated data
```

### Verify Migrated Hands
```sql
SELECT 
    e.id,
    e.decklist_name,
    u.user_name,
    e.created_by_user_id
FROM evaluated_hands e
JOIN asp_net_users u ON e.created_by_user_id = u.id
WHERE u.user_name = 'excel_importer';
```

## Error Handling

If the user doesn't exist, the migration now throws a clear error:
```
InvalidOperationException: System user 'excel_importer' not found. 
The application must be restarted to create this user.
```

This prevents the confusing foreign key error and tells the user exactly what to do.

## Benefits of This Approach

1. ? **Correct** - Uses actual user ID (GUID) not username
2. ? **Dynamic** - Looks up ID at runtime, no hardcoded GUIDs
3. ? **Clear Errors** - Helpful message if user doesn't exist
4. ? **Logged** - Shows which user ID is being used
5. ? **Flexible** - Works regardless of the actual GUID assigned

## Files Modified

1. `Etali Data Collection Web API\Services\OldHandMigrationService.cs`
   - Added user lookup logic
   - Updated method signature
   - Better error handling

2. `Etali Data Collection Web API\OLD_HAND_MIGRATION_GUIDE.md`
   - Clarified ID vs username
   - Updated troubleshooting section

## Build Status

? Build successful - ready to use!

## Next Steps

1. Run the migration again
2. It should now work correctly
3. Check logs for: "Using system user 'excel_importer' (ID: ...)"
4. Verify migrated hands have correct `created_by_user_id`
