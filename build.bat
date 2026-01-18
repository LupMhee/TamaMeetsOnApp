@echo off
REM Build script for TamagotchiMeetsOnEditor

setlocal enabledelayedexpansion

set "CONFIGURATION=Release"
set "CLEAN=0"
set "RESTORE=0"
set "BUILD=0"
set "PUBLISH=1"

REM Parse command line arguments
:parse
if "%~1"=="" goto :build
if /i "%~1"=="Debug" set "CONFIGURATION=Debug" & shift & goto :parse
if /i "%~1"=="Release" set "CONFIGURATION=Release" & shift & goto :parse
if /i "%~1"=="--configuration" (
    set "CONFIGURATION=%~2"
    shift & shift
    goto :parse
)
if /i "%~1"=="-configuration" (
    set "CONFIGURATION=%~2"
    shift & shift
    goto :parse
)
if /i "%~1"=="--clean" set "CLEAN=1" & shift & goto :parse
if /i "%~1"=="-clean" set "CLEAN=1" & shift & goto :parse
if /i "%~1"=="--restore" set "RESTORE=1" & shift & goto :parse
if /i "%~1"=="-restore" set "RESTORE=1" & shift & goto :parse
if /i "%~1"=="--build" set "BUILD=1" & set "PUBLISH=0" & shift & goto :parse
if /i "%~1"=="-build" set "BUILD=1" & set "PUBLISH=0" & shift & goto :parse
if /i "%~1"=="--publish" set "PUBLISH=1" & set "BUILD=0" & shift & goto :parse
if /i "%~1"=="-publish" set "PUBLISH=1" & set "BUILD=0" & shift & goto :parse
if /i "%~1"=="--help" goto :help
if /i "%~1"=="-help" goto :help
if /i "%~1"=="-h" goto :help
if /i "%~1"=="/?" goto :help
shift
goto :parse

:build
echo Building TamagotchiMeetsOnEditor...
echo Configuration: %CONFIGURATION%

REM Clean if requested
if %CLEAN%==1 (
    echo Cleaning build outputs...
    dotnet clean TamagotchiMeetsOnEditor.sln --configuration %CONFIGURATION%
    if errorlevel 1 (
        echo Clean failed!
        exit /b 1
    )
)

REM Restore packages if requested
if %RESTORE%==1 (
    echo Restoring NuGet packages...
    dotnet restore TamagotchiMeetsOnEditor.sln
    if errorlevel 1 (
        echo Restore failed!
        exit /b 1
    )
)

REM Build or Publish (default is Publish)
if %BUILD%==1 (
    echo Building solution...
    dotnet build TamagotchiMeetsOnEditor.sln --configuration %CONFIGURATION% --no-incremental
    if errorlevel 1 (
        echo Build failed!
        exit /b 1
    )
    echo Build completed successfully!
    if exist "bin\%CONFIGURATION%\net8.0-windows\TamagotchiMeetsOnEditor.exe" (
        echo Output: bin\%CONFIGURATION%\net8.0-windows\TamagotchiMeetsOnEditor.exe
    )
) else (
    echo Publishing as single file...
    set "BUILD_OUTPUT_PATH=bin\%CONFIGURATION%\net8.0-windows"
    set "PUBLISH_PATH=%BUILD_OUTPUT_PATH%\publish"
    
    dotnet publish TamagotchiMeetsOnEditor.csproj --configuration %CONFIGURATION% --output "bin\%CONFIGURATION%\net8.0-windows\publish" /p:PublishSingleFile=true
    if errorlevel 1 (
        echo Publish failed!
        exit /b 1
    )
    
    echo Publish completed successfully!
    
    if exist "bin\%CONFIGURATION%\net8.0-windows" (
        echo Cleaning build output directory...
        for /d %%d in ("bin\%CONFIGURATION%\net8.0-windows\*") do (
            set "FOLDER_NAME=%%~nxd"
            if /i not "!FOLDER_NAME!"=="publish" (
                echo Deleting folder: !FOLDER_NAME!
                rd /s /q "%%d" 2>nul
            )
        )
        for %%f in ("bin\%CONFIGURATION%\net8.0-windows\*.*") do (
            echo Deleting file: %%~nxf
            del /q "%%f" 2>nul
        )
    )
    if exist "bin\%CONFIGURATION%\net8.0-windows\publish\TamagotchiMeetsOnEditor.exe" (
        echo Output: bin\%CONFIGURATION%\net8.0-windows\publish\TamagotchiMeetsOnEditor.exe
    )
)

exit /b 0

:help
echo Usage: build.bat [options]
echo.
echo Options:
echo   Debug|Release          Build configuration (default: Release)
echo   --configuration ^<config^>   Set build configuration
echo   --clean                Clean build outputs before building
echo   --restore              Restore NuGet packages before building
echo   --build                Build normally (default: Publish as single file)
echo   --publish              Publish as single file (embeds DLLs into EXE) - DEFAULT
echo   --help                 Show this help message
echo.
echo Examples:
echo   build.bat              Publish as single file EXE (default)
echo   build.bat --build      Build normally
echo   build.bat Debug --clean --restore
exit /b 0
