using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;
using Refit;
using RefitPoc.Handlers;
using RefitPoc.Services;
using System;
using System.Net.Http;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure HttpClient logging
builder.Services.AddLogging(config =>
{
    config.AddConsole();
    config.SetMinimumLevel(LogLevel.Information);
});

// Configure Refit with JSONPlaceholder API
builder.Services
    .AddRefitClient<IJsonPlaceholderApi>()
    .ConfigureHttpClient(c =>
    {
        c.BaseAddress = new Uri("https://jsonplaceholder.typicode.com");
        c.DefaultRequestHeaders.Add("Accept", "application/json");
        c.Timeout = TimeSpan.FromSeconds(30);
    })
    .AddPolicyHandler(GetRetryPolicy())
    .AddPolicyHandler(GetCircuitBreakerPolicy());

// Configure Refit with Authentication example
builder.Services.AddTransient<AuthHeaderHandler>();
builder.Services
    .AddRefitClient<IAuthenticatedApi>()
    .ConfigureHttpClient(c =>
    {
        c.BaseAddress = new Uri("https://api.example.com"); // Replace with actual API
        c.DefaultRequestHeaders.Add("Accept", "application/json");
    })
    .AddHttpMessageHandler<AuthHeaderHandler>()
    .AddPolicyHandler(GetRetryPolicy());

// Configure JSON serialization settings for Refit
builder.Services.AddControllers().AddNewtonsoftJson();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Test endpoint to verify the app is running
app.MapGet("/", () => "Refit POC - API Client Demo. Navigate to /swagger for API documentation- Parthiban");

app.Run();

// Polly retry policy - retry 3 times with exponential backoff
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
                Console.WriteLine($"Retry {retryCount} after {timespan} seconds");
            });
}

// Circuit breaker policy - break after 5 failures for 30 seconds
static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .CircuitBreakerAsync(
            5,
            TimeSpan.FromSeconds(30),
            onBreak: (result, timespan) =>
            {
                Console.WriteLine($"Circuit breaker opened for {timespan}");
            },
            onReset: () =>
            {
                Console.WriteLine("Circuit breaker reset");
            });
}
