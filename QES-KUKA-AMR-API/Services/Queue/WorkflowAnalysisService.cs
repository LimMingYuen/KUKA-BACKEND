using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using QES_KUKA_AMR_API.Data;
using QES_KUKA_AMR_API.Models.Missions;

namespace QES_KUKA_AMR_API.Services.Queue;

/// <summary>
/// Analyzes mission workflows to detect MapCode segments and coordinates
/// </summary>
public interface IWorkflowAnalysisService
{
    /// <summary>
    /// Analyzes mission steps and groups them by MapCode segments
    /// </summary>
    Task<WorkflowAnalysisResult> AnalyzeMissionStepsAsync(
        List<MissionDataItem> missionSteps,
        CancellationToken cancellationToken = default);
}

public class WorkflowAnalysisService : IWorkflowAnalysisService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<WorkflowAnalysisService> _logger;

    public WorkflowAnalysisService(
        ApplicationDbContext dbContext,
        ILogger<WorkflowAnalysisService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<WorkflowAnalysisResult> AnalyzeMissionStepsAsync(
        List<MissionDataItem> missionSteps,
        CancellationToken cancellationToken = default)
    {
        if (missionSteps == null || !missionSteps.Any())
        {
            throw new ArgumentException("Mission steps cannot be null or empty", nameof(missionSteps));
        }

        // Sort steps by sequence to ensure correct order
        var sortedSteps = missionSteps.OrderBy(s => s.Sequence).ToList();

        // Get all unique positions (node labels)
        var positions = sortedSteps.Select(s => s.Position).Distinct().ToList();

        // Look up QR codes for all positions to get their MapCodes and coordinates
        var qrCodeLookup = await _dbContext.QrCodes
            .Where(q => positions.Contains(q.NodeLabel))
            .Select(q => new
            {
                q.NodeLabel,
                q.MapCode,
                q.XCoordinate,
                q.YCoordinate
            })
            .ToListAsync(cancellationToken);

        // Create lookup dictionary for quick access
        var qrCodeDict = qrCodeLookup
            .GroupBy(q => q.NodeLabel)
            .ToDictionary(
                g => g.Key,
                g => g.First() // If multiple QR codes with same NodeLabel, take first
            );

        // Check for missing QR codes
        var missingPositions = positions.Where(p => !qrCodeDict.ContainsKey(p)).ToList();
        if (missingPositions.Any())
        {
            _logger.LogWarning(
                "Missing QR code data for positions: {Positions}. These steps will use null MapCode.",
                string.Join(", ", missingPositions)
            );
        }

        // Group steps into segments by MapCode
        var segments = new List<MapCodeSegment>();
        MapCodeSegment? currentSegment = null;

        foreach (var step in sortedSteps)
        {
            var qrData = qrCodeDict.GetValueOrDefault(step.Position);
            var mapCode = qrData?.MapCode;

            // Start new segment if MapCode changes or it's the first step
            if (currentSegment == null || currentSegment.MapCode != mapCode)
            {
                currentSegment = new MapCodeSegment
                {
                    MapCode = mapCode ?? "UNKNOWN",
                    Steps = new List<MissionDataItem>(),
                    SegmentIndex = segments.Count
                };
                segments.Add(currentSegment);
            }

            currentSegment.Steps.Add(step);

            // Set first node coordinates for this segment
            if (currentSegment.Steps.Count == 1 && qrData != null)
            {
                currentSegment.StartNodeLabel = step.Position;
                currentSegment.StartXCoordinate = qrData.XCoordinate;
                currentSegment.StartYCoordinate = qrData.YCoordinate;
            }

            // Always update last node (overwrites until we reach actual last step)
            if (qrData != null)
            {
                currentSegment.EndNodeLabel = step.Position;
                currentSegment.EndXCoordinate = qrData.XCoordinate;
                currentSegment.EndYCoordinate = qrData.YCoordinate;
            }
        }

        var isMultiMap = segments.Count > 1;

        _logger.LogInformation(
            "Workflow analysis complete: {SegmentCount} segment(s), IsMultiMap={IsMultiMap}, MapCodes=[{MapCodes}]",
            segments.Count,
            isMultiMap,
            string.Join(", ", segments.Select(s => s.MapCode))
        );

        return new WorkflowAnalysisResult
        {
            IsMultiMap = isMultiMap,
            Segments = segments,
            TotalSteps = sortedSteps.Count
        };
    }
}

/// <summary>
/// Result of workflow analysis
/// </summary>
public class WorkflowAnalysisResult
{
    /// <summary>
    /// True if workflow spans multiple MapCodes
    /// </summary>
    public bool IsMultiMap { get; set; }

    /// <summary>
    /// MapCode segments in execution order
    /// </summary>
    public List<MapCodeSegment> Segments { get; set; } = new();

    /// <summary>
    /// Total number of steps across all segments
    /// </summary>
    public int TotalSteps { get; set; }
}

/// <summary>
/// Represents a contiguous sequence of steps on the same MapCode
/// </summary>
public class MapCodeSegment
{
    /// <summary>
    /// The MapCode for this segment
    /// </summary>
    public string MapCode { get; set; } = string.Empty;

    /// <summary>
    /// Zero-based index of this segment in the workflow
    /// </summary>
    public int SegmentIndex { get; set; }

    /// <summary>
    /// Mission steps for this segment
    /// </summary>
    public List<MissionDataItem> Steps { get; set; } = new();

    /// <summary>
    /// First node label in this segment
    /// </summary>
    public string? StartNodeLabel { get; set; }

    /// <summary>
    /// X coordinate of first node
    /// </summary>
    public double? StartXCoordinate { get; set; }

    /// <summary>
    /// Y coordinate of first node
    /// </summary>
    public double? StartYCoordinate { get; set; }

    /// <summary>
    /// Last node label in this segment
    /// </summary>
    public string? EndNodeLabel { get; set; }

    /// <summary>
    /// X coordinate of last node
    /// </summary>
    public double? EndXCoordinate { get; set; }

    /// <summary>
    /// Y coordinate of last node
    /// </summary>
    public double? EndYCoordinate { get; set; }
}
