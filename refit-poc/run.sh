#!/bin/bash

echo "================================"
echo "Refit POC - .NET 8 Demo"
echo "================================"
echo ""

# Check if .NET 8 is installed
if ! command -v dotnet &> /dev/null
then
    echo "❌ .NET SDK not found. Please install .NET 8 SDK first."
    echo "Download from: https://dotnet.microsoft.com/download/dotnet/8.0"
    exit 1
fi

# Check .NET version
echo "📌 Checking .NET version..."
dotnet --version
echo ""

# Restore packages
echo "📦 Restoring NuGet packages..."
dotnet restore
if [ $? -ne 0 ]; then
    echo "❌ Package restore failed"
    exit 1
fi
echo "✅ Packages restored successfully"
echo ""

# Build the project
echo "🔨 Building the project..."
dotnet build --configuration Release
if [ $? -ne 0 ]; then
    echo "❌ Build failed"
    exit 1
fi
echo "✅ Build completed successfully"
echo ""

# Run the application
echo "🚀 Starting the application..."
echo "================================"
echo "API will be available at:"
echo "  - https://localhost:5001"
echo "  - http://localhost:5000"
echo ""
echo "Swagger UI available at:"
echo "  - https://localhost:5001/swagger"
echo "  - http://localhost:5000/swagger"
echo "================================"
echo ""
echo "Press Ctrl+C to stop the application"
echo ""

dotnet run --configuration Release
