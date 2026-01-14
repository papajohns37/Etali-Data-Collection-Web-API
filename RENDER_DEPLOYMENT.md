# Deploying Etali Data Collection API to Render

This guide will help you deploy the Etali Data Collection API to Render with secure configuration management.

## Prerequisites

1. A [Render](https://render.com/) account
2. A PostgreSQL database (can be created on Render)
3. Your API code pushed to a Git repository (GitHub, GitLab, or Bitbucket)

## Step 1: Create a PostgreSQL Database on Render

1. Log in to your Render dashboard
2. Click **New +** ? **PostgreSQL**
3. Configure your database:
   - **Name**: `etali-database` (or your preferred name)
   - **Database**: `etali_db`
   - **User**: `etali_user`
   - **Region**: Choose closest to your users
   - **Plan**: Choose based on your needs (Free tier available for testing)
4. Click **Create Database**
5. **IMPORTANT**: Copy the **Internal Database URL** (starts with `postgresql://`)
   - This will be used as your connection string
   - Format: `postgresql://user:password@host:port/database`

## Step 2: Create a Web Service on Render

1. In Render dashboard, click **New +** ? **Web Service**
2. Connect your Git repository
3. Configure the service:

### Basic Settings
- **Name**: `etali-data-collection-api` (or your preferred name)
- **Region**: Same as your database for best performance
- **Branch**: `master` or your deployment branch
- **Root Directory**: Leave empty (or specify if API is in subdirectory)
- **Runtime**: `.NET`
- **Build Command**: 
  ```bash
  dotnet publish "Etali Data Collection Web API/Etali Data Collection Web API.csproj" -c Release -o out
  ```
- **Start Command**:
  ```bash
  cd out && dotnet "Etali Data Collection Web API.dll"
  ```

### Instance Type
- Choose based on your needs (Free tier available for testing)
- For production, consider **Starter** or higher for better performance

## Step 3: Configure Environment Variables (Secrets)

In the **Environment Variables** section of your Render web service, add the following:

### Required Secrets

#### 1. Database Connection String
```
ConnectionStrings__EtaliDatabase
```
**Value**: Paste your PostgreSQL **Internal Database URL** from Step 1
Example: `postgresql://etali_user:password123@dpg-xxxxx-a.oregon-postgres.render.com/etali_db`

#### 2. JWT Secret Key
```
Jwt__SecretKey
```
**Value**: Generate a strong secret key (minimum 32 characters)
You can generate one with:
```bash
# PowerShell
-join ((65..90) + (97..122) + (48..57) | Get-Random -Count 64 | ForEach-Object {[char]$_})

# Or online at: https://generate.plus/en/base64
```
Example: `YourVerySecureRandomString1234567890ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghij`

#### 3. JWT Issuer
```
Jwt__Issuer
```
**Value**: `EtaliDataCollectionApi`

#### 4. JWT Audience
```
Jwt__Audience
```
**Value**: `EtaliDataCollectionApi`

#### 5. JWT Expiration
```
Jwt__ExpirationInDays
```
**Value**: `7`

#### 6. Admin Username
```
DefaultAdmin__Username
```
**Value**: Your desired admin username (e.g., `admin`)

#### 7. Admin Password
```
DefaultAdmin__Password
```
**Value**: Strong password for admin account
Must meet requirements: 
- At least 6 characters
- Contains uppercase, lowercase, digit, and special character
Example: `Admin123!@#`

#### 8. Moxfield API Key (Optional)
```
MoxfieldApi__ApiKey
```
**Value**: Your Moxfield API key if you have one
Format: `MoxKey; username passkey`
If you don't have one, leave empty (will use default)

#### 9. Moxfield Base URL
```
MoxfieldApi__BaseUrl
```
**Value**: `https://api2.moxfield.com/v3/decks/all/`

#### 10. ASPNETCORE Environment
```
ASPNETCORE_ENVIRONMENT
```
**Value**: `Production`

#### 11. Allowed Origins (CORS)
```
AllowedOrigins
```
**Value**: 
- For testing: `*` (allow all)
- For production: Your frontend URL(s) separated by commas
  Example: `https://yourfrontend.com,https://www.yourfrontend.com`

### Environment Variable Naming Convention

Render uses **double underscores (`__`)** to represent nested configuration sections.
For example:
- `ConnectionStrings__EtaliDatabase` becomes `ConnectionStrings:EtaliDatabase`
- `Jwt__SecretKey` becomes `Jwt:SecretKey`
- `DefaultAdmin__Username` becomes `DefaultAdmin:Username`

## Step 4: Configure Health Checks (Optional but Recommended)

Render can monitor your service's health:

1. In your web service settings, scroll to **Health Check Path**
2. Set to: `/health`
3. Render will ping this endpoint to ensure your service is running

## Step 5: Deploy

1. Click **Create Web Service**
2. Render will automatically:
   - Clone your repository
   - Run the build command
   - Start your application
   - Apply database migrations (AutoMigrate is enabled in Production)

3. Monitor the deployment logs in real-time
4. Once deployed, your API will be available at:
   `https://your-service-name.onrender.com`

## Step 6: Verify Deployment

### Test Health Endpoint
```bash
curl https://your-service-name.onrender.com/health
```
Should return: `Healthy`

### Test API Documentation (Swagger)
Visit in browser:
```
https://your-service-name.onrender.com/
```
You should see the Swagger UI with your API documentation.

### Test Authentication
```bash
curl -X POST https://your-service-name.onrender.com/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "username": "your-admin-username",
    "password": "your-admin-password"
  }'
```
Should return a JWT token.

## Step 7: Database Migrations

The application is configured to automatically run migrations on startup in Production (`AutoMigrate: true`).

If you prefer manual migrations:
1. Set `Database__AutoMigrate` environment variable to `false`
2. Connect to your Render shell and run:
   ```bash
   dotnet ef database update
   ```

## Security Best Practices

### ? DO:
- Use strong, unique passwords for admin accounts
- Generate a long, random JWT secret key (64+ characters recommended)
- Restrict CORS to specific domains in production
- Regularly rotate sensitive credentials
- Use Render's built-in secret management (never commit secrets to Git)
- Enable HTTPS (Render provides this automatically)
- Monitor logs for suspicious activity

### ? DON'T:
- Never commit secrets to Git
- Don't use weak passwords
- Don't use `AllowedOrigins: "*"` in production
- Don't disable SSL/HTTPS validation
- Don't share admin credentials

## Monitoring and Logs

### View Logs
1. Go to your web service in Render
2. Click **Logs** tab
3. View real-time application logs

### Set Up Alerts
1. Go to **Settings** ? **Notifications**
2. Configure email/Slack alerts for:
   - Deploy failures
   - Service health issues
   - High error rates

## Updating Environment Variables

To update secrets after deployment:
1. Go to your web service **Environment** tab
2. Update the variable value
3. Click **Save Changes**
4. Render will automatically redeploy your service

## Troubleshooting

### Issue: Database Connection Fails
**Solution**: 
- Verify `ConnectionStrings__EtaliDatabase` is set correctly
- Use the **Internal Database URL** (not external)
- Ensure web service and database are in the same region

### Issue: Migrations Don't Run
**Solution**:
- Check logs for migration errors
- Verify `Database__AutoMigrate` is `true`
- Manually run migrations from Render shell if needed

### Issue: JWT Token Validation Fails
**Solution**:
- Verify `Jwt__SecretKey` is at least 32 characters
- Ensure `Jwt__Issuer` and `Jwt__Audience` match configuration
- Check that secret key didn't get truncated

### Issue: 401 Unauthorized on Protected Endpoints
**Solution**:
- Login to get a fresh token
- Include `Authorization: Bearer <token>` header
- Verify token hasn't expired (check `Jwt__ExpirationInDays`)

### Issue: CORS Errors
**Solution**:
- Update `AllowedOrigins` to include your frontend domain
- Format: `https://yourdomain.com` (no trailing slash)
- Multiple origins: `https://domain1.com,https://domain2.com`

## Scaling and Performance

### Vertical Scaling
Upgrade your Render instance type for more CPU/memory:
- **Starter**: 0.5 GB RAM, suitable for light traffic
- **Standard**: 2 GB RAM, good for moderate traffic
- **Pro**: 4+ GB RAM, for high traffic

### Database Scaling
- Start with **Free** tier for testing
- Upgrade to **Starter** (1 GB storage) for production
- Consider **Standard** or higher for high-volume applications

### Connection Pooling
The application is configured with:
- Default pool size: 100 connections
- Automatic retry on transient failures
- 30-second command timeout

Adjust `Database__PoolSize` if needed for your workload.

## Continuous Deployment

Render automatically deploys when you push to your configured branch:
1. Push changes to your Git repository
2. Render detects the change
3. Automatically builds and deploys
4. Zero-downtime deployment

To disable auto-deploy:
1. Go to **Settings** ? **Auto-Deploy**
2. Toggle off

## Cost Estimates (as of 2024)

### Free Tier (Good for Testing)
- Web Service: Free (spins down after inactivity)
- PostgreSQL: Free (90 days, 1 GB storage)

### Production Starter
- Web Service Starter: ~$7/month
- PostgreSQL Starter: ~$7/month
- **Total**: ~$14/month

### Production Standard
- Web Service Standard: ~$25/month
- PostgreSQL Standard: ~$20/month
- **Total**: ~$45/month

## Additional Resources

- [Render Documentation](https://render.com/docs)
- [ASP.NET Core Deployment](https://docs.microsoft.com/aspnet/core/host-and-deploy/)
- [PostgreSQL on Render](https://render.com/docs/databases)
- [Environment Variables](https://render.com/docs/environment-variables)

## Support

If you encounter issues:
1. Check Render logs first
2. Review this guide's troubleshooting section
3. Check Render's status page: https://status.render.com/
4. Contact Render support: https://render.com/support
