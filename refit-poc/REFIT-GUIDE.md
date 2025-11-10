# Refit in .NET 8 - Complete Guide and Best Practices

## Table of Contents
1. [Overview](#overview)
2. [Key Features](#key-features)
3. [Setup and Configuration](#setup-and-configuration)
4. [HTTP Methods](#http-methods)
5. [Request/Response Handling](#requestresponse-handling)
6. [Error Handling](#error-handling)
7. [Authentication](#authentication)
8. [Resilience Patterns](#resilience-patterns)
9. [Testing](#testing)
10. [Best Practices](#best-practices)

## Overview

Refit is a REST library for .NET that turns your REST API into a live interface. It's inspired by Retrofit from Square and provides a declarative way to define HTTP clients.

### Benefits
- **Type-safe**: Compile-time checking of API contracts
- **Declarative**: Define APIs using interfaces and attributes
- **Integration**: Works seamlessly with HttpClient and IHttpClientFactory
- **Testable**: Easy to mock and test
- **Extensible**: Support for custom formatters, handlers, and policies

## Key Features

### 1. Automatic Type-Safe REST Client Generation
```csharp
public interface IMyApi
{
    [Get("/users/{id}")]
    Task<User> GetUser(int id);
}
```

### 2. Built-in JSON Serialization
- Supports System.Text.Json and Newtonsoft.Json
- Automatic serialization/deserialization

### 3. Dynamic Headers and Query Parameters
- Static headers via attributes
- Dynamic headers via parameters
- Query string generation

### 4. File Upload Support
- Multipart form data
- Stream uploads
- Progress reporting

## Setup and Configuration

### Basic Setup
```csharp
// In Program.cs
builder.Services
    .AddRefitClient<IMyApi>()
    .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://api.example.com"));
```

### With Polly for Resilience
```csharp
builder.Services
    .AddRefitClient<IMyApi>()
    .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://api.example.com"))
    .AddPolicyHandler(GetRetryPolicy())
    .AddPolicyHandler(GetCircuitBreakerPolicy());
```

## HTTP Methods

### GET Requests
```csharp
[Get("/resource")]
Task<List<Item>> GetItems();

[Get("/resource/{id}")]
Task<Item> GetItem(int id);

[Get("/resource")]
Task<List<Item>> GetItemsWithQuery([Query] string filter);
```

### POST Requests
```csharp
[Post("/resource")]
Task<Item> CreateItem([Body] CreateItemRequest request);

[Post("/resource")]
[Headers("Content-Type: application/x-www-form-urlencoded")]
Task<Item> CreateItemForm([Body(BodySerializationMethod.UrlEncoded)] Dictionary<string, object> form);
```

### PUT/PATCH Requests
```csharp
[Put("/resource/{id}")]
Task<Item> UpdateItem(int id, [Body] UpdateItemRequest request);

[Patch("/resource/{id}")]
Task<Item> PatchItem(int id, [Body] object partialUpdate);
```

### DELETE Requests
```csharp
[Delete("/resource/{id}")]
Task DeleteItem(int id);
```

## Request/Response Handling

### Query Parameters
```csharp
// Single parameter
[Get("/search")]
Task<List<Result>> Search([Query] string q);

// Multiple parameters
[Get("/search")]
Task<List<Result>> SearchAdvanced(
    [Query] string q,
    [Query("_limit")] int limit,
    [Query(CollectionFormat.Multi)] int[] categories);

// Object as query parameters
[Get("/search")]
Task<List<Result>> Search([Query] SearchParams parameters);
```

### Headers
```csharp
// Static headers
[Headers("X-API-Version: 2")]
[Get("/data")]
Task<Data> GetData();

// Dynamic headers
[Get("/data")]
Task<Data> GetDataWithAuth([Header("Authorization")] string token);

// Request-specific headers
[Get("/data")]
Task<Data> GetDataWithHeaders(
    [Header("X-Custom-Header")] string customHeader,
    [Header("X-Request-ID")] string requestId);
```

### Path Parameters
```csharp
[Get("/users/{userId}/posts/{postId}")]
Task<Post> GetUserPost(int userId, int postId);

// With custom formatting
[Get("/date/{date:yyyy-MM-dd}")]
Task<Events> GetEventsByDate(DateTime date);
```

### Request Body
```csharp
// JSON body (default)
[Post("/data")]
Task<Response> PostJson([Body] MyData data);

// Form URL encoded
[Post("/form")]
Task<Response> PostForm([Body(BodySerializationMethod.UrlEncoded)] Dictionary<string, object> form);

// Multipart
[Multipart]
[Post("/upload")]
Task<Response> Upload([AliasAs("file")] StreamPart stream);
```

## Error Handling

### ApiException Handling
```csharp
try
{
    var result = await api.GetDataAsync();
}
catch (ApiException ex)
{
    // Access HTTP status code
    var statusCode = ex.StatusCode;
    
    // Access response content
    var content = ex.Content;
    
    // Access response headers
    var headers = ex.Headers;
    
    // Handle specific status codes
    switch (statusCode)
    {
        case HttpStatusCode.NotFound:
            // Handle 404
            break;
        case HttpStatusCode.Unauthorized:
            // Handle 401
            break;
        default:
            // Handle other errors
            break;
    }
}
```

### Using ApiResponse for Non-Exception Handling
```csharp
[Get("/data")]
Task<ApiResponse<Data>> GetDataResponse();

// Usage
var response = await api.GetDataResponse();
if (response.IsSuccessStatusCode)
{
    var data = response.Content;
}
else
{
    var statusCode = response.StatusCode;
    var error = response.Error;
}
```

## Authentication

### Bearer Token
```csharp
// Static token
[Headers("Authorization: Bearer YOUR_TOKEN")]
[Get("/protected")]
Task<Data> GetProtectedData();

// Dynamic token
[Get("/protected")]
Task<Data> GetProtectedData([Authorize("Bearer")] string token);

// Via HttpMessageHandler
public class AuthHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        request.Headers.Authorization = 
            new AuthenticationHeaderValue("Bearer", GetToken());
        return await base.SendAsync(request, cancellationToken);
    }
}
```

### API Key
```csharp
// In header
[Headers("X-API-Key: YOUR_API_KEY")]
[Get("/data")]
Task<Data> GetData();

// In query string
[Get("/data")]
Task<Data> GetData([Query("api_key")] string apiKey);
```

## Resilience Patterns

### Retry Policy with Polly
```csharp
static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(msg => !msg.IsSuccessStatusCode)
        .WaitAndRetryAsync(
            3,
            retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            onRetry: (outcome, timespan, retryCount, context) =>
            {
                Console.WriteLine($"Retry {retryCount} after {timespan}");
            });
}
```

### Circuit Breaker
```csharp
static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .CircuitBreakerAsync(
            handledEventsAllowedBeforeBreaking: 5,
            durationOfBreak: TimeSpan.FromSeconds(30));
}
```

### Timeout Policy
```csharp
static IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy()
{
    return Policy.TimeoutAsync<HttpResponseMessage>(10);
}
```

### Combining Policies
```csharp
builder.Services
    .AddRefitClient<IMyApi>()
    .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://api.example.com"))
    .AddPolicyHandler(GetRetryPolicy())
    .AddPolicyHandler(GetCircuitBreakerPolicy())
    .AddPolicyHandler(GetTimeoutPolicy());
```

## Testing

### Mocking Refit Interfaces
```csharp
[Fact]
public async Task TestGetUser()
{
    // Arrange
    var mockApi = new Mock<IMyApi>();
    mockApi.Setup(x => x.GetUser(It.IsAny<int>()))
           .ReturnsAsync(new User { Id = 1, Name = "Test" });

    // Act
    var user = await mockApi.Object.GetUser(1);

    // Assert
    Assert.Equal("Test", user.Name);
}
```

### Testing with HttpMessageHandler
```csharp
[Fact]
public async Task TestWithHttpClient()
{
    // Arrange
    var handlerMock = new Mock<HttpMessageHandler>();
    handlerMock
        .Protected()
        .Setup<Task<HttpResponseMessage>>(
            "SendAsync",
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>())
        .ReturnsAsync(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(JsonSerializer.Serialize(new User()))
        });

    var httpClient = new HttpClient(handlerMock.Object)
    {
        BaseAddress = new Uri("https://api.example.com")
    };
    
    var api = RestService.For<IMyApi>(httpClient);

    // Act
    var user = await api.GetUser(1);

    // Assert
    Assert.NotNull(user);
}
```

## Best Practices

### 1. Use IHttpClientFactory
Always configure Refit clients with IHttpClientFactory to properly manage HttpClient lifecycle:
```csharp
builder.Services.AddRefitClient<IMyApi>()
    .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://api.example.com"));
```

### 2. Implement Proper Error Handling
Always wrap API calls in try-catch blocks and handle ApiException:
```csharp
try
{
    var data = await api.GetDataAsync();
}
catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
{
    // Handle not found
}
catch (ApiException ex)
{
    // Handle other API errors
}
catch (TaskCanceledException)
{
    // Handle timeout
}
```

### 3. Use Cancellation Tokens
Always pass cancellation tokens to support request cancellation:
```csharp
[Get("/data")]
Task<Data> GetData(CancellationToken cancellationToken = default);
```

### 4. Configure Timeouts
Set appropriate timeouts for HttpClient:
```csharp
.ConfigureHttpClient(c =>
{
    c.Timeout = TimeSpan.FromSeconds(30);
})
```

### 5. Use Strongly Typed Models
Define DTOs for requests and responses instead of using dynamic or object:
```csharp
public class CreateUserRequest
{
    public string Name { get; set; }
    public string Email { get; set; }
}
```

### 6. Logging
Implement logging for debugging and monitoring:
```csharp
public class LoggingHandler : DelegatingHandler
{
    private readonly ILogger<LoggingHandler> _logger;

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Request: {request.Method} {request.RequestUri}");
        var response = await base.SendAsync(request, cancellationToken);
        _logger.LogInformation($"Response: {response.StatusCode}");
        return response;
    }
}
```

### 7. Validate Input
Validate input parameters before making API calls:
```csharp
public async Task<User> GetUserSafe(int id)
{
    if (id <= 0)
        throw new ArgumentException("Invalid user ID", nameof(id));
    
    return await _api.GetUser(id);
}
```

### 8. Use ApiResponse for Metadata
When you need access to headers or status codes, use ApiResponse:
```csharp
[Get("/data")]
Task<ApiResponse<Data>> GetDataWithMetadata();
```

### 9. Handle Rate Limiting
Implement rate limiting awareness:
```csharp
catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests)
{
    var retryAfter = ex.Headers?.RetryAfter?.Delta;
    await Task.Delay(retryAfter ?? TimeSpan.FromSeconds(60));
    // Retry
}
```

### 10. Document Your APIs
Use XML comments to document your Refit interfaces:
```csharp
/// <summary>
/// Gets a user by ID
/// </summary>
/// <param name="id">User ID</param>
/// <returns>User object</returns>
[Get("/users/{id}")]
Task<User> GetUser(int id);
```

## Common Patterns

### Pagination
```csharp
[Get("/items")]
Task<PagedResult<Item>> GetItems(
    [Query] int page = 1,
    [Query] int pageSize = 20);

public class PagedResult<T>
{
    public List<T> Items { get; set; }
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
```

### Batch Operations
```csharp
[Post("/batch")]
Task<BatchResult> ProcessBatch([Body] List<BatchItem> items);
```

### Conditional Requests
```csharp
[Get("/resource")]
Task<ApiResponse<Resource>> GetResource(
    [Header("If-None-Match")] string etag = null,
    [Header("If-Modified-Since")] DateTimeOffset? ifModifiedSince = null);
```

### Streaming
```csharp
[Get("/stream")]
Task<Stream> GetStream();

// Usage
using var stream = await api.GetStream();
using var reader = new StreamReader(stream);
var content = await reader.ReadToEndAsync();
```

## Troubleshooting

### Common Issues

1. **BaseAddress not set**: Ensure BaseAddress is configured in HttpClient
2. **Serialization errors**: Check JSON property names and types match
3. **404 Not Found**: Verify URL construction and path parameters
4. **401 Unauthorized**: Check authentication headers
5. **Timeout**: Increase timeout or implement retry logic

### Debugging Tips

1. Enable HttpClient logging
2. Use Fiddler or similar tools to inspect HTTP traffic
3. Check ApiException.Content for error details
4. Use ApiResponse to inspect headers and status codes
5. Implement custom DelegatingHandler for detailed logging

## Performance Considerations

1. **Reuse HttpClient**: Use IHttpClientFactory
2. **Compression**: Enable gzip/deflate compression
3. **Caching**: Implement HTTP caching where appropriate
4. **Connection pooling**: Configure ServicePointManager
5. **Async all the way**: Don't block on async calls

## Conclusion

Refit simplifies REST API client development in .NET by providing a declarative, type-safe approach. Combined with Polly for resilience, proper error handling, and testing strategies, it enables building robust and maintainable API clients.
