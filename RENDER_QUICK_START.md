# ? Render Deployment Quick Start

Deploy your Etali Data Collection API to Render in 5 minutes!

## ? Option 1: Deploy with Blueprint (Recommended - Easiest)

The `render.yaml` file automates everything!

### Steps:

1. **Push to GitHub**
   ```bash
   git add .
   git commit -m "Add Render deployment configuration"
   git push origin master
   ```

2. **Deploy on Render**
   - Go to [Render Dashboard](https://dashboard.render.com/)
   - Click **New +** ? **Blueprint**
   - Select your GitHub repository
   - Click **Apply**
   - Render creates everything automatically:
     * PostgreSQL database
     * Web service with Docker
     * All environment variables
     * Health checks

3. **Get Your Admin Password**
   - After deployment, click on your web service
   - Go to **Logs** tab
   - Look for the line: `Default admin user created: admin`
   - **Save the password shown in the logs!**

4. **Access Your API**
   - Your API URL: `https://etali-data-collection-api.onrender.com`
   - Swagger UI: `https://etali-data-collection-api.onrender.com/`
   - Health check: `https://etali-data-collection-api.onrender.com/health`

### Customize Before Deploying:

Edit `render.yaml` to customize:
- **Region**: Change `oregon` to `frankfurt`, `singapore`, or `ohio`
- **Plan**: Change to `free` (testing) or `standard` (production)
- **Admin Username**: Change from `admin` to your preference
- **CORS**: Change from `*` to your frontend URL
- **Branch**: Change from `master` to your deployment branch

---

## ? Option 2: Manual Deployment (More Control)

### Step 1: Create PostgreSQL Database

1. Go to [Render Dashboard](https://dashboard.render.com/)
2. Click **New +** ? **PostgreSQL**
3. Configure:
   - **Name**: `etali-database`
   - **Database**: `etali_db`
   - **User**: `etali_user`
   - **Plan**: `Starter` (or `Free` for testing)
   - **Region**: `Oregon` (or closest to you)
4. Click **Create Database**
5. **Copy the Internal Database URL** - you'll need this!

### Step 2: Create Web Service

1. Click **New +** ? **Web Service**
2. Connect your GitHub repository
3. Configure:
   - **Name**: `etali-data-collection-api`
   - **Region**: Same as your database (important!)
   - **Branch**: `master`
   - **Runtime**: **Docker**
   - **Dockerfile Path**: `Etali Data Collection Web API/Dockerfile`
   - **Plan**: `Starter` (or `Free` for testing)

### Step 3: Add Environment Variables

Click **Advanced** and add these environment variables:

#### Required Variables:

```plaintext
ConnectionStrings__EtaliDatabase = <paste-internal-database-url-here>
Jwt__SecretKey = <generate-64-char-random-string>
Jwt__Issuer = EtaliDataCollectionApi
Jwt__Audience = EtaliDataCollectionApi
Jwt__ExpirationInDays = 7
DefaultAdmin__Username = admin
DefaultAdmin__Password = <your-strong-password>
MoxfieldApi__BaseUrl = https://api2.moxfield.com/v3/decks/all/
AllowedOrigins = *
ASPNETCORE_ENVIRONMENT = Production
```

**Generate JWT Secret** (PowerShell):
```powershell
-join ((65..90) + (97..122) + (48..57) | Get-Random -Count 64 | ForEach-Object {[char]$_})
```

**Admin Password Requirements:**
- At least 6 characters
- Must include: uppercase, lowercase, digit, and special character
- Example: `Admin123!@#`

### Step 4: Configure Health Check

In **Advanced** ? **Health Check Path**:
```
/health
```

### Step 5: Deploy

1. Click **Create Web Service**
2. Wait for build to complete (~3-5 minutes)
3. Check logs for any errors
4. Visit your API URL!

---

## ? Verify Deployment

### Test Health Endpoint
```bash
curl https://your-app.onrender.com/health
```
? Expected: `Healthy`

### View Swagger UI
Open in browser:
```
https://your-app.onrender.com/
```
? You should see the interactive API documentation

### Test Authentication
```bash
curl -X POST https://your-app.onrender.com/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "username": "admin",
    "password": "your-admin-password"
  }'
```
? You should receive a JWT token

---

## ?? Test Locally with Docker First

Before deploying to Render, test locally:

### Build Image
```bash
docker build -t etali-api:latest -f "Etali Data Collection Web API/Dockerfile" .
```
**Note:** Make sure you're in the repository root directory when running this command.

### Run Container
```bash
docker run -d \
  --name etali-test \
  -p 8080:8080 \
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

### Test Locally
```bash
# Health check
curl http://localhost:8080/health

# Swagger UI (Windows)
start http://localhost:8080

# Swagger UI (Mac/Linux)
open http://localhost:8080  # Mac
xdg-open http://localhost:8080  # Linux

# View logs
docker logs etali-test -f
```

### Clean Up
```bash
docker stop etali-test
docker rm etali-test
```

---

## ? Use Docker Compose (Local Development)

For local development with PostgreSQL:

```bash
# Start everything
docker-compose up -d

# View logs
docker-compose logs -f api

# Test API
curl http://localhost:8080/health

# Stop everything
docker-compose down

# Clean up (including database)
docker-compose down -v
```

---

## ?? Common Issues & Solutions

### "failed to read dockerfile: open Dockerfile: no such file or directory"
- ? Make sure you're in the **repository root directory** when running Docker commands
- ? The correct build command is: `docker build -t etali-api:latest -f "Etali Data Collection Web API/Dockerfile" .`
- ? Note the `.` at the end - this specifies the build context
- ? The `-f` flag specifies the Dockerfile location relative to the build context

### "Connection refused" or Database Errors
- ? Make sure you used the **Internal Database URL** (not External)
- ? Verify both services are in the **same region**
- ? Check database is running in Render dashboard

### "JWT token validation failed"
- ? Ensure `Jwt__SecretKey` is at least 32 characters (64+ recommended)
- ? Verify `Jwt__Issuer` and `Jwt__Audience` match exactly
- ? Check for extra spaces or line breaks in secret key

### "401 Unauthorized"
- ? Login first to get a token
- ? Include header: `Authorization: Bearer <your-token>`
- ? Check token hasn't expired

### Build Fails
- ? Verify Dockerfile path is correct: `Etali Data Collection Web API/Dockerfile`
- ? Check that all files are pushed to GitHub
- ? Review build logs in Render dashboard

### Service Spins Down (Free Tier)
- ? This is normal for free tier
- ? First request after inactivity will take longer (cold start)
- ? Upgrade to Starter plan for always-on service

### CORS Errors
- ? For development: Use `AllowedOrigins = *`
- ? For production: Set to your frontend domain (no trailing slash)
- ? Multiple origins: `https://app1.com,https://app2.com`

---

## ? Production Checklist

Before going live:

- [ ] Use **Starter** or **Standard** plan (not Free)
- [ ] Set strong, unique JWT secret (64+ characters)
- [ ] Use strong admin password
- [ ] Restrict CORS to your frontend domain(s)
- [ ] Use same region for database and web service
- [ ] Enable health checks
- [ ] Set up monitoring alerts in Render
- [ ] Test all endpoints with authentication
- [ ] Verify database migrations completed
- [ ] Set `ASPNETCORE_ENVIRONMENT=Production`

---

## ? Continuous Deployment

Render automatically deploys when you push to GitHub:

```bash
# Make changes
git add .
git commit -m "Update feature"
git push origin master
```

Render will:
1. Detect the push
2. Build new Docker image
3. Run tests (if configured)
4. Deploy with zero downtime
5. Rollback automatically if health checks fail

---

## ? Cost Estimate

| Plan | Web Service | PostgreSQL | Total |
|------|-------------|------------|-------|
| **Free** (testing) | Free* | Free** | Free |
| **Starter** (small production) | $7/mo | $7/mo | $14/mo |
| **Standard** (production) | $25/mo | $20/mo | $45/mo |

\* Spins down after 15 minutes of inactivity  
\*\* Limited to 90 days, 1GB storage

---

## ? Next Steps

1. **Deploy**: Use Option 1 (Blueprint) or Option 2 (Manual)
2. **Test**: Verify all endpoints work
3. **Monitor**: Check logs and metrics in Render dashboard
4. **Secure**: Set proper CORS and strong passwords
5. **Scale**: Upgrade to paid plans as needed

---

## ? Need Help?

- **Full Docs**: See `DOCKER_DEPLOYMENT_RENDER.md` for detailed guide
- **Render Docs**: https://render.com/docs/docker
- **Troubleshooting**: See "Common Issues" above
- **Render Support**: https://render.com/support

---

**?? Your API is ready to deploy!**

Choose Option 1 for fastest deployment, or Option 2 for more control.
