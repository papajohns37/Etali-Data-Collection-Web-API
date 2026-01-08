# Quick Start Guide - PostgreSQL Setup

## ?? Fast Track (5 Minutes)

### Option 1: Docker (Recommended for Development)

```bash
# 1. Start PostgreSQL in Docker
docker run --name etali-postgres \
  -e POSTGRES_DB=etali_db \
  -e POSTGRES_USER=etali_user \
  -e POSTGRES_PASSWORD=etali_pass \
  -p 5432:5432 \
  -d postgres:15

# 2. Configure connection string
cd "Etali Data Collection Web API"
dotnet user-secrets set "ConnectionStrings:EtaliDatabase" "Host=localhost;Database=etali_db;Username=etali_user;Password=etali_pass"

# 3. Configure JWT
dotnet user-secrets set "Jwt:SecretKey" "YourVerySecureSecretKeyThatIsAtLeast64CharactersLongForProduction123456789"

# 4. Configure admin user
dotnet user-secrets set "DefaultAdmin:Username" "admin"
dotnet user-secrets set "DefaultAdmin:Password" "Admin123!"

# 5. Install EF Core tools (if not installed)
dotnet tool install --global dotnet-ef --version 8.0.0

# 6. Create and apply migration
dotnet ef migrations add InitialPostgreSQLMigration
dotnet ef database update

# 7. Run the application
dotnet run

# Application will be available at: http://localhost:5000
# Swagger UI at: http://localhost:5000/
# Health check at: http://localhost:5000/health
```

### Option 2: Local PostgreSQL Installation

```bash
# 1. Install PostgreSQL from https://www.postgresql.org/download/

# 2. Create database and user
psql -U postgres
CREATE DATABASE etali_db;
CREATE USER etali_user WITH PASSWORD 'your_password';
GRANT ALL PRIVILEGES ON DATABASE etali_db TO etali_user;
\c etali_db
GRANT ALL ON SCHEMA public TO etali_user;
\q

# 3-7. Follow steps 2-7 from Option 1 above (using your password)
```

## ?? Verification

After running the application, verify:

```bash
# Check health endpoint
curl http://localhost:5000/health

# Expected response: Healthy
```

In browser:
1. Open http://localhost:5000/ (Swagger UI)
2. Try `/health` endpoint - should return "Healthy"
3. Create a user via `/auth/register`
4. Login via `/auth/login` - get JWT token
5. Use token to authenticate other requests

## ?? Run Tests

```bash
cd ../Etali.Tests
dotnet test
```

All tests should pass (they use InMemory database).

## ??? Troubleshooting

### "Can't connect to database"
```bash
# Check PostgreSQL is running
docker ps  # (for Docker)
# or
psql -U postgres -c "SELECT version();"  # (for local install)

# Check connection string
dotnet user-secrets list
```

### "EF Core tools not found"
```bash
dotnet tool update --global dotnet-ef --version 8.0.0
```

### "Migration already exists"
```bash
# If you need to recreate migration
dotnet ef migrations remove
dotnet ef migrations add InitialPostgreSQLMigration
dotnet ef database update
```

### "Permission denied"
```sql
-- Reconnect to database and grant permissions
psql -U postgres -d etali_db
GRANT ALL ON SCHEMA public TO etali_user;
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA public TO etali_user;
GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA public TO etali_user;
```

## ?? Clean Up (Docker)

```bash
# Stop and remove container
docker stop etali-postgres
docker rm etali-postgres

# Remove volume (deletes all data)
docker volume rm etali-postgres-data
```

## ?? Next Steps

- See `DATABASE_SETUP.md` for detailed setup instructions
- See `POSTGRESQL_MIGRATION.md` for migration guide
- See `IMPLEMENTATION_COMPLETE.md` for full feature list

## ?? Quick Commands Reference

```bash
# Docker PostgreSQL
docker start etali-postgres          # Start existing container
docker stop etali-postgres           # Stop container
docker logs etali-postgres           # View logs

# EF Core Migrations
dotnet ef migrations add Name        # Create migration
dotnet ef database update            # Apply migrations
dotnet ef migrations list            # List migrations
dotnet ef migrations remove          # Remove last migration
dotnet ef database drop              # Drop database

# User Secrets
dotnet user-secrets list             # View all secrets
dotnet user-secrets set "Key" "Val"  # Set secret
dotnet user-secrets remove "Key"     # Remove secret
dotnet user-secrets clear            # Clear all secrets

# Application
dotnet build                         # Build project
dotnet run                           # Run application
dotnet watch run                     # Run with hot reload
dotnet test                          # Run tests
```

## ? Development Workflow

```bash
# Daily workflow
docker start etali-postgres          # Start database
cd "Etali Data Collection Web API"
dotnet watch run                     # Run with hot reload

# When done
docker stop etali-postgres           # Stop database
```

## ?? Security Notes

- **Never commit user secrets** - they're stored outside the repo
- **Change default passwords** in production
- **Use strong JWT secret** (64+ characters)
- **Enable SSL** for production database connections

## ? Success!

If you can:
1. ? See "Database is up to date" in logs
2. ? Access Swagger UI at http://localhost:5000/
3. ? `/health` endpoint returns "Healthy"

You're ready to develop! ??
