using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Refit;
using RefitPoc.Models;
using RefitPoc.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RefitPoc.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PostsController : ControllerBase
{
    private readonly IJsonPlaceholderApi _jsonPlaceholderApi;
    private readonly ILogger<PostsController> _logger;

    public PostsController(IJsonPlaceholderApi jsonPlaceholderApi, ILogger<PostsController> logger)
    {
        _jsonPlaceholderApi = jsonPlaceholderApi;
        _logger = logger;
    }

    /// <summary>
    /// Get all posts
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<Post>>> GetAllPosts(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Fetching all posts");
            var posts = await _jsonPlaceholderApi.GetPostsAsync(cancellationToken);
            _logger.LogInformation($"Retrieved {posts.Count} posts");
            return Ok(posts);
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, $"API error: {ex.StatusCode} - {ex.Content}");
            return StatusCode((int)ex.StatusCode, new { error = ex.Message, details = ex.Content });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching posts");
            return StatusCode(500, new { error = "Internal server error", message = ex.Message });
        }
    }

    /// <summary>
    /// Get post by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<Post>> GetPostById(int id, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation($"Fetching post with ID: {id}");
            var post = await _jsonPlaceholderApi.GetPostByIdAsync(id, cancellationToken);
            return Ok(post);
        }
        catch (ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning($"Post with ID {id} not found");
            return NotFound(new { error = $"Post with ID {id} not found" });
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, $"API error: {ex.StatusCode}");
            return StatusCode((int)ex.StatusCode, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get posts with pagination
    /// </summary>
    [HttpGet("paginated")]
    public async Task<ActionResult<List<Post>>> GetPostsPaginated(
        [FromQuery] int start = 0,
        [FromQuery] int limit = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation($"Fetching posts with pagination: start={start}, limit={limit}");
            var posts = await _jsonPlaceholderApi.GetPostsPaginatedAsync(start, limit, cancellationToken);
            return Ok(new
            {
                data = posts,
                pagination = new
                {
                    start,
                    limit,
                    total = posts.Count
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching paginated posts");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get posts by user ID
    /// </summary>
    [HttpGet("user/{userId}")]
    public async Task<ActionResult<List<Post>>> GetPostsByUserId(int userId, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation($"Fetching posts for user ID: {userId}");
            var posts = await _jsonPlaceholderApi.GetPostsByUserIdAsync(userId, cancellationToken);
            return Ok(posts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error fetching posts for user {userId}");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Create a new post
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<Post>> CreatePost([FromBody] CreatePostRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _logger.LogInformation($"Creating new post: {request.Title}");
            var createdPost = await _jsonPlaceholderApi.CreatePostAsync(request, cancellationToken);
            _logger.LogInformation($"Post created with ID: {createdPost.Id}");

            return CreatedAtAction(nameof(GetPostById), new { id = createdPost.Id }, createdPost);
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Error creating post");
            return StatusCode((int)ex.StatusCode, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Update an existing post
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<Post>> UpdatePost(int id, [FromBody] UpdatePostRequest request, CancellationToken cancellationToken)
    {
        try
        {
            request.Id = id; // Ensure ID matches route parameter
            _logger.LogInformation($"Updating post ID: {id}");
            var updatedPost = await _jsonPlaceholderApi.UpdatePostAsync(id, request, cancellationToken);
            return Ok(updatedPost);
        }
        catch (ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return NotFound(new { error = $"Post with ID {id} not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error updating post {id}");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Partially update a post
    /// </summary>
    [HttpPatch("{id}")]
    public async Task<ActionResult<Post>> PatchPost(int id, [FromBody] object partialUpdate, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation($"Patching post ID: {id}");
            var patchedPost = await _jsonPlaceholderApi.PatchPostAsync(id, partialUpdate, cancellationToken);
            return Ok(patchedPost);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error patching post {id}");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Delete a post
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePost(int id, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation($"Deleting post ID: {id}");
            await _jsonPlaceholderApi.DeletePostAsync(id, cancellationToken);
            _logger.LogInformation($"Post {id} deleted successfully");
            return NoContent();
        }
        catch (ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return NotFound(new { error = $"Post with ID {id} not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting post {id}");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get comments for a post
    /// </summary>
    [HttpGet("{postId}/comments")]
    public async Task<ActionResult<List<Comment>>> GetPostComments(int postId, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation($"Fetching comments for post ID: {postId}");
            var comments = await _jsonPlaceholderApi.GetPostCommentsAsync(postId, cancellationToken);
            return Ok(comments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error fetching comments for post {postId}");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get post with metadata (demonstrates ApiResponse usage)
    /// </summary>
    [HttpGet("{id}/metadata")]
    public async Task<IActionResult> GetPostWithMetadata(int id, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation($"Fetching post with metadata for ID: {id}");
            var response = await _jsonPlaceholderApi.GetPostWithMetadataAsync(id, cancellationToken);

            var metadata = new
            {
                statusCode = response.StatusCode,
                headers = response.Headers?.ToDictionary(h => h.Key, h => h.Value.FirstOrDefault()),
                isSuccess = response.IsSuccessStatusCode,
                reasonPhrase = response.ReasonPhrase,
                data = response.Content
            };

            return Ok(metadata);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error fetching post metadata for ID {id}");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Search posts with multiple parameters
    /// </summary>
    [HttpGet("search")]
    public async Task<ActionResult<List<Post>>> SearchPosts(
        [FromQuery] string? title,
        [FromQuery] string? body,
        [FromQuery] int? userId,
        [FromQuery] string? sortBy,
        [FromQuery] string? order,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation($"Searching posts with filters");
            var posts = await _jsonPlaceholderApi.SearchPostsAsync(title, body, userId, sortBy, order, cancellationToken);
            return Ok(posts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching posts");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
