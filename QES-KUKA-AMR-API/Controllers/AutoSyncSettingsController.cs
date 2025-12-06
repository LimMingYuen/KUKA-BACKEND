using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QES_KUKA_AMR_API.Data;
using QES_KUKA_AMR_API.Data.Entities;
using QES_KUKA_AMR_API.Models.AutoSync;
using QES_KUKA_AMR_API.Services.Sync;

namespace QES_KUKA_AMR_API.Controllers;

/// <summary>
/// Controller for managing auto-sync settings
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/auto-sync-settings")]
public class AutoSyncSettingsController : ControllerBase
{
    // SystemSettings keys
    private const string KeyEnabled = "AutoSync.Enabled";
    private const string KeyIntervalMinutes = "AutoSync.IntervalMinutes";
    private const string KeySyncWorkflows = "AutoSync.Workflows";
    private const string KeySyncQrCodes = "AutoSync.QrCodes";
    private const string KeySyncMapZones = "AutoSync.MapZones";
    private const string KeySyncMobileRobots = "AutoSync.MobileRobots";
    private const string KeyLastRun = "AutoSync.LastRun";

    private readonly ApplicationDbContext _dbContext;
    private readonly ISyncService _syncService;
    private readonly ILogger<AutoSyncSettingsController> _logger;

    public AutoSyncSettingsController(
        ApplicationDbContext dbContext,
        ISyncService syncService,
        ILogger<AutoSyncSettingsController> logger)
    {
        _dbContext = dbContext;
        _syncService = syncService;
        _logger = logger;
    }

    /// <summary>
    /// Get current auto-sync settings
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<AutoSyncSettingsDto>> GetSettings(CancellationToken cancellationToken)
    {
        var settings = await _dbContext.SystemSettings
            .AsNoTracking()
            .Where(s => s.Key.StartsWith("AutoSync."))
            .ToDictionaryAsync(s => s.Key, s => s.Value, cancellationToken);

        var enabled = GetBoolSetting(settings, KeyEnabled, false);
        var intervalMinutes = GetIntSetting(settings, KeyIntervalMinutes, 60);
        var lastRun = GetDateTimeSetting(settings, KeyLastRun);

        var result = new AutoSyncSettingsDto
        {
            Enabled = enabled,
            IntervalMinutes = intervalMinutes,
            SyncWorkflows = GetBoolSetting(settings, KeySyncWorkflows, true),
            SyncQrCodes = GetBoolSetting(settings, KeySyncQrCodes, true),
            SyncMapZones = GetBoolSetting(settings, KeySyncMapZones, true),
            SyncMobileRobots = GetBoolSetting(settings, KeySyncMobileRobots, true),
            LastRun = lastRun,
            NextRun = enabled && lastRun.HasValue
                ? lastRun.Value.AddMinutes(intervalMinutes)
                : null
        };

        return Ok(result);
    }

    /// <summary>
    /// Update auto-sync settings
    /// </summary>
    [HttpPut]
    public async Task<ActionResult<AutoSyncSettingsDto>> UpdateSettings(
        [FromBody] AutoSyncSettingsUpdateRequest request,
        CancellationToken cancellationToken)
    {
        // Validate interval
        if (request.IntervalMinutes < 1 || request.IntervalMinutes > 1440)
        {
            return BadRequest(new { Message = "Interval must be between 1 and 1440 minutes (24 hours)." });
        }

        // Get all existing settings
        var existingSettings = await _dbContext.SystemSettings
            .Where(s => s.Key.StartsWith("AutoSync."))
            .ToDictionaryAsync(s => s.Key, s => s, cancellationToken);

        // Update or create each setting
        UpsertSetting(existingSettings, KeyEnabled, request.Enabled.ToString(), "Enable/disable auto-sync");
        UpsertSetting(existingSettings, KeyIntervalMinutes, request.IntervalMinutes.ToString(), "Sync interval in minutes");
        UpsertSetting(existingSettings, KeySyncWorkflows, request.SyncWorkflows.ToString(), "Sync workflows");
        UpsertSetting(existingSettings, KeySyncQrCodes, request.SyncQrCodes.ToString(), "Sync QR codes");
        UpsertSetting(existingSettings, KeySyncMapZones, request.SyncMapZones.ToString(), "Sync map zones");
        UpsertSetting(existingSettings, KeySyncMobileRobots, request.SyncMobileRobots.ToString(), "Sync mobile robots");

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "AutoSync settings updated: Enabled={Enabled}, Interval={Interval}min, Workflows={W}, QrCodes={Q}, MapZones={M}, MobileRobots={R}",
            request.Enabled, request.IntervalMinutes,
            request.SyncWorkflows, request.SyncQrCodes, request.SyncMapZones, request.SyncMobileRobots);

