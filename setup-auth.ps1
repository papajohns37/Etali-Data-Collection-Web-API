# Setup Script for Authentication
# Run this script to configure JWT secrets and default admin account

Write-Host "=== Etali Data Collection API - Authentication Setup ===" -ForegroundColor Cyan
Write-Host ""

# Change to the API project directory
$projectPath = "Etali Data Collection Web API"
if (!(Test-Path $projectPath)) {
    Write-Host "Error: Project directory not found at: $projectPath" -ForegroundColor Red
    exit 1
}

Set-Location $projectPath

Write-Host "Step 1: Generating secure JWT secret key..." -ForegroundColor Yellow
# Generate a secure random 64-character alphanumeric string
$key = -join ((65..90) + (97..122) + (48..57) | Get-Random -Count 64 | ForEach-Object {[char]$_})
dotnet user-secrets set "Jwt:SecretKey" $key
Write-Host "? JWT secret key generated and stored" -ForegroundColor Green
Write-Host ""

Write-Host "Step 2: Setting up default admin account..." -ForegroundColor Yellow
Write-Host "Please provide admin credentials for the default administrator account."
Write-Host ""

# Get admin email
do {
    $adminEmail = Read-Host "Admin Email Address"
    if ($adminEmail -notmatch "^[^@]+@[^@]+\.[^@]+$") {
        Write-Host "Invalid email format. Please enter a valid email address." -ForegroundColor Red
    }
} while ($adminEmail -notmatch "^[^@]+@[^@]+\.[^@]+$")

dotnet user-secrets set "DefaultAdmin:Email" $adminEmail
Write-Host "? Admin email set to: $adminEmail" -ForegroundColor Green
Write-Host ""

# Get admin password
$passwordValid = $false
do {
    $adminPassword = Read-Host "Admin Password" -AsSecureString
    $adminPasswordConfirm = Read-Host "Confirm Password" -AsSecureString
    
    # Convert SecureString to plain text for comparison
    $passwordText = [Runtime.InteropServices.Marshal]::PtrToStringAuto(
        [Runtime.InteropServices.Marshal]::SecureStringToBSTR($adminPassword))
    $passwordConfirmText = [Runtime.InteropServices.Marshal]::PtrToStringAuto(
        [Runtime.InteropServices.Marshal]::SecureStringToBSTR($adminPasswordConfirm))
    
    if ($passwordText -ne $passwordConfirmText) {
        Write-Host "Passwords do not match. Please try again." -ForegroundColor Red
        Write-Host ""
    }
    else {
        $passwordValid = $true
        dotnet user-secrets set "DefaultAdmin:Password" $passwordText
        Write-Host "? Admin password set successfully" -ForegroundColor Green
    }
    
    # Clear password from memory
    $passwordText = $null
    $passwordConfirmText = $null
    
} while (!$passwordValid)

Write-Host ""
Write-Host "=== Setup Complete! ===" -ForegroundColor Green
Write-Host ""
Write-Host "Your authentication configuration is now complete." -ForegroundColor Cyan
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  1. Run the application: dotnet run" -ForegroundColor Gray
Write-Host "  2. Navigate to: http://localhost:5000" -ForegroundColor Gray
Write-Host "  3. Login with your admin credentials in Swagger UI" -ForegroundColor Gray
Write-Host "  4. Create additional users via /auth/register endpoint" -ForegroundColor Gray
Write-Host ""
Write-Host "Your secrets are stored securely in:" -ForegroundColor Cyan
Write-Host "  $env:APPDATA\Microsoft\UserSecrets\19c29f11-363a-4cab-9889-896ff6a76452" -ForegroundColor Gray
Write-Host ""
Write-Host "To view your secrets: dotnet user-secrets list" -ForegroundColor Gray
Write-Host "To remove a secret: dotnet user-secrets remove ""SecretName""" -ForegroundColor Gray
Write-Host ""

# Return to original directory
Set-Location ..
