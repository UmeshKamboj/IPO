using IPOClient.Data;
using IPOClient.Models.Entities;
using System.Diagnostics;
using System.Security.Claims;

namespace IPOClient.Middleware
{
    /// <summary>
    /// Lightweight API logging middleware that only logs errors (4xx, 5xx status codes)
    /// to minimize performance impact and database storage
    /// </summary>
    public class ApiLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ApiLoggingMiddleware> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public ApiLoggingMiddleware(
            RequestDelegate next,
            ILogger<ApiLoggingMiddleware> logger,
            IServiceScopeFactory scopeFactory)
        {
            _next = next;
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Skip logging for certain paths (like Swagger, static files, health checks)
            if (context.Request.Path.StartsWithSegments("/swagger") ||
                context.Request.Path.StartsWithSegments("/_framework") ||
                context.Request.Path.StartsWithSegments("/health"))
            {
                await _next(context);
                return;
            }

            var stopwatch = Stopwatch.StartNew();
            var statusCode = 200;
            Exception? caughtException = null;

            try
            {
                await _next(context);
                statusCode = context.Response.StatusCode;
            }
            catch (Exception ex)
            {
                statusCode = 500;
                caughtException = ex;
                _logger.LogError(ex, "API Exception: {Method} {Path}", context.Request.Method, context.Request.Path);
                throw;
            }
            finally
            {
                stopwatch.Stop();

                // Only log errors (4xx, 5xx) to minimize performance impact
                if (statusCode >= 400)
                {
                    var logLevel = statusCode >= 500 ? LogLevel.Error : LogLevel.Warning;
                    _logger.Log(logLevel,
                        "API Error: {Method} {Path} - Status: {StatusCode} - Duration: {Duration}ms",
                        context.Request.Method,
                        context.Request.Path,
                        statusCode,
                        stopwatch.ElapsedMilliseconds);

                    // Save error log to database asynchronously (fire and forget)
                    _ = SaveErrorLogAsync(context, statusCode, stopwatch.ElapsedMilliseconds, caughtException);
                }
            }
        }

        private async Task SaveErrorLogAsync(
            HttpContext context,
            int statusCode,
            long durationMs,
            Exception? exception)
        {
            try
            {
                // Use a new scope to avoid DbContext threading issues
                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<IPOClientDbContext>();

                var apiLog = new IPO_ApiLog
                {
                    Method = context.Request.Method,
                    Path = context.Request.Path,
                    QueryString = context.Request.QueryString.ToString(),
                    StatusCode = statusCode,
                    IpAddress = context.Connection.RemoteIpAddress?.ToString(),
                    RequestTime = DateTime.UtcNow,
                    DurationMs = durationMs,
                    ErrorMessage = exception?.Message ?? $"HTTP {statusCode}"
                };

                // Get UserId from claims if authenticated
                if (context.User.Identity?.IsAuthenticated == true)
                {
                    var userIdClaim = context.User.FindFirst("sub")?.Value;
                    if (int.TryParse(userIdClaim, out var userId))
                    {
                        apiLog.UserId = userId;
                    }
                }

                await dbContext.IPO_ApiLogs.AddAsync(apiLog);
                await dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Don't throw - just log the failure to avoid impacting the API
                _logger.LogError(ex, "Failed to save error log to database");
            }
        }
    }
}
