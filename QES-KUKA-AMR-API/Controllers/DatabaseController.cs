using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QES_KUKA_AMR_API.Services.Database;

namespace QES_KUKA_AMR_API.Controllers;

/// <summary>
/// Database management endpoints for production deployment
/// WARNING: These endpoints should be secured or disabled in production!
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class DatabaseController : ControllerBase
{
    private readonly IDatabaseInitializationService _dbInitService;
    private readonly ILogger<DatabaseController> _logger;

    public DatabaseController(
        IDatabaseInitializationService dbInitService,
        ILogger<DatabaseController> logger)
    {
        _dbInitService = dbInitService;
        _logger = logger;
    }

    /// <summary>
    /// Clears all data from database tables
    /// WARNING: This will delete all data! Use only for clean production deployment.
    /// </summary>
    /// <param name="confirmationCode">Must provide "CLEAR_ALL_DATA" to confirm</param>
    [HttpPost("clear")]
    public async Task<IActionResult> ClearAllDataAsync(
        [FromQuery] string confirmationCode,
        CancellationToken cancellationToken)
    {
        if (confirmationCode != "CLEAR_ALL_DATA")
        {
            return BadRequest(new
            {
                success = false,
                message = "Confirmation code required. Pass ?confirmationCode=CLEAR_ALL_DATA to confirm"
            });
        }

        try
        {
            _logger.LogWarning("Database clear requested from {RemoteIp}", HttpContext.Connection.RemoteIpAddress);

            await _dbInitService.ClearAllDataAsync(cancellationToken);

            return Ok(new
            {
                success = true,
                message = "All data cleared successfully. Identity seeds have been reset."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing database");
            return StatusCode(500, new
            {
                success = false,
                message = $"Error clearing database: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Ensures database is created and migrated to latest version
    /// </summary>
    [HttpPost("migrate")]
    public async Task<IActionResult> MigrateDatabaseAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _dbInitService.EnsureDatabaseCreatedAsync(cancellationToken);

            return Ok(new
            {
                success = true,
                message = "Database migrated successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error migrating database");
            return StatusCode(500, new
            {
                success = false,
                message = $"Error migrating database: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Full database reset: Clear data and seed initial data
    /// WARNING: This will delete all data!
    /// </summary>
    /// <param name="confirmationCode">Must provide "RESET_DATABASE" to confirm</param>
    [HttpPost("reset")]
    public async Task<IActionResult> ResetDatabaseAsync(
        [FromQuery] string confirmationCode,
        CancellationToken cancellationToken)
    {
        if (confirmationCode != "RESET_DATABASE")
        {
            return BadRequest(new
            {
                success = false,
                message = "Confirmation code required. Pass ?confirmationCode=RESET_DATABASE to confirm"
            });
        }

        try
        {
            _logger.LogWarning("Database reset requested from {RemoteIp}", HttpContext.Connection.RemoteIpAddress);

            await _dbInitService.ResetDatabaseAsync(cancellationToken);

            return Ok(new
            {
                success = true,
                message = "Database reset successfully. All data cleared and reseeded."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting database");
            return StatusCode(500, new
            {
                success = false,
                message = $"Error resetting database: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Gets database status and row counts
    /// </summary>
    [HttpGet("status")]
    public async Task<IActionResult> GetDatabaseStatusAsync(CancellationToken cancellationToken)
    {
        try
        {
            // This is a safe read-only operation, no confirmation needed
            var dbContext = HttpContext.RequestServices
                .GetRequiredService<Data.ApplicationDbContext>();

            var status = new
            {
                WorkflowDiagrams = await dbContext.WorkflowDiagrams.CountAsync(cancellationToken)
            };

            return Ok(new
            {
                success = true,
                data = status
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting database status");
            return StatusCode(500, new
            {
                success = false,
                message = $"Error getting database status: {ex.Message}"
            });
        }
    }
}
