# Docker Quick Start Guide

Get your Etali API running with Docker in 5 minutes!

## ?? Option 1: Docker Compose (Easiest - Includes Database)

### Prerequisites
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) installed

### Start Everything
```bash
# From the solution root directory
docker-compose up -d
```

This starts:
- PostgreSQL database on port 5432
- API on port 8080

### Verify It's Running
```bash
# Check health
curl http://localhost:8080/health

# View logs
docker-compose logs -f api

# Check Swagger UI
open http://localhost:8080/
```

### Default Credentials
- **Username**: `admin`
- **Password**: `Admin123!`

### Stop Everything
```bash
docker-compose down

# To also remove data
docker-compose down -v
```

---

## ?? Option 2: Docker Only (No Database)

### Prerequisites
- Docker installed
- PostgreSQL running separately

### Build the Image
```bash
cd "Etali Data Collection Web API"
docker build -t etali-api:latest .
```

### Run the Container
```bash
docker run -d \
  --name etali-api \
  -p 8080:8080 \
  -e ConnectionStrings__EtaliDatabase="Host=host.docker.internal;Database=etali_db;Username=etali_user;Password=password" \
  -e Jwt__SecretKey="YourVeryLongSecretKeyHere1234567890ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghij" \
  -e Jwt__Issuer="EtaliDataCollectionApi" \
  -e Jwt__Audience="EtaliDataCollectionApi" \
  -e Jwt__ExpirationInDays="7" \
  -e DefaultAdmin__Username="admin" \
  -e DefaultAdmin__Password="Admin123!" \
  -e MoxfieldApi__BaseUrl="https://api2.moxfield.com/v3/decks/all/" \
  -e AllowedOrigins="*" \
  -e ASPNETCORE_ENVIRONMENT="Development" \
  etali-api:latest
```

### Check Status
```bash
# View logs
docker logs etali-api

# Follow logs
docker logs -f etali-api

# Check if running
docker ps
```

### Stop and Remove
```bash
docker stop etali-api
docker rm etali-api
```

---

## ?? Common Commands

### Docker Compose
```bash
# Start in foreground (see logs)
docker-compose up

# Start in background
docker-compose up -d

# Stop services
docker-compose down

# Rebuild images
docker-compose build

# View logs
docker-compose logs -f

# Restart just the API
docker-compose restart api

# Run migrations manually
docker-compose exec api dotnet ef database update
```

### Docker
```bash
# List running containers
docker ps

# List all containers
docker ps -a

# View logs
docker logs <container-name>

# Execute command in container
docker exec -it etali-api bash

# Remove all stopped containers
docker container prune

# Remove all unused images
docker image prune -a
```

---

## ?? Customization

### Change Admin Password
Edit `docker-compose.yml`:
```yaml
- DefaultAdmin__Password=YourNewPassword123!
```

Then restart:
```bash
docker-compose down
docker-compose up -d
```

### Change Database Password
Edit `docker-compose.yml`:
```yaml
postgres:
  environment:
    POSTGRES_PASSWORD: your-new-password

api:
  environment:
    - ConnectionStrings__EtaliDatabase=Host=postgres;Database=etali_db;Username=etali_user;Password=your-new-password
```

### Use Your Own PostgreSQL
Edit `docker-compose.yml` and remove the `postgres` service, then update the connection string in the `api` service.

---

## ?? Troubleshooting

### Port Already in Use
```bash
# Change the port in docker-compose.yml
ports:
  - "9000:8080"  # Use port 9000 instead

# Or stop the conflicting service
docker ps  # Find the container using port 8080
docker stop <container-id>
```

### Database Connection Fails
```bash
# Check if PostgreSQL is running
docker-compose ps

# View PostgreSQL logs
docker-compose logs postgres

# Restart database
docker-compose restart postgres
```

### Container Keeps Restarting
```bash
# Check logs for errors
docker-compose logs api

# Common issues:
# - JWT secret too short (needs 32+ chars)
# - Database not ready (wait a few seconds)
# - Missing environment variables
```

### Can't Access Swagger UI
```bash
# Check if API is running
curl http://localhost:8080/health

# View API logs
docker-compose logs api

# Verify port mapping
docker-compose ps
```

### Need to Reset Everything
```bash
# Stop and remove everything including data
docker-compose down -v

# Remove Docker images
docker rmi etali-api:latest

# Start fresh
docker-compose up -d
```

---

## ?? Monitoring

### View Resource Usage
```bash
# Real-time stats
docker stats

# For specific container
docker stats etali-api
```

### Access Container Shell
```bash
# API container
docker exec -it etali-api bash

# Database container
docker exec -it etali-postgres bash

# Connect to PostgreSQL
docker exec -it etali-postgres psql -U etali_user -d etali_db
```

---

## ?? Production Notes

**?? Do NOT use this docker-compose.yml in production!**

For production on Render:
- Follow `DOCKER_DEPLOYMENT_RENDER.md`
- Use Render's managed PostgreSQL
- Set strong, unique passwords
- Use environment variables (not hardcoded)
- Restrict CORS origins

---

## ? Success Checklist

- [ ] Docker Desktop running
- [ ] Ran `docker-compose up -d`
- [ ] Checked `docker-compose ps` (both containers running)
- [ ] Visited http://localhost:8080/ (Swagger UI loads)
- [ ] Tested http://localhost:8080/health (returns "Healthy")
- [ ] Logged in with admin/Admin123! via Swagger

**?? You're ready to develop!**

---

## ?? Next Steps

- Read `DOCKER_DEPLOYMENT_RENDER.md` for production deployment
- Check `LOCAL_DEVELOPMENT.md` for non-Docker local setup
- See `PRODUCTION_SETUP_COMPLETE.md` for security best practices

## ?? Tips

- Use `docker-compose logs -f api` to watch logs in real-time
- Database data persists in Docker volume (survives restarts)
- Use `docker-compose down -v` to clean up volumes
- Press `Ctrl+C` to stop `docker-compose up` (without `-d`)
