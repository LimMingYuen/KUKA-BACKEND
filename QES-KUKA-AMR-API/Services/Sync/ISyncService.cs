using QES_KUKA_AMR_API.Models.AutoSync;

namespace QES_KUKA_AMR_API.Services.Sync;

/// <summary>
/// Service interface for synchronizing data from external APIs
/// </summary>
public interface ISyncService
{
    /// <summary>
    /// Sync workflow templates from external API
    /// </summary>
    Task<SyncResultDto> SyncWorkflowsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sync QR codes/position markers from external API
    /// </summary>
    Task<SyncResultDto> SyncQrCodesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sync map zones from external API
    /// </summary>
    Task<SyncResultDto> SyncMapZonesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sync mobile robots from external API
    /// </summary>
    Task<SyncResultDto> SyncMobileRobotsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Run all enabled syncs based on settings
    /// </summary>
    Task<AutoSyncRunResultDto> RunAllEnabledSyncsAsync(
        bool syncWorkflows,
        bool syncQrCodes,
        bool syncMapZones,
        bool syncMobileRobots,
        CancellationToken cancellationToken = default);
}
