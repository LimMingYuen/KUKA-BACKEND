using Microsoft.EntityFrameworkCore;
using QES_KUKA_AMR_API.Data;
using QES_KUKA_AMR_API.Data.Entities;
using QES_KUKA_AMR_API.Models.AutoSync;

namespace QES_KUKA_AMR_API.Services.Sync;

/// <summary>
/// Background service for automatic synchronization of external APIs
/// </summary>
public class AutoSyncHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AutoSyncHostedService> _logger;

    // SystemSettings keys
    private const string KeyEnabled = "AutoSync.Enabled";
    private const string KeyIntervalMinutes = "AutoSync.IntervalMinutes";
    private const string KeySyncWorkflows = "AutoSync.Workflows";
    private const string KeySyncQrCodes = "AutoSync.QrCodes";
    private const string KeySyncMapZones = "AutoSync.MapZones";
    private const string KeySyncMobileRobots = "AutoSync.MobileRobots";
    private const string KeyLastRun = "AutoSync.LastRun";

    // Default values
    private const int DefaultIntervalMinutes = 60;
    private const int CheckIntervalSeconds = 60; // Check settings every minute when disabled

    public AutoSyncHostedService(
        IServiceProvider serviceProvider,
        ILogger<AutoSyncHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AutoSync background service starting...");

        // Wait a bit for application to fully start
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var settings = await GetSettingsAsync(scope, stoppingToken);

                if (settings.Enabled)
                {
                    _logger.LogInformation("AutoSync is enabled. Running sync cycle...");
                    await RunSyncCycleAsync(scope, settings, stoppingToken);
                    await UpdateLastRunAsync(scope, stoppingToken);

                    // Wait for configured interval
                    _logger.LogInformation("AutoSync cycle complete. Next run in {Minutes} minutes.", settings.IntervalMinutes);
                    await Task.Delay(TimeSpan.FromMinutes(settings.IntervalMinutes), stoppingToken);
                }
                else
                {
                    // When disabled, check settings periodically
                    await Task.Delay(TimeSpan.FromSeconds(CheckIntervalSeconds), stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when stopping
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AutoSync background service cycle");
                // Wait a bit before retrying to avoid tight error loops
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        _logger.LogInformation("AutoSync background service stopped.");
    }

    private async Task<AutoSyncSettingsDto> GetSettingsAsync(IServiceScope scope, CancellationToken cancellationToken)
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var settings = await dbContext.SystemSettings
            .AsNoTracking()
            .Where(s => s.Key.StartsWith("AutoSync."))
            .ToDictionaryAsync(s => s.Key, s => s.Value, cancellationToken);

        return new AutoSyncSettingsDto
        {
            Enabled = GetBoolSetting(settings, KeyEnabled, false),
            IntervalMinutes = GetIntSetting(settings, KeyIntervalMinutes, DefaultIntervalMinutes),
            SyncWorkflows = GetBoolSetting(settings, KeySyncWorkflows, true),
            SyncQrCodes = GetBoolSetting(settings, KeySyncQrCodes, true),
            SyncMapZones = GetBoolSetting(settings, KeySyncMapZones, true),
            SyncMobileRobots = GetBoolSetting(settings, KeySyncMobileRobots, true),
            LastRun = GetDateTimeSetting(settings, KeyLastRun)
        };
    }

    private async Task RunSyncCycleAsync(IServiceScope scope, AutoSyncSettingsDto settings, CancellationToken cancellationToken)
    {
        var syncService = scope.ServiceProvider.GetRequiredService<ISyncService>();

        var result = await syncService.RunAllEnabledSyncsAsync(
            settings.SyncWorkflows,
            settings.SyncQrCodes,
            settings.SyncMapZones,
            settings.SyncMobileRobots,
            cancellationToken);

        // Log results
        foreach (var syncResult in result.Results)
        {
            if (syncResult.Success)
            {
                _logger.LogInformation(
                    "AutoSync {ApiName}: {Total} total, {Inserted} inserted, {Updated} updated",
                    syncResult.ApiName, syncResult.Total, syncResult.Inserted, syncResult.Updated);
            }
            else
            {
                _logger.LogWarning(
                    "AutoSync {ApiName} failed: {Error}",
                    syncResult.ApiName, syncResult.ErrorMessage);
            }
        }

        _logger.LogInformation(
            "AutoSync cycle completed: {Successful}/{Total} APIs successful",
            result.SuccessfulApis, result.TotalApis);
    }

    private async Task UpdateLastRunAsync(IServiceScope scope, CancellationToken cancellationToken)
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var now = DateTime.UtcNow;

        // Try to update first (most common case)
        var updated = await dbContext.SystemSettings
            .Where(s => s.Key == KeyLastRun)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.Value, now.ToString("o"))
                .SetProperty(p => p.LastUpdated, now),
                cancellationToken);

        // If no rows updated, insert new setting
        if (updated == 0)
        {
            try
            {
                dbContext.SystemSettings.Add(new SystemSetting
                {
                    Key = KeyLastRun,
                    Value = now.ToString("o"),
                    Description = "Last auto-sync run timestamp",
                    LastUpdated = now
                });
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                // Race condition - another process inserted first, just update instead
                await dbContext.SystemSettings
                    .Where(s => s.Key == KeyLastRun)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(p => p.Value, now.ToString("o"))
                        .SetProperty(p => p.LastUpdated, now),
                        cancellationToken);
            }
        }
    }

    #region Helper Methods

    private static bool GetBoolSetting(Dictionary<string, string> settings, string key, bool defaultValue)
    {
        if (settings.TryGetValue(key, out var value) && bool.TryParse(value, out var result))
        {
            return result;
        }
        return defaultValue;
    }

    private static int GetIntSetting(Dictionary<string, string> settings, string key, int defaultValue)
    {
        if (settings.TryGetValue(key, out var value) && int.TryParse(value, out var result))
        {
            return Math.Max(1, Math.Min(1440, result)); // Clamp between 1 min and 24 hours
        }
        return defaultValue;
    }

    private static DateTime? GetDateTimeSetting(Dictionary<string, string> settings, string key)
    {
        if (settings.TryGetValue(key, out var value) &&
            DateTime.TryParse(value, null, System.Globalization.DateTimeStyles.RoundtripKind, out var result))
        {
            // Ensure UTC kind is preserved for proper timezone handling
            return result.Kind == DateTimeKind.Utc ? result : result.ToUniversalTime();
        }
        return null;
    }

    #endregion
}
