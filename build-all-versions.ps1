# Build script for creating both Silk 0.7.0 and 0.6.1 versions
param(
    [switch]$CleanFirst = $false
)

Write-Host "Stats Mod Multi-Version Builder" -ForegroundColor Cyan
Write-Host ""

# Clean if requested
if ($CleanFirst) {
    Write-Host "Cleaning previous builds..." -ForegroundColor Yellow
    dotnet clean
    Write-Host ""
}

# Build for Silk 0.7.0
Write-Host "Building for Silk 0.7.0..." -ForegroundColor Green
dotnet build StatsMod.csproj -c Release-v0.7.0
if ($LASTEXITCODE -ne 0) {
    Write-Host "Failed to build for Silk 0.7.0" -ForegroundColor Red
    exit 1
}
Write-Host "✓ Silk 0.7.0 build complete" -ForegroundColor Green
Write-Host ""

# Build for Silk 0.6.1
Write-Host "Building for Silk 0.6.1..." -ForegroundColor Green
dotnet build StatsMod.csproj -c Release-v0.6.1
if ($LASTEXITCODE -ne 0) {
    Write-Host "Failed to build for Silk 0.6.1" -ForegroundColor Red
    exit 1
}
Write-Host "✓ Silk 0.6.1 build complete" -ForegroundColor Green
Write-Host ""

# Summary
Write-Host "=================================" -ForegroundColor Cyan
Write-Host "Build Summary" -ForegroundColor Cyan
Write-Host "=================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Output files created:" -ForegroundColor White
Write-Host "  • bin\Release-v0.7\net472\StatsMod-v0.7.0.dll" -ForegroundColor Yellow
Write-Host "  • bin\Release-v0.6\net472\StatsMod-v0.6.1.dll" -ForegroundColor Yellow
Write-Host ""
Write-Host "✓ All builds completed successfully!" -ForegroundColor Green
