using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using QES_KUKA_AMR_API.Data;
using QES_KUKA_AMR_API.Data.Entities;
using QES_KUKA_AMR_API.Models.MapImport;

namespace QES_KUKA_AMR_API.Services.MapImport;

public interface IMapImportService
{
    Task<MapImportResponse> ImportFromJsonFileAsync(string filePath, bool overwriteExisting, CancellationToken cancellationToken = default);
    Task<MapDataDto?> ParseMapJsonFileAsync(string filePath, CancellationToken cancellationToken = default);
}

public class MapImportService : IMapImportService
{
    private readonly ApplicationDbContext _context;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<MapImportService> _logger;

    public MapImportService(
        ApplicationDbContext context,
        TimeProvider timeProvider,
        ILogger<MapImportService> logger)
    {
        _context = context;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <summary>
    /// Parse map JSON file and return structured data
    /// </summary>
    public async Task<MapDataDto?> ParseMapJsonFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                _logger.LogWarning("Map file not found: {FilePath}", filePath);
                return null;
            }

            var jsonContent = await File.ReadAllTextAsync(filePath, cancellationToken);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var mapData = JsonSerializer.Deserialize<MapDataDto>(jsonContent, options);
            return mapData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing map JSON file: {FilePath}", filePath);
            return null;
        }
    }

    /// <summary>
    /// Import QR codes from map JSON file into the database
    /// </summary>
    public async Task<MapImportResponse> ImportFromJsonFileAsync(
        string filePath,
        bool overwriteExisting,
        CancellationToken cancellationToken = default)
    {
        var response = new MapImportResponse
        {
            Stats = new MapImportStats
            {
                ImportedAt = _timeProvider.GetUtcNow().UtcDateTime
            }
        };

        try
        {
            // Parse the JSON file
            var mapData = await ParseMapJsonFileAsync(filePath, cancellationToken);

            if (mapData == null)
            {
                response.Success = false;
                response.Message = "Failed to parse map JSON file";
                response.Errors.Add($"File not found or invalid JSON format: {filePath}");
                return response;
            }

            response.Stats.MapCode = mapData.MapCode;

            // Process each floor
            foreach (var floor in mapData.FloorList)
            {
                var floorResult = await ImportFloorNodesAsync(
                    mapData.MapCode,
                    floor,
                    overwriteExisting,
                    cancellationToken);

                response.Stats.TotalNodesInFile += floorResult.TotalNodes;
                response.Stats.NodesImported += floorResult.NodesImported;
                response.Stats.NodesUpdated += floorResult.NodesUpdated;
                response.Stats.NodesSkipped += floorResult.NodesSkipped;
                response.Stats.NodesFailed += floorResult.NodesFailed;
                response.Errors.AddRange(floorResult.Errors);
                response.Warnings.AddRange(floorResult.Warnings);
            }

            response.Success = response.Stats.NodesFailed == 0;
            response.Message = response.Success
                ? $"Successfully imported {response.Stats.NodesImported} nodes, updated {response.Stats.NodesUpdated} nodes from map '{mapData.MapCode}'"
                : $"Completed with errors: {response.Stats.NodesFailed} nodes failed";

            _logger.LogInformation(
                "Map import completed: {MapCode}, Imported={Imported}, Updated={Updated}, Skipped={Skipped}, Failed={Failed}",
                mapData.MapCode, response.Stats.NodesImported, response.Stats.NodesUpdated,
                response.Stats.NodesSkipped, response.Stats.NodesFailed);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing map from file: {FilePath}", filePath);
            response.Success = false;
            response.Message = "Import failed due to unexpected error";
            response.Errors.Add($"Exception: {ex.Message}");
            return response;
        }
    }

    private async Task<FloorImportResult> ImportFloorNodesAsync(
        string mapCode,
        FloorDto floor,
        bool overwriteExisting,
        CancellationToken cancellationToken)
    {
        var result = new FloorImportResult
        {
            TotalNodes = floor.NodeList.Count
        };

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        const string importSource = "MapImportService";
        const string importApp = "QES-KUKA-AMR-API";

        foreach (var node in floor.NodeList)
        {
            try
            {
                // Check if QR code already exists
                var existing = await _context.QrCodes
                    .FirstOrDefaultAsync(q =>
                        q.NodeLabel == node.NodeLabel &&
                        q.MapCode == mapCode,
                        cancellationToken);

                if (existing != null)
                {
                    if (overwriteExisting)
                    {
                        // Update existing
                        UpdateQrCodeFromNode(existing, node, mapCode, floor, now, importSource, importApp);
                        result.NodesUpdated++;
                    }
                    else
                    {
                        result.NodesSkipped++;
                        result.Warnings.Add($"Node '{node.NodeLabel}' already exists, skipped (use overwrite to update)");
                    }
                }
                else
                {
                    // Create new
                    var qrCode = CreateQrCodeFromNode(node, mapCode, floor, now, importSource, importApp);
                    _context.QrCodes.Add(qrCode);
                    result.NodesImported++;
                }
            }
            catch (Exception ex)
            {
                result.NodesFailed++;
                result.Errors.Add($"Failed to import node '{node.NodeLabel}': {ex.Message}");
                _logger.LogError(ex, "Error importing node: {NodeLabel}", node.NodeLabel);
            }
        }

        // Save changes
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving changes to database");
            result.Errors.Add($"Database save failed: {ex.Message}");
            result.NodesFailed = result.TotalNodes;
            result.NodesImported = 0;
            result.NodesUpdated = 0;
        }

        return result;
    }

    private QrCode CreateQrCodeFromNode(
        NodeDto node,
        string mapCode,
        FloorDto floor,
        DateTime now,
        string source,
        string app)
    {
        return new QrCode
        {
            NodeLabel = node.NodeLabel,
            MapCode = mapCode,
            FloorNumber = floor.FloorNumber,
            NodeNumber = node.NodeNumber,
            NodeUuid = node.NodeUuid,
            NodeType = node.NodeType,
            XCoordinate = node.XCoordinate,
            YCoordinate = node.YCoordinate,
            TransitOrientations = node.TransitOrientations,
            DistanceAccuracy = node.DistanceAccuracy,
            GoalDistanceAccuracy = node.GoalDistanceAccuracy,
            AngularAccuracy = node.AngularAccuracy,
            GoalAngularAccuracy = node.GoalAngularAccuracy,
            SpecialConfig = node.SpecialConfig,
            FunctionListJson = node.FunctionList != null && node.FunctionList.Any()
                ? JsonSerializer.Serialize(node.FunctionList)
                : null,
            Reliability = 100, // Default reliability
            ReportTimes = 0,
            CreateTime = now,
            CreateBy = source,
            CreateApp = app,
            LastUpdateTime = now,
            LastUpdateBy = source,
            LastUpdateApp = app
        };
    }

    private void UpdateQrCodeFromNode(
        QrCode existing,
        NodeDto node,
        string mapCode,
        FloorDto floor,
        DateTime now,
        string source,
        string app)
    {
        existing.FloorNumber = floor.FloorNumber;
        existing.NodeNumber = node.NodeNumber;
        existing.NodeUuid = node.NodeUuid;
        existing.NodeType = node.NodeType;
        existing.XCoordinate = node.XCoordinate;
        existing.YCoordinate = node.YCoordinate;
        existing.TransitOrientations = node.TransitOrientations;
        existing.DistanceAccuracy = node.DistanceAccuracy;
        existing.GoalDistanceAccuracy = node.GoalDistanceAccuracy;
        existing.AngularAccuracy = node.AngularAccuracy;
        existing.GoalAngularAccuracy = node.GoalAngularAccuracy;
        existing.SpecialConfig = node.SpecialConfig;
        existing.FunctionListJson = node.FunctionList != null && node.FunctionList.Any()
            ? JsonSerializer.Serialize(node.FunctionList)
            : null;
        existing.LastUpdateTime = now;
        existing.LastUpdateBy = source;
        existing.LastUpdateApp = app;
    }

    private class FloorImportResult
    {
        public int TotalNodes { get; set; }
        public int NodesImported { get; set; }
        public int NodesUpdated { get; set; }
        public int NodesSkipped { get; set; }
        public int NodesFailed { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
    }
}
