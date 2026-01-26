using IntervalGenerator.Api.Models;
using Microsoft.Extensions.Options;

namespace IntervalGenerator.Api.Authentication;

/// <summary>
/// Middleware for API key authentication matching Electralink's authentication scheme.
/// </summary>
public class ApiKeyAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiKeyAuthenticationMiddleware> _logger;

    private const string ApiKeyHeader = "X-Api-Key";
    private const string ApiPasswordHeader = "X-Api-Password";

    public ApiKeyAuthenticationMiddleware(RequestDelegate next, ILogger<ApiKeyAuthenticationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IOptions<ApiSettings> settings)
    {
        var authSettings = settings.Value.Authentication;

        // Skip authentication if disabled
        if (!authSettings.Enabled)
        {
            await _next(context);
            return;
        }

        // Skip authentication for health check and swagger endpoints
        var path = context.Request.Path.Value ?? "";
        if (path.StartsWith("/health", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase) ||
            path.Equals("/", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        // Get credentials from headers
        var apiKey = context.Request.Headers[ApiKeyHeader].FirstOrDefault();
        var apiPassword = context.Request.Headers[ApiPasswordHeader].FirstOrDefault();

        if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiPassword))
        {
            _logger.LogWarning("Missing API credentials for request to {Path}", context.Request.Path);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new ErrorResponse
            {
                Error = "Unauthorized",
                Message = "Missing API key or password",
                Status = 401
            });
            return;
        }

        // Validate credentials
        if (!ValidateCredentials(apiKey, apiPassword, authSettings))
        {
            _logger.LogWarning("Invalid API credentials for request to {Path}", context.Request.Path);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new ErrorResponse
            {
                Error = "Unauthorized",
                Message = "Invalid API key or password",
                Status = 401
            });
            return;
        }

        await _next(context);
    }

    private static bool ValidateCredentials(string apiKey, string apiPassword, AuthenticationSettings settings)
    {
        return string.Equals(apiKey, settings.ApiKey, StringComparison.Ordinal) &&
               string.Equals(apiPassword, settings.ApiPassword, StringComparison.Ordinal);
    }
}

/// <summary>
/// Extension methods for API key authentication.
/// </summary>
public static class ApiKeyAuthenticationExtensions
{
    public static IApplicationBuilder UseApiKeyAuthentication(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ApiKeyAuthenticationMiddleware>();
    }
}
