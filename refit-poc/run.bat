@echo off
echo ================================
echo Refit POC - .NET 8 Demo
echo ================================
echo.

REM Check if .NET is installed
where dotnet >nul 2>nul
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: .NET SDK not found. Please install .NET 8 SDK first.
    echo Download from: https://dotnet.microsoft.com/download/dotnet/8.0
    pause
    exit /b 1
)

REM Check .NET version
echo Checking .NET version...
dotnet --version
echo.

REM Restore packages
echo Restoring NuGet packages...
dotnet restore
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: Package restore failed
    pause
    exit /b 1
)
echo Packages restored successfully
echo.

REM Build the project
echo Building the project...
dotnet build --configuration Release
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: Build failed
    pause
    exit /b 1
)
echo Build completed successfully
echo.

REM Run the application
echo Starting the application...
echo ================================
echo API will be available at:
echo   - https://localhost:5001
echo   - http://localhost:5000
echo.
echo Swagger UI available at:
echo   - https://localhost:5001/swagger
echo   - http://localhost:5000/swagger
echo ================================
echo.
echo Press Ctrl+C to stop the application
echo.

dotnet run --configuration Release
