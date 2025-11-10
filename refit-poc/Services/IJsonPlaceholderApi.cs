using Refit;
using RefitPoc.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RefitPoc.Services;

/// <summary>
/// Refit interface for JSONPlaceholder API
/// Demonstrates various Refit features and HTTP methods
/// </summary>
public interface IJsonPlaceholderApi
{
    // GET requests
    [Get("/posts")]
    Task<List<Post>> GetPostsAsync(CancellationToken cancellationToken = default);

    [Get("/posts/{id}")]
    Task<Post> GetPostByIdAsync(int id, CancellationToken cancellationToken = default);

    [Get("/posts")]
    Task<List<Post>> GetPostsByUserIdAsync([Query] int userId, CancellationToken cancellationToken = default);

    [Get("/posts")]
    Task<List<Post>> GetPostsPaginatedAsync(
        [Query] int _start, 
        [Query] int _limit, 
        CancellationToken cancellationToken = default);

    // POST request
    [Post("/posts")]
    Task<Post> CreatePostAsync([Body] CreatePostRequest request, CancellationToken cancellationToken = default);

    // PUT request
    [Put("/posts/{id}")]
    Task<Post> UpdatePostAsync(int id, [Body] UpdatePostRequest request, CancellationToken cancellationToken = default);

    // PATCH request (partial update)
    [Patch("/posts/{id}")]
    Task<Post> PatchPostAsync(int id, [Body] object partialUpdate, CancellationToken cancellationToken = default);

    // DELETE request
    [Delete("/posts/{id}")]
    Task DeletePostAsync(int id, CancellationToken cancellationToken = default);

    // Related resources
    [Get("/posts/{postId}/comments")]
    Task<List<Comment>> GetPostCommentsAsync(int postId, CancellationToken cancellationToken = default);

    [Get("/users/{id}")]
    Task<User> GetUserAsync(int id, CancellationToken cancellationToken = default);

    [Get("/users")]
    Task<List<User>> GetUsersAsync(CancellationToken cancellationToken = default);

    // Headers example
    [Headers("X-Custom-Header: CustomValue")]
    [Get("/posts")]
    Task<List<Post>> GetPostsWithCustomHeaderAsync(CancellationToken cancellationToken = default);

    // Dynamic headers
    [Get("/posts")]
    Task<List<Post>> GetPostsWithDynamicHeaderAsync(
        [Header("X-API-KEY")] string apiKey, 
        CancellationToken cancellationToken = default);

    // Query with multiple parameters
    [Get("/posts")]
    Task<List<Post>> SearchPostsAsync(
        [Query] string? title,
        [Query] string? body,
        [Query] int? userId,
        [Query("_sort")] string? sortBy,
        [Query("_order")] string? order,
        CancellationToken cancellationToken = default);

    // Form URL encoded content
    [Post("/posts")]
    [Headers("Content-Type: application/x-www-form-urlencoded")]
    Task<Post> CreatePostFormAsync(
        [Body(BodySerializationMethod.UrlEncoded)] Dictionary<string, object> form,
        CancellationToken cancellationToken = default);

    // Multipart form data (for file uploads)
    [Multipart]
    [Post("/upload")]
    Task<ApiResponse<string>> UploadFileAsync(
        [AliasAs("file")] StreamPart stream,
        CancellationToken cancellationToken = default);

    // Get raw HttpResponseMessage
    [Get("/posts/{id}")]
    Task<HttpResponseMessage> GetPostResponseAsync(int id, CancellationToken cancellationToken = default);

    // Get with ApiResponse wrapper for status code and headers
    [Get("/posts/{id}")]
    Task<ApiResponse<Post>> GetPostWithMetadataAsync(int id, CancellationToken cancellationToken = default);
}
