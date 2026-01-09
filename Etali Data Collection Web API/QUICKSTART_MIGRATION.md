# Quick Start - CSV Migration

## Prerequisites
? Database migration applied
? Admin account available
? CSV file prepared

## Step 1: Apply Migration
```bash
dotnet ef database update --project "Etali Data Collection Web API"
```

## Step 2: Place CSV File
Put your `OldHandData.csv` in:
```
Etali Data Collection Web API/OldHandData.csv
```

## Step 3: Start API
```bash
dotnet run --project "Etali Data Collection Web API"
```

## Step 4: Import via Swagger
1. Open browser ? `http://localhost:5000`
2. Login ? `/auth/login` ? Get token
3. Click **Authorize** ? Enter `Bearer {token}`
4. Find **Migration** section
5. Execute `POST /migration/import-old-hands`

## Check Results
```bash
# Statistics
curl http://localhost:5000/migration/old-hands/stats

# View data
curl http://localhost:5000/migration/old-hands
```

## CSV Format
```csv
Decklist Name,User,Can Cast Etali,Minimum Cards,Card 1,Card 2,Card 3,Card 4,Card 5,Card 6,Card 7
Fast Etali,John,true,3,Sol Ring,Mana Vault,Ancient Tomb,Forest,Mountain,Plains,Island
```

## Endpoints Created
| Endpoint | Auth | Description |
|----------|------|-------------|
| `POST /migration/import-old-hands` | Admin | Import CSV |
| `GET /migration/old-hands` | None | Get all data |
| `GET /migration/old-hands/stats` | None | Get statistics |

## New Table: `old_evaluated_hands`
Stores historical evaluation data with:
- Decklist name and optional FK
- User name
- Can cast Etali (boolean)
- Minimum cards needed (1-8)
- 7 card names
- Import timestamp

See **MIGRATION_GUIDE.md** for detailed instructions.
