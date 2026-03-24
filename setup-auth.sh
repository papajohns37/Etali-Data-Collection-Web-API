#!/bin/bash

# Setup Script for Authentication (Linux/Mac)
# Run this script to configure JWT secrets and default admin account

echo "=== Etali Data Collection API - Authentication Setup ==="
echo ""

# Change to the API project directory
PROJECT_PATH="Etali Data Collection Web API"
if [ ! -d "$PROJECT_PATH" ]; then
    echo "Error: Project directory not found at: $PROJECT_PATH"
    exit 1
fi

cd "$PROJECT_PATH"

echo "Step 1: Generating secure JWT secret key..."
# Generate a secure random key using openssl
KEY=$(openssl rand -base64 48)
dotnet user-secrets set "Jwt:SecretKey" "$KEY"
echo "? JWT secret key generated and stored"
echo ""

echo "Step 2: Setting up default admin account..."
echo "Please provide admin credentials for the default administrator account."
echo ""

# Get admin email
while true; do
    read -p "Admin Email Address: " ADMIN_EMAIL
    if [[ "$ADMIN_EMAIL" =~ ^[^@]+@[^@]+\.[^@]+$ ]]; then
        break
    else
        echo "Invalid email format. Please enter a valid email address."
    fi
done

dotnet user-secrets set "DefaultAdmin:Email" "$ADMIN_EMAIL"
echo "? Admin email set to: $ADMIN_EMAIL"
echo ""

# Get admin password
while true; do
    read -s -p "Admin Password: " ADMIN_PASSWORD
    echo ""
    read -s -p "Confirm Password: " ADMIN_PASSWORD_CONFIRM
    echo ""
    
    if [ "$ADMIN_PASSWORD" != "$ADMIN_PASSWORD_CONFIRM" ]; then
        echo "Passwords do not match. Please try again."
        echo ""
        continue
    fi
    
    # All validations passed
    dotnet user-secrets set "DefaultAdmin:Password" "$ADMIN_PASSWORD"
    echo "? Admin password set successfully"
    break
done

echo ""
echo "=== Setup Complete! ==="
echo ""
echo "Your authentication configuration is now complete."
echo ""
echo "Next steps:"
echo "  1. Run the application: dotnet run"
echo "  2. Navigate to: http://localhost:5000"
echo "  3. Login with your admin credentials in Swagger UI"
echo "  4. Create additional users via /auth/register endpoint"
echo ""
echo "Your secrets are stored securely in:"
echo "  ~/.microsoft/usersecrets/19c29f11-363a-4cab-9889-896ff6a76452"
echo ""
echo "To view your secrets: dotnet user-secrets list"
echo "To remove a secret: dotnet user-secrets remove \"SecretName\""
echo ""

# Return to original directory
cd ..
