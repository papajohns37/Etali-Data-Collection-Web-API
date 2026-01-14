# Docker Test Script for Etali Data Collection API
# This script helps you build and test the Docker container locally

Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "Etali API - Docker Build & Test" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

# Configuration
$ImageName = "etali-api"
$ImageTag = "latest"
$ContainerName = "etali-api-test"
$Port = "8080"

# Step 1: Build Docker Image
Write-Host "[1/4] Building Docker image..." -ForegroundColor Yellow
docker build -t "${ImageName}:${ImageTag}" -f "Etali Data Collection Web API/Dockerfile" .

if ($LASTEXITCODE -ne 0) {
    Write-Host "? Docker build failed!" -ForegroundColor Red
    exit 1
}

Write-Host "? Docker image built successfully!" -ForegroundColor Green
Write-Host ""

# Step 2: Stop and remove existing container if it exists
Write-Host "[2/4] Cleaning up existing container..." -ForegroundColor Yellow
docker stop $ContainerName 2>$null
docker rm $ContainerName 2>$null
Write-Host "? Cleanup complete!" -ForegroundColor Green
Write-Host ""

# Step 3: Generate a test JWT secret
Write-Host "[3/4] Generating test JWT secret..." -ForegroundColor Yellow
$JwtSecret = -join ((65..90) + (97..122) + (48..57) | Get-Random -Count 64 | ForEach-Object {[char]$_})
Write-Host "? JWT Secret generated!" -ForegroundColor Green
Write-Host ""

# Step 4: Run the container
Write-Host "[4/4] Starting container..." -ForegroundColor Yellow
Write-Host "Container name: $ContainerName" -ForegroundColor Cyan
Write-Host "Port: $Port" -ForegroundColor Cyan
Write-Host ""

docker run -d `
  --name $ContainerName `
  -p "${Port}:8080" `
  -e ConnectionStrings__EtaliDatabase="Host=host.docker.internal;Database=etali_db;Username=postgres;Password=postgres" `
  -e Jwt__SecretKey="$JwtSecret" `
  -e Jwt__Issuer="EtaliDataCollectionApi" `
  -e Jwt__Audience="EtaliDataCollectionApi" `
  -e Jwt__ExpirationInDays="7" `
  -e DefaultAdmin__Username="admin" `
  -e DefaultAdmin__Password="Admin123!" `
  -e MoxfieldApi__BaseUrl="https://api2.moxfield.com/v3/decks/all/" `
  -e MoxfieldApi__ApiKey="" `
  -e AllowedOrigins="*" `
  -e ASPNETCORE_ENVIRONMENT="Development" `
  "${ImageName}:${ImageTag}"

if ($LASTEXITCODE -ne 0) {
    Write-Host "? Container failed to start!" -ForegroundColor Red
    exit 1
}

Write-Host "? Container started successfully!" -ForegroundColor Green
Write-Host ""

# Wait for container to be ready
Write-Host "Waiting for API to be ready..." -ForegroundColor Yellow
Start-Sleep -Seconds 5

# Show container logs
Write-Host ""
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "Container Logs (last 20 lines):" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
docker logs $ContainerName --tail 20

Write-Host ""
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "Container is running!" -ForegroundColor Green
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

# Test health endpoint
Write-Host "Testing health endpoint..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "http://localhost:$Port/health" -UseBasicParsing -TimeoutSec 10
    if ($response.StatusCode -eq 200) {
        Write-Host "? Health check passed! Status: $($response.Content)" -ForegroundColor Green
    } else {
        Write-Host "??  Health check returned status: $($response.StatusCode)" -ForegroundColor Yellow
    }
} catch {
    Write-Host "??  Health check failed: $($_.Exception.Message)" -ForegroundColor Yellow
    Write-Host "    This may be normal if the API is still starting up." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "Quick Commands:" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "View logs:        docker logs $ContainerName -f" -ForegroundColor White
Write-Host "Stop container:   docker stop $ContainerName" -ForegroundColor White
Write-Host "Remove container: docker rm $ContainerName" -ForegroundColor White
Write-Host "Restart:          docker restart $ContainerName" -ForegroundColor White
Write-Host ""
Write-Host "Swagger UI:       http://localhost:$Port" -ForegroundColor Cyan
Write-Host "Health endpoint:  http://localhost:$Port/health" -ForegroundColor Cyan
Write-Host ""
Write-Host "Admin credentials:" -ForegroundColor Yellow
Write-Host "  Username: admin" -ForegroundColor White
Write-Host "  Password: Admin123!" -ForegroundColor White
Write-Host ""
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "To stop and clean up:" -ForegroundColor Yellow
Write-Host "  docker stop $ContainerName && docker rm $ContainerName" -ForegroundColor White
Write-Host "=====================================" -ForegroundColor Cyan
