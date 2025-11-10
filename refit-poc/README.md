# Refit POC - .NET 8 API Client

## Overview
This is a complete Proof of Concept demonstrating Refit library in .NET 8 for building robust API clients. 
This example uses the JSONPlaceholder API (a free fake REST API) to demonstrate various Refit features.

## Features Demonstrated
- Basic GET, POST, PUT, DELETE operations
- Query parameters and dynamic URLs
- Request/Response models
- Error handling with Polly
- Authentication headers
- Logging integration
- Dependency Injection setup

## Prerequisites
- .NET 8 SDK
- Visual Studio 2022 or VS Code

## Setup Instructions
1. Clone/Copy this project
2. Run `dotnet restore`
3. Run `dotnet build`
4. Run `dotnet run`

## Project Structure
```
refit-poc/
├── Program.cs              # Main configuration and DI setup
├── Controllers/            # API Controllers
│   └── PostsController.cs
├── Services/              # Refit API Interfaces
│   ├── IJsonPlaceholderApi.cs
│   └── IAuthenticatedApi.cs
├── Models/                # Request/Response DTOs
│   ├── Post.cs
│   ├── User.cs
│   └── Comment.cs
├── Handlers/              # Custom HTTP handlers
│   └── AuthHeaderHandler.cs
└── appsettings.json       # Configuration
```

## API Endpoints
- GET /api/posts - Get all posts
- GET /api/posts/{id} - Get post by ID
- POST /api/posts - Create new post
- PUT /api/posts/{id} - Update post
- DELETE /api/posts/{id} - Delete post
- GET /api/posts/{id}/comments - Get post comments
