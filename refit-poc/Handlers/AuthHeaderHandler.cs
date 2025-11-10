using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace RefitPoc.Handlers;

/// <summary>
/// Custom HTTP message handler for adding authentication headers
/// </summary>
public class AuthHeaderHandler : DelegatingHandler
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthHeaderHandler> _logger;

    public AuthHeaderHandler(IConfiguration configuration, ILogger<AuthHeaderHandler> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Get token from configuration or token service
        var token = _configuration["ApiSettings:AuthToken"] ?? "demo-token-12345";

        // Add Bearer token to request
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Log the request
        _logger.LogInformation($"Sending request to {request.RequestUri}");
        _logger.LogDebug($"Request headers: {request.Headers}");

        try
        {
            // Send the request
            var response = await base.SendAsync(request, cancellationToken);

            // Log the response
            _logger.LogInformation($"Received response: {response.StatusCode}");

            // Handle token refresh if needed
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _logger.LogWarning("Received 401 Unauthorized. Token may be expired.");
                // Implement token refresh logic here if needed
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error sending request to {request.RequestUri}");
            throw;
        }
    }
}

/// <summary>
/// Alternative handler for API key authentication
/// </summary>
public class ApiKeyHandler : DelegatingHandler
{
    private readonly string _apiKey;
    private readonly ILogger<ApiKeyHandler> _logger;

    public ApiKeyHandler(IConfiguration configuration, ILogger<ApiKeyHandler> logger)
    {
        _apiKey = configuration["ApiSettings:ApiKey"] ?? "default-api-key";
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Add API key to request headers
        request.Headers.Add("X-API-Key", _apiKey);

        _logger.LogDebug($"Added API key to request for {request.RequestUri}");

        return await base.SendAsync(request, cancellationToken);
    }
}
