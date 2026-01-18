# Build script for TamagotchiMeetsOnEditor
param(
    [string]$Configuration = "Release",
    [switch]$Clean,
    [switch]$Restore,
    [switch]$Build,
    [switch]$Publish,
    [switch]$Help
)

$ErrorActionPreference = "Stop"

if ($Help) {
    Write-Host "Usage: .\build.ps1 [options]"
    Write-Host ""
    Write-Host "Options:"
    Write-Host "  -Configuration <Debug|Release>  Build configuration (default: Release)"
    Write-Host "  -Clean                          Clean build outputs before building"
    Write-Host "  -Restore                        Restore NuGet packages before building"
    Write-Host "  -Build                          Build normally (default: Publish as single file)"
    Write-Host "  -Publish                        Publish as single file (embeds DLLs into EXE) - DEFAULT"
    Write-Host "  -Help                           Show this help message"
    Write-Host ""
    Write-Host "Examples:"
    Write-Host "  .\build.ps1                     Publish as single file EXE (default)"
    Write-Host "  .\build.ps1 -Build              Build normally"
    Write-Host "  .\build.ps1 -Configuration Debug -Clean -Restore"
    exit 0
}

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$SolutionFile = Join-Path $ScriptDir "TamagotchiMeetsOnEditor.sln"
$ProjectFile = Join-Path $ScriptDir "TamagotchiMeetsOnEditor.csproj"

Write-Host "Building TamagotchiMeetsOnEditor..." -ForegroundColor Cyan
Write-Host "Configuration: $Configuration" -ForegroundColor Yellow

# Clean if requested
if ($Clean) {
    Write-Host "Cleaning build outputs..." -ForegroundColor Yellow
    dotnet clean $SolutionFile --configuration $Configuration
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Clean failed!" -ForegroundColor Red
        exit 1
    }
}

# Restore packages if requested
if ($Restore -or $Clean) {
    Write-Host "Restoring NuGet packages..." -ForegroundColor Yellow
    dotnet restore $SolutionFile
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Restore failed!" -ForegroundColor Red
        exit 1
    }
}

# Build or Publish (default is Publish)
if ($Build) {
    Write-Host "Building solution..." -ForegroundColor Yellow
    dotnet build $SolutionFile --configuration $Configuration --no-incremental
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Build failed!" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "Build completed successfully!" -ForegroundColor Green
    
    $OutputPath = Join-Path $ScriptDir "bin\$Configuration\net8.0-windows"
    if (Test-Path $OutputPath) {
        $ExeFile = Join-Path $OutputPath "TamagotchiMeetsOnEditor.exe"
        if (Test-Path $ExeFile) {
            Write-Host "Output: $ExeFile" -ForegroundColor Cyan
        }
    }
}
else {
    Write-Host "Publishing as single file..." -ForegroundColor Yellow
    
    $BuildOutputPath = Join-Path $ScriptDir "bin\$Configuration\net8.0-windows"
    $PublishPath = Join-Path $BuildOutputPath "publish"
    
    dotnet publish $ProjectFile --configuration $Configuration --output $PublishPath /p:PublishSingleFile=true
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Publish failed!" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "Publish completed successfully!" -ForegroundColor Green
    
    if (Test-Path $BuildOutputPath -PathType Container) {
        Write-Host "Cleaning build output directory..." -ForegroundColor Yellow
        Get-ChildItem -Path $BuildOutputPath -Exclude "publish" -Force | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
    }
    
    if (Test-Path $PublishPath) {
        $ExeFile = Join-Path $PublishPath "TamagotchiMeetsOnEditor.exe"
        if (Test-Path $ExeFile) {
            Write-Host "Output: $ExeFile" -ForegroundColor Cyan
        }
    }
}
