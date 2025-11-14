using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace QES_KUKA_AMR_API.Services
{

    public class LogCleanupHostedService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<LogCleanupHostedService> _logger;

        public LogCleanupHostedService(IServiceProvider serviceProvider, ILogger<LogCleanupHostedService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🧹 LogCleanupHostedService started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var cleaner = scope.ServiceProvider.GetRequiredService<LogCleanupService>();
                        await cleaner.CleanOldLogsAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during scheduled log cleanup.");
                }

                var delay = TimeSpan.FromDays(1);
                _logger.LogInformation("Next cleanup scheduled after {Hours} hours.", delay.TotalHours);

                try
                {
                    await Task.Delay(delay, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    break; 
                }
            }

            _logger.LogInformation("🧹 LogCleanupHostedService stopped.");
        }
    }
}
