using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Polly.CircuitBreaker;
using Refit;
using RefitPoc.Models;
using RefitPoc.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace RefitPoc.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdvancedController : ControllerBase
{
    private readonly IJsonPlaceholderApi _api;
    private readonly ILogger<AdvancedController> _logger;

    public AdvancedController(IJsonPlaceholderApi api, ILogger<AdvancedController> logger)
    {
        _api = api;
        _logger = logger;
    }

    /// <summary>
    /// Demonstrates parallel API calls with error handling
    /// </summary>
    [HttpGet("parallel-fetch")]
    public async Task<IActionResult> ParallelFetch(CancellationToken cancellationToken)
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();
            
            // Create multiple parallel tasks
            var tasks = new List<Task<object>>();
            
            // Fetch posts
            tasks.Add(Task.Run(async () => (object)await _api.GetPostsAsync(cancellationToken), cancellationToken));
            
            // Fetch users
            tasks.Add(Task.Run(async () => (object)await _api.GetUsersAsync(cancellationToken), cancellationToken));
            
            // Fetch specific posts
            for (int i = 1; i <= 5; i++)
            {
                int postId = i; // Capture variable for closure
                tasks.Add(Task.Run(async () => (object)await _api.GetPostByIdAsync(postId, cancellationToken), cancellationToken));
            }

            // Wait for all tasks to complete
            var results = await Task.WhenAll(tasks);
            
            stopwatch.Stop();

            return Ok(new
            {
                executionTime = stopwatch.ElapsedMilliseconds,
                posts = results[0],
                users = results[1],
                specificPosts = new[] { results[2], results[3], results[4], results[5], results[6] },
                message = "Successfully fetched data in parallel"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in parallel fetch");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Demonstrates batch operations with transaction-like behavior
    /// </summary>
    [HttpPost("batch-create")]
    public async Task<IActionResult> BatchCreate([FromBody] List<CreatePostRequest> requests, CancellationToken cancellationToken)
    {
        var createdPosts = new List<Post>();
        var errors = new List<object>();

        foreach (var request in requests)
        {
            try
            {
                var post = await _api.CreatePostAsync(request, cancellationToken);
                createdPosts.Add(post);
                _logger.LogInformation($"Created post: {post.Id}");
            }
            catch (ApiException ex)
            {
                errors.Add(new
                {
                    request = request,
                    error = ex.Message,
                    statusCode = ex.StatusCode
                });
                _logger.LogError(ex, $"Failed to create post: {request.Title}");
            }
        }

        return Ok(new
        {
            successful = createdPosts.Count,
            failed = errors.Count,
            createdPosts,
            errors
        });
    }

    /// <summary>
    /// Demonstrates retry and circuit breaker patterns
    /// </summary>
    [HttpGet("resilience-test/{id}")]
    public async Task<IActionResult> ResilienceTest(int id, CancellationToken cancellationToken)
    {
        var attempts = new List<string>();
        
        try
        {
            _logger.LogInformation($"Testing resilience for post ID: {id}");
            
            // This will use the Polly policies configured in Program.cs
            var post = await _api.GetPostByIdAsync(id, cancellationToken);
            
            return Ok(new
            {
                success = true,
                data = post,
                message = "Successfully retrieved post with resilience policies"
            });
        }
        catch (BrokenCircuitException ex)
        {
            _logger.LogError(ex, "Circuit breaker is open");
            return StatusCode(503, new
            {
                error = "Service temporarily unavailable",
                message = "Circuit breaker is open due to repeated failures",
                retryAfter = 30
            });
        }
        catch (ApiException ex)
        {
            return StatusCode((int)ex.StatusCode, new
            {
                error = ex.Message,
                statusCode = ex.StatusCode,
                content = ex.Content
            });
        }
    }

    /// <summary>
    /// Demonstrates caching with ETags
    /// </summary>
    [HttpGet("cached/{id}")]
    public async Task<IActionResult> GetWithCaching(int id, [FromHeader(Name = "If-None-Match")] string? etag, CancellationToken cancellationToken)
    {
        try
        {
            // Get the raw response to access headers
            var response = await _api.GetPostResponseAsync(id, cancellationToken);
            
            // Check ETag
            var responseEtag = response.Headers.ETag?.Tag;
            
            if (!string.IsNullOrEmpty(etag) && etag == responseEtag)
            {
                return StatusCode(304); // Not Modified
            }

            // Read content
            var content = await response.Content.ReadAsStringAsync();
            
            // Add ETag to response
            if (!string.IsNullOrEmpty(responseEtag))
            {
                Response.Headers.Add("ETag", responseEtag);
            }
            
            // Add cache control headers
            Response.Headers.Add("Cache-Control", "private, max-age=300");
            
            return Ok(content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting cached post {id}");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Demonstrates timeout handling
    /// </summary>
    [HttpGet("timeout-test")]
    public async Task<IActionResult> TimeoutTest(CancellationToken cancellationToken)
    {
        try
        {
            // Create a timeout token that expires in 2 seconds
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            var posts = await _api.GetPostsAsync(linkedCts.Token);
            return Ok(posts);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Request timed out");
            return StatusCode(408, new { error = "Request timeout" });
        }
    }

    /// <summary>
    /// Demonstrates complex query building
    /// </summary>
    [HttpGet("complex-query")]
    public async Task<IActionResult> ComplexQuery(
        [FromQuery] Dictionary<string, string> filters,
        CancellationToken cancellationToken)
    {
        try
        {
            // Build dynamic query parameters
            string? title = filters.GetValueOrDefault("title");
            string? body = filters.GetValueOrDefault("body");
            int? userId = filters.ContainsKey("userId") ? int.Parse(filters["userId"]) : null;
            string? sortBy = filters.GetValueOrDefault("sortBy", "id");
            string? order = filters.GetValueOrDefault("order", "asc");

            var posts = await _api.SearchPostsAsync(title, body, userId, sortBy, order, cancellationToken);

            return Ok(new
            {
                filters = filters,
                resultCount = posts.Count,
                data = posts
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in complex query");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Demonstrates error aggregation from multiple API calls
    /// </summary>
    [HttpGet("aggregate-data/{userId}")]
    public async Task<IActionResult> AggregateUserData(int userId, CancellationToken cancellationToken)
    {
        var errors = new List<string>();
        User? user = null;
        List<Post>? posts = null;
        var comments = new List<Comment>();

        // Fetch user
        try
        {
            user = await _api.GetUserAsync(userId, cancellationToken);
        }
        catch (Exception ex)
        {
            errors.Add($"Failed to fetch user: {ex.Message}");
            _logger.LogError(ex, "Failed to fetch user");
        }

        // Fetch user's posts
        try
        {
            posts = await _api.GetPostsByUserIdAsync(userId, cancellationToken);
            
            // Fetch comments for each post
            if (posts != null)
            {
                foreach (var post in posts.Take(5)) // Limit to first 5 posts
                {
                    try
                    {
                        var postComments = await _api.GetPostCommentsAsync(post.Id, cancellationToken);
                        comments.AddRange(postComments);
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Failed to fetch comments for post {post.Id}: {ex.Message}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            errors.Add($"Failed to fetch posts: {ex.Message}");
            _logger.LogError(ex, "Failed to fetch posts");
        }

        // Return aggregated data
        if (errors.Count > 0 && user == null && posts == null)
        {
            return StatusCode(500, new { errors });
        }

        return Ok(new
        {
            user,
            posts,
            comments,
            statistics = new
            {
                postCount = posts?.Count ?? 0,
                commentCount = comments.Count,
                averageCommentsPerPost = posts?.Count > 0 ? comments.Count / (double)posts.Count : 0
            },
            errors = errors.Count > 0 ? errors : null
        });
    }

    /// <summary>
    /// Demonstrates streaming response handling
    /// </summary>
    [HttpGet("stream")]
    public async Task<IActionResult> StreamData(CancellationToken cancellationToken)
    {
        Response.Headers.Add("Content-Type", "text/event-stream");
        Response.Headers.Add("Cache-Control", "no-cache");
        Response.Headers.Add("Connection", "keep-alive");

        for (int i = 1; i <= 5; i++)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                var post = await _api.GetPostByIdAsync(i, cancellationToken);
                var data = $"data: {System.Text.Json.JsonSerializer.Serialize(post)}\n\n";
                await Response.WriteAsync(data, cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
                
                // Small delay between chunks
                await Task.Delay(500, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error streaming post {i}");
            }
        }

        await Response.WriteAsync("data: [DONE]\n\n", cancellationToken);
        return new EmptyResult();
    }
}
