# Local Development Setup Guide

This guide will help you set up the Etali Data Collection API for local development with secure secret management.

## Prerequisites

1. .NET 8.0 SDK or later
2. PostgreSQL 14 or later (or Docker for PostgreSQL)
3. Visual Studio 2022, VS Code, or JetBrains Rider
4. Git

## Step 1: Clone the Repository

```bash
git clone https://github.com/papajohns37/Etali-Data-Collection-Web-API.git
cd "Etali-Data-Collection-Web-API"
```

## Step 2: Set Up PostgreSQL Database

### Option A: Local PostgreSQL Installation

1. Install PostgreSQL: https://www.postgresql.org/download/
2. Create a database and user:

```sql
CREATE DATABASE etali_db;
CREATE USER etali_user WITH PASSWORD 'your_secure_password';
GRANT ALL PRIVILEGES ON DATABASE etali_db TO etali_user;
```

### Option B: Docker PostgreSQL

```bash
docker run --name etali-postgres -e POSTGRES_PASSWORD=your_secure_password -e POSTGRES_USER=etali_user -e POSTGRES_DB=etali_db -p 5432:5432 -d postgres:14
```

## Step 3: Configure User Secrets

**IMPORTANT**: Never commit secrets to Git! Use .NET User Secrets for local development.

### Initialize User Secrets

Open PowerShell and navigate to the project directory:

```powershell
cd "Etali Data Collection Web API"
dotnet user-secrets init
```

### Set Database Connection String

```powershell
dotnet user-secrets set "ConnectionStrings:EtaliDatabase" "Host=localhost;Database=etali_db;Username=etali_user;Password=your_secure_password"
```

### Generate and Set JWT Secret Key

First, generate a strong random key (64+ characters):

```powershell
# Generate a secure random string
$jwtSecret = -join ((65..90) + (97..122) + (48..57) | Get-Random -Count 64 | ForEach-Object {[char]$_})
Write-Host "Generated JWT Secret: $jwtSecret"

# Set the secret
dotnet user-secrets set "Jwt:SecretKey" $jwtSecret
```

### Set Other JWT Configuration

```powershell
dotnet user-secrets set "Jwt:Issuer" "EtaliDataCollectionApi"
dotnet user-secrets set "Jwt:Audience" "EtaliDataCollectionApi"
dotnet user-secrets set "Jwt:ExpirationInDays" "7"
```

### Set Admin Credentials

```powershell
dotnet user-secrets set "DefaultAdmin:Username" "admin"
dotnet user-secrets set "DefaultAdmin:Password" "AdminPass123!@#"
```

### Set Moxfield API Configuration (Optional)

```powershell
dotnet user-secrets set "MoxfieldApi:BaseUrl" "https://api2.moxfield.com/v3/decks/all/"
# If you have a Moxfield API key:
dotnet user-secrets set "MoxfieldApi:ApiKey" "YOUR_MOXFIELD_API_KEY"
```

### View Your Secrets

```powershell
dotnet user-secrets list
```

## Step 4: Apply Database Migrations

Navigate back to the solution directory and run:

```powershell
cd ..
dotnet ef database update --project "Etali Data Collection Web API"
```

If you don't have Entity Framework tools installed:

```powershell
dotnet tool install --global dotnet-ef
```

## Step 5: Build and Run

```powershell
dotnet build
dotnet run --project "Etali Data Collection Web API"
```

The API will start at:
- HTTPS: https://localhost:7000 (or your configured port)
- HTTP: http://localhost:5000 (or your configured port)

## Step 6: Test the API

### Option A: Using Swagger UI

Open your browser and navigate to:
```
https://localhost:7000
```

You should see the Swagger UI with all API endpoints documented.

### Option B: Using curl

Test the health endpoint:
```bash
curl https://localhost:7000/health
```

Login with admin credentials:
```bash
curl -X POST https://localhost:7000/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "username": "admin",
    "password": "AdminPass123!@#"
  }'
```

## Troubleshooting

### Issue: Database connection fails

**Error**: `Npgsql.NpgsqlException: Failed to connect to localhost:5432`

**Solution**:
1. Verify PostgreSQL is running: `pg_isready` or `docker ps`
2. Check your connection string in user secrets
3. Ensure the database exists: `psql -U etali_user -d etali_db`

### Issue: JWT Secret Key is invalid

**Error**: `JWT SecretKey must be at least 32 characters long`

**Solution**:
1. Regenerate a longer secret key (64+ characters recommended)
2. Update user secrets with the new key

### Issue: Migrations fail

**Error**: `Cannot connect to database` or `Migration failed`

**Solution**:
1. Ensure database exists
2. Check connection string
3. Try manual migration: `dotnet ef database update --project "Etali Data Collection Web API"`

### Issue: User Secrets not found

**Error**: Configuration values are null or default

**Solution**:
1. Verify user secrets are set: `dotnet user-secrets list`
2. Ensure you're in the correct project directory
3. Check that UserSecretsId exists in .csproj file

## Development Tips

### Hot Reload

Use `dotnet watch` for automatic recompilation on file changes:

```powershell
dotnet watch run --project "Etali Data Collection Web API"
```

### Reset Database

To completely reset your database:

```powershell
dotnet ef database drop --project "Etali Data Collection Web API"
dotnet ef database update --project "Etali Data Collection Web API"
```

### View Detailed Logs

Set log level to Debug in appsettings.Development.json:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug"
    }
  }
}
```

### Testing Authentication

1. Login to get a JWT token
2. Copy the token
3. In Swagger UI, click "Authorize" button
4. Enter: `Bearer YOUR_TOKEN_HERE`
5. Click "Authorize"
6. Now you can test authenticated endpoints

## Security Best Practices

? **DO**:
- Use User Secrets for local development
- Use strong passwords (12+ characters, mixed case, numbers, symbols)
- Generate long, random JWT secret keys (64+ characters)
- Regularly update dependencies
- Use HTTPS for local development
- Keep PostgreSQL updated

? **DON'T**:
- Never commit secrets to Git
- Don't use weak or common passwords
- Don't share your user secrets
- Don't disable SSL verification in production
- Don't use the same credentials across environments

## Next Steps

- Read RENDER_DEPLOYMENT.md for production deployment
- Review the API documentation in Swagger UI
- Check out the test suite in Etali.Tests project
- Contribute to the project on GitHub

## Additional Resources

- [.NET User Secrets](https://docs.microsoft.com/aspnet/core/security/app-secrets)
- [Entity Framework Core Migrations](https://docs.microsoft.com/ef/core/managing-schemas/migrations/)
- [ASP.NET Core Configuration](https://docs.microsoft.com/aspnet/core/fundamentals/configuration/)
- [PostgreSQL Documentation](https://www.postgresql.org/docs/)
