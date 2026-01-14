# Docker Build Troubleshooting Guide

## ? Error: "failed to read dockerfile: open Dockerfile: no such file or directory"

### Problem
You're trying to build the Docker image but getting an error that the Dockerfile can't be found.

### Solution

#### 1. Check Your Current Directory
Make sure you're in the **repository root directory**:

```bash
# Windows PowerShell
pwd

# You should see something like:
# Path
# ----
# C:\Users\YourName\source\repos\Etali Data Collection Web API
```

If you're in the wrong directory, navigate to the repository root:

```bash
# Windows PowerShell
cd "C:\Users\John\source\repos\Etali Data Collection Web API"

# Or if you're in a subdirectory
cd ..
```

#### 2. Use the Correct Build Command
The Dockerfile is located at: `Etali Data Collection Web API/Dockerfile`

**Correct command from repository root:**
```bash
docker build -t etali-api:latest -f "Etali Data Collection Web API/Dockerfile" .
```

**Breakdown:**
- `docker build` - Build command
- `-t etali-api:latest` - Tag the image
- `-f "Etali Data Collection Web API/Dockerfile"` - Path to Dockerfile (in quotes because of spaces)
- `.` - Build context (current directory = repository root)

#### 3. Verify File Exists
Check that the Dockerfile exists:

```powershell
# Windows PowerShell
Test-Path "Etali Data Collection Web API\Dockerfile"

# Should return: True
```

```bash
# Mac/Linux
ls -la "Etali Data Collection Web API/Dockerfile"
```

### Why This Happens

The Dockerfile is in a subdirectory called `Etali Data Collection Web API/`, but it needs to access files from the entire repository during the build process. Therefore:

1. The **build context** (`.`) is the repository root
2. The **Dockerfile path** (`-f`) points to the subdirectory
3. Inside the Dockerfile, paths are relative to the build context

### Directory Structure
```
Etali Data Collection Web API/          ? Repository root (run commands here)
?
??? Etali Data Collection Web API/      ? Project subdirectory
?   ??? Dockerfile                      ? Dockerfile location
?   ??? .dockerignore
?   ??? Program.cs
?   ??? ...
?
??? docker-compose.yml
??? render.yaml
??? ...
```

---

## Other Common Docker Issues

### Issue: "COPY failed: no source files were specified"

**Solution:** This usually means you're running the docker build command from the wrong directory.

```bash
# Wrong (from project subdirectory)
cd "Etali Data Collection Web API"
docker build -t etali-api:latest .

# Correct (from repository root)
cd ..
docker build -t etali-api:latest -f "Etali Data Collection Web API/Dockerfile" .
```

### Issue: Docker build is very slow

**Solution:** Make sure `.dockerignore` is in place and working.

```bash
# Verify .dockerignore exists
Test-Path "Etali Data Collection Web API\.dockerignore"

# Check Docker build context size
docker build -t etali-api:latest -f "Etali Data Collection Web API/Dockerfile" . --no-cache
```

The `.dockerignore` file should exclude:
- `bin/`
- `obj/`
- `.vs/`
- `.git/`
- Test projects

### Issue: "docker: command not found"

**Solution:** Docker is not installed or not in PATH.

1. Install Docker Desktop: https://www.docker.com/products/docker-desktop
2. Restart your terminal after installation
3. Verify installation: `docker --version`

### Issue: "Cannot connect to Docker daemon"

**Solution:** Docker Desktop is not running.

1. Start Docker Desktop application
2. Wait for it to finish starting (icon will be solid in system tray)
3. Try your command again

---

## Quick Reference: Correct Docker Commands

### Build Image (from repository root)
```bash
docker build -t etali-api:latest -f "Etali Data Collection Web API/Dockerfile" .
```

### Run Container
```bash
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
```

### View Logs
```bash
docker logs etali-test -f
```

### Stop and Remove Container
```bash
docker stop etali-test
docker rm etali-test
```

### Use Docker Compose (easier!)
```bash
# From repository root
docker-compose up -d
docker-compose logs -f api
docker-compose down
```

### Use PowerShell Test Script (easiest!)
```powershell
# From repository root
.\test-docker.ps1
```

---

## Still Having Issues?

1. **Check Docker is running:** `docker --version`
2. **Verify you're in repository root:** `pwd`
3. **Check Dockerfile exists:** `Test-Path "Etali Data Collection Web API\Dockerfile"`
4. **Try Docker Compose instead:** `docker-compose up -d`
5. **Use the PowerShell script:** `.\test-docker.ps1`

If you're still stuck, please check:
- Docker Desktop is installed and running
- You have the latest code from GitHub
- Your antivirus isn't blocking Docker
- You have enough disk space (Docker images ~200MB)
