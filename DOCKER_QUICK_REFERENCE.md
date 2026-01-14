# ?? Quick Command Reference

## Prerequisites
- Docker Desktop installed and running
- Git repository cloned
- Terminal/PowerShell open in repository root

---

## ? Verify You're in the Right Directory

```powershell
# Check current directory
pwd

# Should show something like:
# C:\Users\John\source\repos\Etali Data Collection Web API
```

If not in the right directory:
```powershell
cd "C:\Users\John\source\repos\Etali Data Collection Web API"
```

---

## ?? Docker Commands (Run from Repository Root)

### Option 1: Use PowerShell Script (Easiest)
```powershell
.\test-docker.ps1
```
This script automatically:
- Builds the Docker image
- Runs the container
- Tests the health endpoint
- Shows you the Swagger UI URL

### Option 2: Use Docker Compose
```bash
# Start everything (API + PostgreSQL)
docker-compose up -d

# View logs
docker-compose logs -f api

# Stop everything
docker-compose down

# Clean up (including database)
docker-compose down -v
```

### Option 3: Manual Docker Commands
```bash
# Build image
docker build -t etali-api:latest -f "Etali Data Collection Web API/Dockerfile" .

# Run container
docker run -d --name etali-test -p 8080:8080 \
  -e ConnectionStrings__EtaliDatabase="Host=host.docker.internal;Database=etali_db;Username=postgres;Password=postgres" \
  -e Jwt__SecretKey="TestSecretKeyForLocalDevelopment1234567890ABCDEFGHIJKLMNOPQRSTUVWXYZ" \
  -e Jwt__Issuer="EtaliDataCollectionApi" \
  -e Jwt__Audience="EtaliDataCollectionApi" \
  -e Jwt__ExpirationInDays="7" \
  -e DefaultAdmin__Username="admin" \
  -e DefaultAdmin__Password="Admin123!" \
  -e MoxfieldApi__BaseUrl="https://api2.moxfield.com/v3/decks/all/" \
  -e AllowedOrigins="*" \
  -e ASPNETCORE_ENVIRONMENT="Production" \
  etali-api:latest

# View logs
docker logs etali-test -f

# Stop and remove
docker stop etali-test
docker rm etali-test
```

---

## ?? Access Your API

After starting the container:

- **Swagger UI**: http://localhost:8080
- **Health Check**: http://localhost:8080/health
- **API Base**: http://localhost:8080/api

---

## ?? Deploy to Render

### Option 1: Blueprint (Recommended)
```bash
# 1. Commit and push to GitHub
git add .
git commit -m "Add Docker containerization"
git push origin master

# 2. Go to Render Dashboard
# https://dashboard.render.com/

# 3. Click: New + ? Blueprint
# 4. Select your repository
# 5. Click Apply
# 6. Done! ?
```

### Option 2: Manual Deployment
See `RENDER_QUICK_START.md` for step-by-step instructions.

---

## ?? Troubleshooting

### "failed to read dockerfile" Error
- ? Make sure you're in the repository root: `pwd`
- ? Use the correct command: `docker build -t etali-api:latest -f "Etali Data Collection Web API/Dockerfile" .`
- ? See `DOCKER_TROUBLESHOOTING.md` for detailed help

### "Cannot connect to Docker daemon"
- ? Start Docker Desktop
- ? Wait for it to finish starting
- ? Verify: `docker --version`

### Other Issues
See `DOCKER_TROUBLESHOOTING.md` for comprehensive troubleshooting guide.

---

## ?? Documentation Files

| File | Purpose |
|------|---------|
| `RENDER_QUICK_START.md` | 5-minute deployment guide |
| `DOCKER_TROUBLESHOOTING.md` | Common Docker issues |
| `DOCKER_DEPLOYMENT_RENDER.md` | Comprehensive Docker guide |
| `RENDER_DEPLOYMENT.md` | Detailed Render setup |
| `DOCKER_SETUP_COMPLETE.md` | Complete overview |
| `LOCAL_DEVELOPMENT.md` | Local development guide |

---

## ?? Tips

1. **Always run Docker commands from repository root**
2. **Use `.\test-docker.ps1` for quickest local testing**
3. **Use `docker-compose` for development with PostgreSQL**
4. **Use Blueprint deployment for easiest Render setup**
5. **Check `DOCKER_TROUBLESHOOTING.md` if you get errors**

---

## ? Common Workflows

### First Time Setup (Local)
```powershell
# 1. Navigate to repository root
cd "C:\Users\John\source\repos\Etali Data Collection Web API"

# 2. Test with Docker
.\test-docker.ps1

# 3. Open Swagger UI
start http://localhost:8080
```

### First Time Setup (Render)
```bash
# 1. Commit and push
git add .
git commit -m "Ready for deployment"
git push origin master

# 2. Deploy with Blueprint
# Go to: https://dashboard.render.com/
# Click: New + ? Blueprint
# Select repository ? Apply
```

### Development Workflow
```bash
# 1. Make code changes
# 2. Test locally
docker-compose up -d

# 3. Commit changes
git add .
git commit -m "Update feature"

# 4. Push to GitHub
git push origin master

# 5. Render auto-deploys!
```

---

**Need help? Check `DOCKER_TROUBLESHOOTING.md` or `RENDER_QUICK_START.md`**
