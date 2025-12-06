using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QES_KUKA_AMR_API.Data;
using QES_KUKA_AMR_API.Data.Entities;

namespace QES_KUKA_AMR_API.Controllers;

[ApiController]
[Authorize]
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
    public async Task<ActionResult<IEnumerable<MissionHistoryResponseDto>>> GetAllHistoryAsync(CancellationToken cancellationToken)
    {
        try
        {
            var history = await _context.MissionHistories
                .OrderByDescending(m => m.CreatedDate)
                .ToListAsync(cancellationToken);

            // Transform to response DTOs with calculated duration
            var response = history.Select(record =>
            {
                // Ensure all DateTime values are treated as UTC
                var createdDate = record.CreatedDate.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(record.CreatedDate, DateTimeKind.Utc)
                    : record.CreatedDate;

                var processedDate = record.ProcessedDate.HasValue && record.ProcessedDate.Value.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(record.ProcessedDate.Value, DateTimeKind.Utc)
                    : record.ProcessedDate;

                var submittedDate = record.SubmittedToAmrDate.HasValue && record.SubmittedToAmrDate.Value.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(record.SubmittedToAmrDate.Value, DateTimeKind.Utc)
                    : record.SubmittedToAmrDate;

                var completedDate = record.CompletedDate.HasValue && record.CompletedDate.Value.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(record.CompletedDate.Value, DateTimeKind.Utc)
                    : record.CompletedDate;

                // Calculate duration (working time)
                double? durationMinutes = null;
                if (completedDate.HasValue)
                {
                    var startDate = processedDate ?? submittedDate ?? createdDate;
                    durationMinutes = (completedDate.Value - startDate).TotalMinutes;
                }

                return new MissionHistoryResponseDto
                {
                    Id = record.Id,
                    MissionCode = record.MissionCode,
                    RequestId = record.RequestId,
                    WorkflowId = record.WorkflowId,
                    WorkflowName = record.WorkflowName,
                    SavedMissionId = record.SavedMissionId,
                    TriggerSource = record.TriggerSource,
                    Status = record.Status,
                    MissionType = record.MissionType,
                    CreatedDate = createdDate,
                    ProcessedDate = processedDate,
                    SubmittedToAmrDate = submittedDate,
                    CompletedDate = completedDate,
                    AssignedRobotId = record.AssignedRobotId,
                    ErrorMessage = record.ErrorMessage,
                    CreatedBy = record.CreatedBy,
                    DurationMinutes = durationMinutes
                };
            }).ToList();

            return Ok(response);
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
                CreatedDate = DateTime.UtcNow,
                // Optional fields for analytics
                WorkflowId = request.WorkflowId,
                SavedMissionId = request.SavedMissionId,
                TriggerSource = request.TriggerSource ?? MissionTriggerSource.Manual,
                MissionType = request.MissionType,
                AssignedRobotId = request.AssignedRobotId,
                // Parse ISO date strings to DateTime
                ProcessedDate = TryParseDateTime(request.ProcessedDate),
                SubmittedToAmrDate = TryParseDateTime(request.SubmittedToAmrDate) ?? DateTime.UtcNow,
                CompletedDate = TryParseDateTime(request.CompletedDate),
                ErrorMessage = request.ErrorMessage,
                CreatedBy = request.CreatedBy
            };

            _context.MissionHistories.Add(missionHistory);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("✓ Mission history record added successfully: {MissionCode} - {Status} (ID={Id}), Robot={RobotId}",
                missionHistory.MissionCode, missionHistory.Status, missionHistory.Id, missionHistory.AssignedRobotId);
            _logger.LogInformation("=== END MissionHistoryController.AddHistoryAsync DEBUG ===");

            // Ensure DateTime values are treated as UTC when serialized
            // This adds the 'Z' suffix so JavaScript correctly interprets them as UTC
            if (missionHistory.CreatedDate.Kind == DateTimeKind.Unspecified)
                missionHistory.CreatedDate = DateTime.SpecifyKind(missionHistory.CreatedDate, DateTimeKind.Utc);
            if (missionHistory.ProcessedDate.HasValue && missionHistory.ProcessedDate.Value.Kind == DateTimeKind.Unspecified)
                missionHistory.ProcessedDate = DateTime.SpecifyKind(missionHistory.ProcessedDate.Value, DateTimeKind.Utc);
            if (missionHistory.SubmittedToAmrDate.HasValue && missionHistory.SubmittedToAmrDate.Value.Kind == DateTimeKind.Unspecified)
                missionHistory.SubmittedToAmrDate = DateTime.SpecifyKind(missionHistory.SubmittedToAmrDate.Value, DateTimeKind.Utc);
            if (missionHistory.CompletedDate.HasValue && missionHistory.CompletedDate.Value.Kind == DateTimeKind.Unspecified)
                missionHistory.CompletedDate = DateTime.SpecifyKind(missionHistory.CompletedDate.Value, DateTimeKind.Utc);

            return StatusCode(201, missionHistory);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "✗ Error adding mission history record");
            return StatusCode(500, new { message = "Error adding mission history record" });
        }
    }

    [HttpPut("{missionCode}")]
    public async Task<ActionResult<MissionHistory>> UpdateHistoryAsync(
        string missionCode,
        [FromBody] UpdateMissionHistoryRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("=== MissionHistoryController.UpdateHistoryAsync DEBUG ===");
            _logger.LogInformation("Updating mission history for MissionCode={MissionCode}, Status={Status}, Robot={RobotId}",
                missionCode, request.Status, request.AssignedRobotId);

            // Find existing record by mission code
            var missionHistory = await _context.MissionHistories
                .FirstOrDefaultAsync(m => m.MissionCode == missionCode, cancellationToken);

            if (missionHistory == null)
            {
                _logger.LogWarning("Mission history not found for MissionCode={MissionCode}", missionCode);
                return NotFound(new { message = $"Mission history not found for mission code: {missionCode}" });
            }

            // Update fields if provided
            if (request.Status != null)
                missionHistory.Status = request.Status;

            if (request.AssignedRobotId != null)
                missionHistory.AssignedRobotId = request.AssignedRobotId;

            if (request.ProcessedDate.HasValue)
                missionHistory.ProcessedDate = request.ProcessedDate.Value;

            if (request.SubmittedToAmrDate.HasValue)
                missionHistory.SubmittedToAmrDate = request.SubmittedToAmrDate.Value;

            if (request.CompletedDate.HasValue)
                missionHistory.CompletedDate = request.CompletedDate.Value;

            if (request.ErrorMessage != null)
                missionHistory.ErrorMessage = request.ErrorMessage;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("✓ Mission history updated successfully: {MissionCode} - {Status}, Robot={RobotId}, Completed={CompletedDate}",
                missionHistory.MissionCode, missionHistory.Status, missionHistory.AssignedRobotId, missionHistory.CompletedDate);
            _logger.LogInformation("=== END MissionHistoryController.UpdateHistoryAsync DEBUG ===");

            // Ensure DateTime values are treated as UTC when serialized
            if (missionHistory.CreatedDate.Kind == DateTimeKind.Unspecified)
                missionHistory.CreatedDate = DateTime.SpecifyKind(missionHistory.CreatedDate, DateTimeKind.Utc);
            if (missionHistory.ProcessedDate.HasValue && missionHistory.ProcessedDate.Value.Kind == DateTimeKind.Unspecified)
                missionHistory.ProcessedDate = DateTime.SpecifyKind(missionHistory.ProcessedDate.Value, DateTimeKind.Utc);
            if (missionHistory.SubmittedToAmrDate.HasValue && missionHistory.SubmittedToAmrDate.Value.Kind == DateTimeKind.Unspecified)
                missionHistory.SubmittedToAmrDate = DateTime.SpecifyKind(missionHistory.SubmittedToAmrDate.Value, DateTimeKind.Utc);
            if (missionHistory.CompletedDate.HasValue && missionHistory.CompletedDate.Value.Kind == DateTimeKind.Unspecified)
                missionHistory.CompletedDate = DateTime.SpecifyKind(missionHistory.CompletedDate.Value, DateTimeKind.Utc);

            return Ok(missionHistory);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "✗ Error updating mission history record");
            return StatusCode(500, new { message = "Error updating mission history record" });
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

    /// <summary>
    /// Helper method to safely parse ISO date strings to DateTime
    /// </summary>
    private DateTime? TryParseDateTime(string? dateString)
    {
        if (string.IsNullOrWhiteSpace(dateString))
            return null;

        if (DateTime.TryParse(dateString, out var result))
            return result;

        _logger.LogWarning("Failed to parse date string: {DateString}", dateString);
        return null;
    }
}

