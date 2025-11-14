namespace QES_KUKA_AMR_API.Services.Database;

/// <summary>
/// Service for initializing and seeding the database for production deployment
/// </summary>
public interface IDatabaseInitializationService
{
    /// <summary>
    /// Ensures database is created and migrated to latest version
    /// </summary>
    Task EnsureDatabaseCreatedAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all data from database tables (use for clean production deployment)
    /// </summary>
    Task ClearAllDataAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Seeds initial production data (if needed)
    /// </summary>
    Task SeedInitialDataAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Full reset: Clear data and reseed
    /// </summary>
    Task ResetDatabaseAsync(CancellationToken cancellationToken = default);
}
