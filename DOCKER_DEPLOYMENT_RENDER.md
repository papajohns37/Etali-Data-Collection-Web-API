# Docker Deployment on Render - Complete Guide

This guide explains how to deploy your Etali Data Collection API as a Docker container on Render.

## ?? What's Been Configured

Your application now includes:
- **`Dockerfile`** - Multi-stage Docker build optimized for .NET 8
- **`.dockerignore`** - Excludes unnecessary files from Docker build
- Port 8080 configuration (Render's default)
- Health check endpoint integration
- Non-root user for security

## ?? Deployment Steps

### Step 1: Create PostgreSQL Database on Render

1. Log in to [Render Dashboard](https://dashboard.render.com/)
2. Click **New +** ? **PostgreSQL**
3. Configure:
   - **Name**: `etali-database`
   - **Database**: `etali_db`
   - **User**: `etali_user`
   - **Region**: Choose closest to your users
   - **Plan**: Select based on needs
4. Click **Create Database**
5. **Copy the Internal Database URL** (starts with `postgresql://`)

### Step 2: Push Your Code to GitHub

```bash
# Add the new Docker files
git add "Etali Data Collection Web API/Dockerfile"
git add "Etali Data Collection Web API/.dockerignore"
git add DOCKER_DEPLOYMENT_RENDER.md

# Commit
git commit -m "Add Docker support for Render deployment"

# Push to GitHub
git push origin master
```

### Step 3: Create Web Service on Render

1. In Render Dashboard, click **New +** ? **Web Service**
2. Connect your GitHub repository
3. Configure the service:

#### Basic Settings
- **Name**: `etali-data-collection-api`
- **Region**: Same as your database
- **Branch**: `master`
- **Root Directory**: Leave empty
- **Runtime**: **Docker**
- **Dockerfile Path**: `Etali Data Collection Web API/Dockerfile`

#### Instance Type
- **Free**: For testing (spins down after inactivity)
- **Starter** ($7/month): For small production
- **Standard** ($25/month): For production with guaranteed uptime

### Step 4: Configure Environment Variables

In the **Environment** section, add these variables:

```plaintext
# Database Connection
ConnectionStrings__EtaliDatabase = <your-internal-database-url>

# JWT Configuration
Jwt__SecretKey = <64-char-random-string>
Jwt__Issuer = EtaliDataCollectionApi
Jwt__Audience = EtaliDataCollectionApi
Jwt__ExpirationInDays = 7

# Admin Account
DefaultAdmin__Username = admin
DefaultAdmin__Password = <strong-password>

# Moxfield API
MoxfieldApi__BaseUrl = https://api2.moxfield.com/v3/decks/all/
MoxfieldApi__ApiKey = <your-key-or-leave-empty>

# CORS
AllowedOrigins = *

# Environment
ASPNETCORE_ENVIRONMENT = Production
```

**Generate JWT Secret:**
```powershell
-join ((65..90) + (97..122) + (48..57) | Get-Random -Count 64 | ForEach-Object {[char]$_})
```

### Step 5: Configure Health Check

In **Settings** ? **Health Check Path**:
```
/health
```

### Step 6: Deploy

1. Click **Create Web Service**
2. Render will automatically:
   - Clone your repository
   - Build the Docker image
   - Run the container
   - Apply database migrations

## ?? Verify Deployment

### Check Health
```bash
curl https://your-app.onrender.com/health
```
Expected response: `Healthy`

### Check Swagger UI
Visit in browser:
```
https://your-app.onrender.com/
```

### Test Authentication
```bash
curl -X POST https://your-app.onrender.com/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "username": "admin",
    "password": "your-admin-password"
  }'
```

## ?? Docker Build Details

### Multi-Stage Build
The Dockerfile uses a multi-stage build:

1. **Build Stage** - Uses `mcr.microsoft.com/dotnet/sdk:8.0`
   - Restores NuGet packages
   - Builds the application
   - Publishes release artifacts

2. **Runtime Stage** - Uses `mcr.microsoft.com/dotnet/aspnet:8.0`
   - Smaller runtime-only image
   - Non-root user for security
   - Only contains published artifacts

### Image Size Optimization
- Build artifacts excluded via `.dockerignore`
- Multi-stage build keeps final image small (~200MB)
- Only runtime dependencies included

### Security Features
- ? Non-root user (`appuser`)
- ? Minimal runtime image
- ? Health check configured
- ? No secrets in image

## ?? Local Docker Testing

### Build the Image
```bash
cd "Etali Data Collection Web API"
docker build -t etali-api:latest .
```

### Run Locally
```bash
docker run -d \
  --name etali-api \
  -p 8080:8080 \
  -e ConnectionStrings__EtaliDatabase="Host=host.docker.internal;Database=etali_db;Username=etali_user;Password=password" \
  -e Jwt__SecretKey="YourVeryLongSecretKeyHere1234567890ABCDEFGHIJKLMNOPQRSTUVWXYZ" \
  -e Jwt__Issuer="EtaliDataCollectionApi" \
  -e Jwt__Audience="EtaliDataCollectionApi" \
  -e Jwt__ExpirationInDays="7" \
  -e DefaultAdmin__Username="admin" \
  -e DefaultAdmin__Password="Admin123!" \
  -e MoxfieldApi__BaseUrl="https://api2.moxfield.com/v3/decks/all/" \
  -e AllowedOrigins="*" \
  -e ASPNETCORE_ENVIRONMENT="Production" \
  etali-api:latest
```

### Check Logs
```bash
docker logs etali-api
```

### Test Health
```bash
curl http://localhost:8080/health
```

### Stop and Remove
```bash
docker stop etali-api
docker rm etali-api
```

## ?? Docker Compose (Optional - For Local Development)

Create `docker-compose.yml` in the solution root:

```yaml
version: '3.8'

services:
  postgres:
    image: postgres:14
    container_name: etali-postgres
    environment:
      POSTGRES_DB: etali_db
      POSTGRES_USER: etali_user
      POSTGRES_PASSWORD: etali_pass
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U etali_user"]
      interval: 10s
      timeout: 5s
      retries: 5

  api:
    build:
      context: .
      dockerfile: Etali Data Collection Web API/Dockerfile
    container_name: etali-api
    ports:
      - "8080:8080"
    environment:
      - ConnectionStrings__EtaliDatabase=Host=postgres;Database=etali_db;Username=etali_user;Password=etali_pass
      - Jwt__SecretKey=YourVeryLongSecretKeyHere1234567890ABCDEFGHIJKLMNOPQRSTUVWXYZ
      - Jwt__Issuer=EtaliDataCollectionApi
      - Jwt__Audience=EtaliDataCollectionApi
      - Jwt__ExpirationInDays=7
      - DefaultAdmin__Username=admin
      - DefaultAdmin__Password=Admin123!
      - MoxfieldApi__BaseUrl=https://api2.moxfield.com/v3/decks/all/
      - AllowedOrigins=*
      - ASPNETCORE_ENVIRONMENT=Development
    depends_on:
      postgres:
        condition: service_healthy
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 30s
      timeout: 3s
      retries: 3

volumes:
  postgres_data:
```

**Usage:**
```bash
# Start everything
docker-compose up -d

# View logs
docker-compose logs -f api

# Stop everything
docker-compose down

# Clean up (including data)
docker-compose down -v
```

## ?? Troubleshooting

### Build Fails
**Issue**: Docker build fails with restore errors

**Solution**:
```bash
# Clear Docker cache and rebuild
docker build --no-cache -t etali-api:latest .
```

### Container Exits Immediately
**Issue**: Container starts then stops

**Solution**:
```bash
# Check logs
docker logs <container-id>

# Common causes:
# - Missing required environment variables
# - Database connection fails
# - JWT secret too short
```

### Database Connection Fails
**Issue**: Can't connect to PostgreSQL

**Solution**:
- Use **Internal Database URL** from Render (not External)
- Ensure both services are in the same region
- Check database is running in Render dashboard
- For local Docker: Use `host.docker.internal` instead of `localhost`

### Port Issues
**Issue**: Port 8080 not accessible

**Solution**:
- Render automatically handles port mapping
- Locally: Ensure `-p 8080:8080` is in docker run command
- Check firewall settings

### Health Check Fails
**Issue**: Render shows unhealthy service

**Solution**:
```bash
# Test health endpoint
curl https://your-app.onrender.com/health

# Check application logs in Render dashboard
# Verify database migrations completed
```

## ?? Performance Optimization

### Image Size
Current image size: ~200MB (optimized)

Further optimization:
```dockerfile
# Use Alpine-based images (smaller)
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS final
```

### Build Cache
Render caches Docker layers. Order in Dockerfile is optimized:
1. Copy .csproj first (changes less often)
2. Restore dependencies (cached unless .csproj changes)
3. Copy source code (changes frequently)

### Resource Limits
For Render, select instance type based on needs:
- **Starter**: 0.5 GB RAM (good for light traffic)
- **Standard**: 2 GB RAM (production)
- **Pro**: 4+ GB RAM (high traffic)

## ?? Security Best Practices

? **Non-Root User**
- Container runs as `appuser`, not root
- Limits damage if container is compromised

? **Minimal Runtime Image**
- Uses `aspnet` runtime (not full SDK)
- Smaller attack surface

? **No Secrets in Image**
- All secrets via environment variables
- Never hardcoded in Dockerfile

? **Health Checks**
- Render monitors application health
- Automatic restart on failures

? **HTTPS Enforcement**
- Render provides free SSL/TLS
- Production mode enforces HTTPS

## ?? Monitoring

### Render Dashboard
- CPU and memory usage graphs
- Request logs
- Deployment history
- Automatic health monitoring

### Application Logs
```bash
# View in Render dashboard
Logs tab ? Select time range

# Or use Render CLI
render logs -s your-service-name
```

### Metrics
- Response times
- Error rates
- Database query performance
- Health check status

## ?? Continuous Deployment

Render automatically deploys when you push to GitHub:

```bash
git add .
git commit -m "Update application"
git push origin master
```

Render will:
1. Detect the push
2. Pull latest code
3. Build new Docker image
4. Deploy with zero downtime
5. Roll back automatically if health checks fail

### Disable Auto-Deploy
In Render dashboard:
1. Go to service settings
2. Toggle **Auto-Deploy** off
3. Deploy manually via dashboard

## ?? Cost Estimate

| Component | Free | Starter | Standard |
|-----------|------|---------|----------|
| Web Service | Free* | $7/month | $25/month |
| PostgreSQL | Free** | $7/month | $20/month |
| **Total** | **Free** | **$14/month** | **$45/month** |

\* Spins down after inactivity  
\*\* 90 days, then expires

## ?? Additional Resources

- [Render Docker Docs](https://render.com/docs/docker)
- [.NET Docker Best Practices](https://docs.microsoft.com/dotnet/core/docker/build-container)
- [Dockerfile Reference](https://docs.docker.com/engine/reference/builder/)
- [Docker Compose Docs](https://docs.docker.com/compose/)

## ? Deployment Checklist

- [ ] Dockerfile created and tested locally
- [ ] .dockerignore configured
- [ ] Code pushed to GitHub
- [ ] PostgreSQL database created on Render
- [ ] Web service created with Docker runtime
- [ ] All environment variables set
- [ ] Health check path configured (`/health`)
- [ ] JWT secret is 64+ characters
- [ ] Strong admin password set
- [ ] CORS origins configured
- [ ] Database migrations completed
- [ ] Swagger UI accessible
- [ ] Authentication tested

---

**?? Your API is containerized and production-ready on Render!**

The Docker setup provides:
- Fast, consistent builds
- Easy scaling
- Zero-downtime deployments
- Automatic health monitoring
- Production-grade security