        // Return updated settings
        return await GetSettings(cancellationToken);
    }

    /// <summary>
    /// Trigger immediate sync of all enabled APIs
    /// </summary>
    [HttpPost("run-now")]
    public async Task<ActionResult<AutoSyncRunResultDto>> RunNow(CancellationToken cancellationToken)
    {
        // Get current settings to know which APIs to sync
        var settings = await _dbContext.SystemSettings
            .AsNoTracking()
            .Where(s => s.Key.StartsWith("AutoSync."))
            .ToDictionaryAsync(s => s.Key, s => s.Value, cancellationToken);

        var syncWorkflows = GetBoolSetting(settings, KeySyncWorkflows, true);
        var syncQrCodes = GetBoolSetting(settings, KeySyncQrCodes, true);
        var syncMapZones = GetBoolSetting(settings, KeySyncMapZones, true);
        var syncMobileRobots = GetBoolSetting(settings, KeySyncMobileRobots, true);

        _logger.LogInformation(
            "Manual sync triggered: Workflows={W}, QrCodes={Q}, MapZones={M}, MobileRobots={R}",
            syncWorkflows, syncQrCodes, syncMapZones, syncMobileRobots);

        var result = await _syncService.RunAllEnabledSyncsAsync(
            syncWorkflows,
            syncQrCodes,
            syncMapZones,
            syncMobileRobots,
            cancellationToken);

        // Update last run timestamp
        await UpdateLastRunAsync(cancellationToken);

        // Log results
        foreach (var syncResult in result.Results)
        {
            if (syncResult.Success)
            {
                _logger.LogInformation(
                    "Manual sync {ApiName}: {Total} total, {Inserted} inserted, {Updated} updated",
                    syncResult.ApiName, syncResult.Total, syncResult.Inserted, syncResult.Updated);
            }
            else
            {
                _logger.LogWarning(
                    "Manual sync {ApiName} failed: {Error}",
                    syncResult.ApiName, syncResult.ErrorMessage);
            }
        }

        return Ok(result);
    }

    /// <summary>
    /// Sync a specific API manually
    /// </summary>
    [HttpPost("run-now/{apiName}")]
    public async Task<ActionResult<SyncResultDto>> RunNowSingle(
        string apiName,
        CancellationToken cancellationToken)
    {
        SyncResultDto result;

        switch (apiName.ToLowerInvariant())
        {
            case "workflows":
                result = await _syncService.SyncWorkflowsAsync(cancellationToken);
                break;
            case "qrcodes":
                result = await _syncService.SyncQrCodesAsync(cancellationToken);
                break;
            case "mapzones":
                result = await _syncService.SyncMapZonesAsync(cancellationToken);
                break;
            case "mobilerobots":
                result = await _syncService.SyncMobileRobotsAsync(cancellationToken);
                break;
            default:
                return BadRequest(new { Message = $"Unknown API: {apiName}. Valid options: workflows, qrcodes, mapzones, mobilerobots" });
        }

        if (result.Success)
        {
            _logger.LogInformation(
                "Manual sync {ApiName}: {Total} total, {Inserted} inserted, {Updated} updated",
                result.ApiName, result.Total, result.Inserted, result.Updated);
        }
        else
        {
            _logger.LogWarning(
                "Manual sync {ApiName} failed: {Error}",
                result.ApiName, result.ErrorMessage);
        }

        return result.Success ? Ok(result) : StatusCode(StatusCodes.Status502BadGateway, result);
    }

    #region Helper Methods

    private void UpsertSetting(
        Dictionary<string, SystemSetting> existingSettings,
        string key,
        string value,
        string description)
    {
        if (existingSettings.TryGetValue(key, out var setting))
        {
            setting.Value = value;
            setting.LastUpdated = DateTime.UtcNow;
        }
        else
        {
            var newSetting = new SystemSetting
            {
                Key = key,
                Value = value,
                Description = description,
                LastUpdated = DateTime.UtcNow
            };
            _dbContext.SystemSettings.Add(newSetting);
            existingSettings[key] = newSetting;
        }
    }

    private async Task UpdateLastRunAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        // Try to update first (most common case)
        var updated = await _dbContext.SystemSettings
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
                _dbContext.SystemSettings.Add(new SystemSetting
                {
                    Key = KeyLastRun,
                    Value = now.ToString("o"),
                    Description = "Last auto-sync run timestamp",
                    LastUpdated = now
                });
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                // Race condition - another process inserted first, just update instead
                await _dbContext.SystemSettings
                    .Where(s => s.Key == KeyLastRun)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(p => p.Value, now.ToString("o"))
                        .SetProperty(p => p.LastUpdated, now),
                        cancellationToken);
            }
        }
    }

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
            return Math.Max(1, Math.Min(1440, result));
        }
        return defaultValue;
    }

    private static DateTime? GetDateTimeSetting(Dictionary<string, string> settings, string key)
    {
        if (settings.TryGetValue(key, out var value) && DateTime.TryParse(value, out var result))
        {
            return result;
        }
        return null;
    }

    #endregion
}
