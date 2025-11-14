using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QES_KUKA_AMR_API.Data;
using QES_KUKA_AMR_API.Data.Entities;

namespace QES_KUKA_AMR_API.Controllers;

[ApiController]
[Route("api/mission-history")]
public class MissionHistoryController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<MissionHistoryController> _logger;
    private const int MAX_RECORDS = 5000;

    public MissionHistoryController(
        ApplicationDbContext context,
        ILogger<MissionHistoryController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<MissionHistory>>> GetAllHistoryAsync(CancellationToken cancellationToken)
    {
        try
        {
            var history = await _context.MissionHistories
                .OrderByDescending(m => m.CreatedDate)
                .ToListAsync(cancellationToken);

            return Ok(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving mission history");
            return StatusCode(500, new { message = "Error retrieving mission history" });
        }
    }

    [HttpPost]
    public async Task<ActionResult<MissionHistory>> AddHistoryAsync(
        [FromBody] MissionHistoryRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("=== MissionHistoryController.AddHistoryAsync DEBUG ===");
            _logger.LogInformation("Received mission history request - MissionCode={MissionCode}, WorkflowName={WorkflowName}, Status={Status}, RequestId={RequestId}",
                request.MissionCode, request.WorkflowName, request.Status, request.RequestId);

            // Check if we need to clear the table
            var currentCount = await _context.MissionHistories.CountAsync(cancellationToken);
            _logger.LogInformation("Current mission history count: {Count}/{MaxRecords}", currentCount, MAX_RECORDS);

            if (currentCount >= MAX_RECORDS)
            {
                _logger.LogInformation("Mission history table reached {MaxRecords} records. Clearing all records.", MAX_RECORDS);

                // Delete all records
                _context.MissionHistories.RemoveRange(_context.MissionHistories);
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Mission history table cleared");
            }

            // Add new record
            var missionHistory = new MissionHistory
            {
                MissionCode = request.MissionCode,
                RequestId = request.RequestId,
                WorkflowName = request.WorkflowName,
                Status = request.Status,
                CreatedDate = DateTime.UtcNow
            };

            _context.MissionHistories.Add(missionHistory);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("✓ Mission history record added successfully: {MissionCode} - {Status} (ID={Id})",
                missionHistory.MissionCode, missionHistory.Status, missionHistory.Id);
            _logger.LogInformation("=== END MissionHistoryController.AddHistoryAsync DEBUG ===");

            return StatusCode(201, missionHistory);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "✗ Error adding mission history record");
            return StatusCode(500, new { message = "Error adding mission history record" });
        }
    }

    [HttpDelete("clear")]
    public async Task<IActionResult> ClearAllHistoryAsync(CancellationToken cancellationToken)
    {
        try
        {
            _context.MissionHistories.RemoveRange(_context.MissionHistories);
            var deletedCount = await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Mission history cleared. {Count} records deleted.", deletedCount);

            return Ok(new { message = $"{deletedCount} records deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing mission history");
            return StatusCode(500, new { message = "Error clearing mission history" });
        }
    }

    [HttpGet("count")]
    public async Task<ActionResult<int>> GetCountAsync(CancellationToken cancellationToken)
    {
        try
        {
            var count = await _context.MissionHistories.CountAsync(cancellationToken);
            return Ok(new { count, maxRecords = MAX_RECORDS });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting mission history count");
            return StatusCode(500, new { message = "Error getting count" });
        }
    }
}

public class MissionHistoryRequest
{
    public string MissionCode { get; set; } = string.Empty;
    public string RequestId { get; set; } = string.Empty;
    public string WorkflowName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
