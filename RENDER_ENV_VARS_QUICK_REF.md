# Render Environment Variables - Quick Reference Card

Copy-paste this into your Render environment variables setup (one per line):

## Required Environment Variables for Render

```
ConnectionStrings__EtaliDatabase
Jwt__SecretKey
Jwt__Issuer
Jwt__Audience
Jwt__ExpirationInDays
DefaultAdmin__Username
DefaultAdmin__Password
MoxfieldApi__BaseUrl
AllowedOrigins
ASPNETCORE_ENVIRONMENT
```

## Example Values (Update with your actual values!)

```plaintext
# Database (Get from Render PostgreSQL dashboard)
ConnectionStrings__EtaliDatabase = postgresql://user:pass@host:port/database

# JWT Configuration (Generate a strong random key!)
Jwt__SecretKey = YourVerySecureRandomString1234567890ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghij
Jwt__Issuer = EtaliDataCollectionApi
Jwt__Audience = EtaliDataCollectionApi
Jwt__ExpirationInDays = 7

# Admin Account (Use strong password!)
DefaultAdmin__Username = admin
DefaultAdmin__Password = YourSecureAdminPassword123!@#

# Moxfield API
Moxfield Api__BaseUrl = https://api2.moxfield.com/v3/decks/all/
MoxfieldApi__ApiKey = (Optional - leave empty if you don't have one)

# CORS (Use specific domains in production)
AllowedOrigins = *
# Production example: https://yourdomain.com,https://www.yourdomain.com

# Environment
ASPNETCORE_ENVIRONMENT = Production
```

## ?? Security Tips

### Generate Strong JWT Secret (PowerShell):
```powershell
-join ((65..90) + (97..122) + (48..57) | Get-Random -Count 64 | ForEach-Object {[char]$_})
```

### Password Requirements:
- Minimum 6 characters
- At least one uppercase letter
- At least one lowercase letter
- At least one digit
- At least one special character

### Recommended Values:
- **JWT Secret**: 64+ characters, random
- **Admin Password**: 12+ characters, mixed case, numbers, symbols
- **AllowedOrigins**: Specific domains in production (not `*`)

## ?? Render Build Configuration

### Build Command:
```bash
dotnet publish "Etali Data Collection Web API/Etali Data Collection Web API.csproj" -c Release -o out
```

### Start Command:
```bash
cd out && dotnet "Etali Data Collection Web API.dll"
```

### Health Check Path:
```
/health
```

## ?? Deployment Checklist

- [ ] PostgreSQL database created on Render
- [ ] Internal Database URL copied
- [ ] All environment variables configured
- [ ] Strong JWT secret generated (64+ chars)
- [ ] Strong admin password set
- [ ] CORS origins configured for production
- [ ] Build and start commands configured
- [ ] Health check path set to `/health`

## ?? After Deployment

Test these endpoints:
- `https://your-app.onrender.com/health` ? Should return "Healthy"
- `https://your-app.onrender.com/` ? Should show Swagger UI
- `https://your-app.onrender.com/auth/login` ? Should accept login credentials

## ?? Troubleshooting

### If deployment fails:
1. Check Render logs
2. Verify all environment variables are set
3. Ensure database URL is the **Internal** URL (not External)
4. Verify JWT secret is at least 32 characters

### If database connection fails:
1. Use Internal Database URL (starts with `postgresql://`)
2. Ensure database and web service are in same region
3. Check database is running in Render dashboard

### If authentication fails:
1. Verify JWT secret length (32+ characters)
2. Check admin credentials are set
3. Ensure Jwt__Issuer and Jwt__Audience match

## ?? Full Documentation

See `RENDER_DEPLOYMENT.md` for complete deployment guide with detailed troubleshooting.

---

**Pro Tip:** Save your environment variable values in a secure password manager after setup!