public class MissionHistoryRequest
{
    public string MissionCode { get; set; } = string.Empty;
    public string RequestId { get; set; } = string.Empty;
    public string WorkflowName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;

    // Additional fields for analytics and duration tracking
    public int? WorkflowId { get; set; }
    public int? SavedMissionId { get; set; }
    public MissionTriggerSource? TriggerSource { get; set; }
    public string? MissionType { get; set; }
    public string? AssignedRobotId { get; set; }
    // Date fields accept ISO string format from frontend (e.g., "2025-11-17T18:04:14.285Z")
    public string? ProcessedDate { get; set; }
    public string? SubmittedToAmrDate { get; set; }
    public string? CompletedDate { get; set; }
    public string? ErrorMessage { get; set; }
    public string? CreatedBy { get; set; }
}

public class UpdateMissionHistoryRequest
{
    public string? Status { get; set; }
    public string? AssignedRobotId { get; set; }
    public DateTime? ProcessedDate { get; set; }
    public DateTime? SubmittedToAmrDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public string? ErrorMessage { get; set; }
}

public class MissionHistoryResponseDto
{
    public int Id { get; set; }
    public string MissionCode { get; set; } = string.Empty;
    public string RequestId { get; set; } = string.Empty;
    public int? WorkflowId { get; set; }
    public string? WorkflowName { get; set; }
    public int? SavedMissionId { get; set; }
    public MissionTriggerSource TriggerSource { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? MissionType { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? ProcessedDate { get; set; }
    public DateTime? SubmittedToAmrDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public string? AssignedRobotId { get; set; }
    public string? ErrorMessage { get; set; }
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Mission duration in minutes (working time from start to completion)
    /// </summary>
    public double? DurationMinutes { get; set; }
}
