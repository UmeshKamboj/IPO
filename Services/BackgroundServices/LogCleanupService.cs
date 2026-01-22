using IPOClient.Data;
using Microsoft.EntityFrameworkCore;

namespace IPOClient.Services.BackgroundServices
{
    public class LogCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<LogCleanupService> _logger;

        public LogCleanupService(IServiceProvider serviceProvider, ILogger<LogCleanupService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Log Cleanup Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CleanupOldLogsAsync();

                    // Run cleanup every hour
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while cleaning up logs");

                    // Wait 5 minutes before retrying on error
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
            }

            _logger.LogInformation("Log Cleanup Service stopped");
        }

        private async Task CleanupOldLogsAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<IPOClientDbContext>();

            // Delete logs older than 24 hours
            var cutoffTime = DateTime.UtcNow.AddHours(-24);

            var logsToDelete = await dbContext.IPO_ApiLogs
                .Where(log => log.RequestTime < cutoffTime)
                .ToListAsync();

            if (logsToDelete.Any())
            {
                dbContext.IPO_ApiLogs.RemoveRange(logsToDelete);
                await dbContext.SaveChangesAsync();

                _logger.LogInformation($"Cleaned up {logsToDelete.Count} API logs older than 24 hours");
            }
            else
            {
                _logger.LogInformation("No API logs to clean up");
            }
        }
    }
}
