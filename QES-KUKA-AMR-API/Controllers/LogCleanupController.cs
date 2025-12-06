using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QES_KUKA_AMR_API.Data;
using QES_KUKA_AMR_API.Data.Entities;
using QES_KUKA_AMR_API.Services;

namespace QES_KUKA_AMR_API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class LogCleanupController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly LogCleanupService _cleanupService;
    private readonly ILogger<LogCleanupController> _logger;


    public LogCleanupController(
        ApplicationDbContext dbContext,
        LogCleanupService cleanupService,
        ILogger<LogCleanupController> logger)
    {
        _dbContext = dbContext;
        _cleanupService = cleanupService;
        _logger = logger;
    }


    [HttpPost("run")]
    public async Task<IActionResult> RunCleanupAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Manual log cleanup triggered via API.");

            await _cleanupService.CleanOldLogsAsync();

            return Ok(new
            {
                Success = true,
                Message = "Log cleanup executed successfully."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing log cleanup.");
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                Success = false,
                Message = "Error executing log cleanup.",
                Exception = ex.Message
            });
        }
    }

    [HttpGet("setting")]
    public async Task<IActionResult> GetSettingAsync(CancellationToken cancellationToken)
    {
        var setting = await _dbContext.SystemSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Key == "LogRetentionMonths", cancellationToken);

        var months = 1;
        if (setting != null && int.TryParse(setting.Value, out var parsed))
        {
            months = parsed;
        }

        return Ok(new
        {
            Key = "LogRetentionMonths",
            Value = months
        });
    }

    /// <summary>
    /// Update log retention setting (months).
    /// </summary>
    [HttpPost("setting")]
    public async Task<IActionResult> UpdateSettingAsync([FromBody] LogRetentionUpdateRequest request, CancellationToken cancellationToken)
    {
        if (request.RetentionMonths < 1 || request.RetentionMonths > 12)
        {
            return BadRequest(new
            {
                Success = false,
                Message = "RetentionMonths must be between 1 and 12."
            });
        }

        var setting = await _dbContext.SystemSettings
            .FirstOrDefaultAsync(s => s.Key == "LogRetentionMonths", cancellationToken);

        if (setting == null)
        {
            setting = new SystemSetting
            {
                Key = "LogRetentionMonths",
                Value = request.RetentionMonths.ToString(),
                Description = "Number of months to retain logs."
            };
            _dbContext.SystemSettings.Add(setting);
        }
        else
        {
            setting.Value = request.RetentionMonths.ToString();
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated log retention months to {Months}", request.RetentionMonths);

        return Ok(new
        {
            Success = true,
            Message = $"Log retention updated to {request.RetentionMonths} month(s)."
        });
    }

    /// <summary>
    /// Test endpoint: list all log folders currently in the system.
    /// </summary>
    [HttpGet("folders")]
    public IActionResult ListLogFolders()
    {
        var logRootPath = Path.Combine(AppContext.BaseDirectory, "Logs");
        if (!Directory.Exists(logRootPath))
        {
            return Ok(new
            {
                Success = true,
                Folders = Array.Empty<string>(),
                Message = "No log directory found."
            });
        }

        var folders = Directory.GetDirectories(logRootPath)
            .Select(Path.GetFileName)
            .Where(n => !string.IsNullOrEmpty(n))
            .OrderBy(n => n)
            .ToList();

        return Ok(new
        {
            Success = true,
            Count = folders.Count,
            Folders = folders
        });
    }
}

/// <summary>
/// DTO for updating log retention months.
/// </summary>
public class LogRetentionUpdateRequest
{
    public int RetentionMonths { get; set; }
}
