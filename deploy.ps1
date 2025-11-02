# EventEase Deployment Script
# This script builds and publishes the EventEase application for production deployment

Write-Host "Starting EventEase deployment preparation..." -ForegroundColor Green

# Clean previous builds
Write-Host "Cleaning previous builds..." -ForegroundColor Yellow
dotnet clean --configuration Release

# Restore packages
Write-Host "Restoring NuGet packages..." -ForegroundColor Yellow
dotnet restore

# Build in Release mode
Write-Host "Building application in Release mode..." -ForegroundColor Yellow
dotnet build --configuration Release --no-restore

# Publish the application
Write-Host "Publishing application..." -ForegroundColor Yellow
dotnet publish --configuration Release --no-build --output "./publish"

Write-Host "Deployment preparation completed!" -ForegroundColor Green
Write-Host "Published files are in the './publish' directory" -ForegroundColor Cyan
Write-Host ""
Write-Host "Deployment Options:" -ForegroundColor White
Write-Host "1. Static Web Hosting (GitHub Pages, Netlify, Vercel):" -ForegroundColor Yellow
Write-Host "   - Upload contents of './publish/wwwroot' to your hosting provider"
Write-Host ""
Write-Host "2. Azure Static Web Apps:" -ForegroundColor Yellow
Write-Host "   - Use Azure CLI: az staticwebapp create --source ./publish/wwwroot"
Write-Host ""
Write-Host "3. IIS/Web Server:" -ForegroundColor Yellow
Write-Host "   - Copy './publish' contents to your web server directory"
Write-Host "   - Ensure your web server supports Blazor WebAssembly"
Write-Host ""
Write-Host "4. Docker Container:" -ForegroundColor Yellow
Write-Host "   - Use the included Dockerfile to build a container image"