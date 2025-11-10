using Refit;
using RefitPoc.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RefitPoc.Services;

/// <summary>
/// Example of an authenticated API interface
/// Demonstrates authorization headers and secured endpoints
/// </summary>
[Headers("Authorization: Bearer")]
public interface IAuthenticatedApi
{
    [Get("/api/protected/resource")]
    Task<object> GetProtectedResourceAsync(CancellationToken cancellationToken = default);

    [Post("/api/protected/data")]
    Task<object> PostProtectedDataAsync([Body] object data, CancellationToken cancellationToken = default);

    // Override authorization for specific endpoint
    [Headers("Authorization: ApiKey")]
    [Get("/api/different-auth")]
    Task<object> GetWithDifferentAuthAsync(CancellationToken cancellationToken = default);

    // Dynamic authorization
    [Get("/api/dynamic-auth")]
    Task<object> GetWithDynamicAuthAsync(
        [Authorize("Bearer")] string token,
        CancellationToken cancellationToken = default);
}
