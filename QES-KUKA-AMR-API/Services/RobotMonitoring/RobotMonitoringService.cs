using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QES_KUKA_AMR_API.Data;
using QES_KUKA_AMR_API.Data.Entities;
using QES_KUKA_AMR_API.Models.RobotMonitoring;
using QES_KUKA_AMR_API.Options;

namespace QES_KUKA_AMR_API.Services.RobotMonitoring;

public class RobotMonitoringService : IRobotMonitoringService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<RobotMonitoringService> _logger;
    private readonly FileStorageOptions _fileStorageOptions;
    private readonly TimeProvider _timeProvider;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public RobotMonitoringService(
        ApplicationDbContext dbContext,
        ILogger<RobotMonitoringService> logger,
        IOptions<FileStorageOptions> fileStorageOptions,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _logger = logger;
        _fileStorageOptions = fileStorageOptions.Value;
        _timeProvider = timeProvider;
    }

    public async Task<IEnumerable<RobotMonitoringMapSummaryDto>> GetAllMapsAsync(CancellationToken cancellationToken = default)
    {
        var maps = await _dbContext.RobotMonitoringMaps
            .AsNoTracking()
            .OrderByDescending(m => m.IsDefault)
            .ThenBy(m => m.Name)
            .ToListAsync(cancellationToken);

        return maps.Select(m => new RobotMonitoringMapSummaryDto
        {
            Id = m.Id,
            Name = m.Name,
            Description = m.Description,
            MapCode = m.MapCode,
            FloorNumber = m.FloorNumber,
            HasBackgroundImage = !string.IsNullOrEmpty(m.BackgroundImagePath),
            IsDefault = m.IsDefault,
            CreatedBy = m.CreatedBy,
            CreatedUtc = m.CreatedUtc,
            LastUpdatedUtc = m.LastUpdatedUtc
        });
    }

    public async Task<RobotMonitoringMapDto?> GetMapByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.RobotMonitoringMaps
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

        return entity == null ? null : MapToDto(entity);
    }

    public async Task<RobotMonitoringMapDto?> GetDefaultMapAsync(CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.RobotMonitoringMaps
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.IsDefault, cancellationToken);

        return entity == null ? null : MapToDto(entity);
    }

    public async Task<RobotMonitoringMapDto> CreateMapAsync(CreateRobotMonitoringMapRequest request, string username, CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        // If this is set as default, unset any existing default
        if (request.IsDefault)
        {
            await UnsetDefaultMapsAsync(cancellationToken);
        }

        var entity = new RobotMonitoringMap
        {
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            MapCode = request.MapCode?.Trim(),
            FloorNumber = request.FloorNumber?.Trim(),
            DisplaySettingsJson = request.DisplaySettings != null
                ? JsonSerializer.Serialize(request.DisplaySettings, JsonOptions)
                : null,
            CustomNodesJson = request.CustomNodes != null && request.CustomNodes.Count > 0
                ? JsonSerializer.Serialize(request.CustomNodes, JsonOptions)
                : null,
            CustomZonesJson = request.CustomZones != null && request.CustomZones.Count > 0
                ? JsonSerializer.Serialize(request.CustomZones, JsonOptions)
                : null,
            CustomLinesJson = request.CustomLines != null && request.CustomLines.Count > 0
                ? JsonSerializer.Serialize(request.CustomLines, JsonOptions)
                : null,
            IsDefault = request.IsDefault,
            CreatedBy = username,
            CreatedUtc = now
        };

        _dbContext.RobotMonitoringMaps.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created robot monitoring map {Id} '{Name}' by {Username}", entity.Id, entity.Name, username);

        return MapToDto(entity);
    }

    public async Task<RobotMonitoringMapDto?> UpdateMapAsync(int id, UpdateRobotMonitoringMapRequest request, string username, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.RobotMonitoringMaps
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

        if (entity == null)
        {
            return null;
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        // If this is set as default, unset any existing default
        if (request.IsDefault == true && !entity.IsDefault)
        {
            await UnsetDefaultMapsAsync(cancellationToken);
        }

        if (request.Name != null)
            entity.Name = request.Name.Trim();

        if (request.Description != null)
            entity.Description = request.Description.Trim();

        if (request.MapCode != null)
            entity.MapCode = request.MapCode.Trim();

        if (request.FloorNumber != null)
            entity.FloorNumber = request.FloorNumber.Trim();

        if (request.DisplaySettings != null)
            entity.DisplaySettingsJson = JsonSerializer.Serialize(request.DisplaySettings, JsonOptions);

        if (request.CustomNodes != null)
            entity.CustomNodesJson = request.CustomNodes.Count > 0
                ? JsonSerializer.Serialize(request.CustomNodes, JsonOptions)
                : null;

        if (request.CustomZones != null)
            entity.CustomZonesJson = request.CustomZones.Count > 0
                ? JsonSerializer.Serialize(request.CustomZones, JsonOptions)
                : null;

        if (request.CustomLines != null)
            entity.CustomLinesJson = request.CustomLines.Count > 0
                ? JsonSerializer.Serialize(request.CustomLines, JsonOptions)
                : null;

        if (request.IsDefault.HasValue)
            entity.IsDefault = request.IsDefault.Value;

        entity.LastUpdatedBy = username;
        entity.LastUpdatedUtc = now;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated robot monitoring map {Id} '{Name}' by {Username}", entity.Id, entity.Name, username);

        return MapToDto(entity);
    }

    public async Task<bool> DeleteMapAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.RobotMonitoringMaps
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

        if (entity == null)
        {
            return false;
        }

        // Delete background image file if exists
        if (!string.IsNullOrEmpty(entity.BackgroundImagePath))
        {
            DeleteImageFile(entity.BackgroundImagePath);
        }

        _dbContext.RobotMonitoringMaps.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted robot monitoring map {Id} '{Name}'", entity.Id, entity.Name);

        return true;
    }

    public async Task<ImageUploadResponse> UploadBackgroundImageAsync(int mapId, IFormFile file, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.RobotMonitoringMaps
            .FirstOrDefaultAsync(m => m.Id == mapId, cancellationToken);

        if (entity == null)
        {
            return new ImageUploadResponse { Success = false, Message = "Map configuration not found" };
        }

        // Validate file
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
        {
            return new ImageUploadResponse { Success = false, Message = "Invalid file type. Allowed: jpg, jpeg, png, gif, webp" };
        }

        const long maxFileSize = 10 * 1024 * 1024; // 10MB
        if (file.Length > maxFileSize)
        {
            return new ImageUploadResponse { Success = false, Message = "File size exceeds 10MB limit" };
        }

        try
        {
            // Delete existing image if any
            if (!string.IsNullOrEmpty(entity.BackgroundImagePath))
            {
                DeleteImageFile(entity.BackgroundImagePath);
            }

            // Create uploads directory using configured path
            var uploadsPath = Path.Combine(_fileStorageOptions.UploadsPath, _fileStorageOptions.MapsFolder);
            Directory.CreateDirectory(uploadsPath);

            // Generate unique filename
            var uniqueFileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsPath, uniqueFileName);

            // Save file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream, cancellationToken);
            }

            // Image dimensions - set to null for now (can be calculated on frontend)
            // Note: System.Drawing.Image is Windows-only
            int? width = null;
            int? height = null;

            // Update entity - relative path for serving via /uploads route
            var relativePath = $"/uploads/{_fileStorageOptions.MapsFolder}/{uniqueFileName}";
            entity.BackgroundImagePath = relativePath;
            entity.BackgroundImageOriginalName = file.FileName;
            entity.ImageWidth = width;
            entity.ImageHeight = height;
            entity.LastUpdatedUtc = _timeProvider.GetUtcNow().UtcDateTime;

            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Uploaded background image for map {Id}: {FileName} to {Path}", mapId, file.FileName, filePath);

            return new ImageUploadResponse
            {
                Success = true,
                ImagePath = relativePath,
                OriginalName = file.FileName,
                Width = width,
                Height = height,
                Message = "Image uploaded successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload background image for map {Id}", mapId);
            return new ImageUploadResponse { Success = false, Message = "Failed to upload image" };
        }
    }

    public async Task<bool> DeleteBackgroundImageAsync(int mapId, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.RobotMonitoringMaps
            .FirstOrDefaultAsync(m => m.Id == mapId, cancellationToken);

        if (entity == null || string.IsNullOrEmpty(entity.BackgroundImagePath))
        {
            return false;
        }

        DeleteImageFile(entity.BackgroundImagePath);

        entity.BackgroundImagePath = null;
        entity.BackgroundImageOriginalName = null;
        entity.ImageWidth = null;
        entity.ImageHeight = null;
        entity.LastUpdatedUtc = _timeProvider.GetUtcNow().UtcDateTime;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted background image for map {Id}", mapId);

        return true;
    }

    public async Task<MapDataDto> GetMapDataAsync(string? mapCode, string? floorNumber, CancellationToken cancellationToken = default)
    {
        // Query QR codes (nodes)
        var nodesQuery = _dbContext.QrCodes.AsNoTracking();

        if (!string.IsNullOrEmpty(mapCode))
            nodesQuery = nodesQuery.Where(q => q.MapCode == mapCode);

        if (!string.IsNullOrEmpty(floorNumber))
            nodesQuery = nodesQuery.Where(q => q.FloorNumber == floorNumber);

        var nodes = await nodesQuery
            .Where(q => q.XCoordinate.HasValue && q.YCoordinate.HasValue)
            .Select(q => new MapNodeDto
            {
                Id = q.Id,
                NodeLabel = q.NodeLabel,
                NodeUuid = q.NodeUuid,
                NodeNumber = q.NodeNumber,
                X = q.XCoordinate!.Value,
                Y = q.YCoordinate!.Value,
                NodeType = q.NodeType,
                MapCode = q.MapCode,
                FloorNumber = q.FloorNumber
            })
            .ToListAsync(cancellationToken);

        // Query map zones
        var zonesQuery = _dbContext.MapZones.AsNoTracking();

        if (!string.IsNullOrEmpty(mapCode))
            zonesQuery = zonesQuery.Where(z => z.MapCode == mapCode);

        if (!string.IsNullOrEmpty(floorNumber))
            zonesQuery = zonesQuery.Where(z => z.FloorNumber == floorNumber);

        var zoneEntities = await zonesQuery
            .Where(z => z.Status == 1) // Only active zones
            .ToListAsync(cancellationToken);

        var zones = zoneEntities.Select(z => new MapZoneDto
        {
            Id = z.Id,
            ZoneName = z.ZoneName,
            ZoneCode = z.ZoneCode,
            ZoneColor = z.ZoneColor,
            ZoneType = z.ZoneType,
            Points = ParsePoints(z.Points),
            NodeLabels = ParseNodeLabels(z.Nodes),
            MapCode = z.MapCode,
            FloorNumber = z.FloorNumber
        }).ToList();

        return new MapDataDto
        {
            Nodes = nodes,
            Zones = zones
        };
    }

    public async Task<IEnumerable<string>> GetAvailableMapCodesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.QrCodes
            .AsNoTracking()
            .Select(q => q.MapCode)
            .Distinct()
            .OrderBy(m => m)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<string>> GetAvailableFloorNumbersAsync(string? mapCode, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.QrCodes.AsNoTracking();

        if (!string.IsNullOrEmpty(mapCode))
            query = query.Where(q => q.MapCode == mapCode);

        return await query
            .Select(q => q.FloorNumber)
            .Distinct()
            .OrderBy(f => f)
            .ToListAsync(cancellationToken);
    }

    private async Task UnsetDefaultMapsAsync(CancellationToken cancellationToken)
    {
        var defaultMaps = await _dbContext.RobotMonitoringMaps
            .Where(m => m.IsDefault)
            .ToListAsync(cancellationToken);

        foreach (var map in defaultMaps)
        {
            map.IsDefault = false;
        }
    }

    private void DeleteImageFile(string relativePath)
    {
        try
        {
            // relativePath is like "/uploads/maps/filename.jpg"
            // We need to map it to the actual file path
            var pathWithoutUploads = relativePath.Replace("/uploads/", "").TrimStart('/');
            var fullPath = Path.Combine(_fileStorageOptions.UploadsPath, pathWithoutUploads);

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                _logger.LogInformation("Deleted image file: {Path}", fullPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete image file: {Path}", relativePath);
        }
    }

    private static RobotMonitoringMapDto MapToDto(RobotMonitoringMap entity)
    {
        return new RobotMonitoringMapDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            MapCode = entity.MapCode,
            FloorNumber = entity.FloorNumber,
            BackgroundImagePath = entity.BackgroundImagePath,
            BackgroundImageOriginalName = entity.BackgroundImageOriginalName,
            ImageWidth = entity.ImageWidth,
            ImageHeight = entity.ImageHeight,
            DisplaySettings = string.IsNullOrEmpty(entity.DisplaySettingsJson)
                ? null
                : JsonSerializer.Deserialize<DisplaySettings>(entity.DisplaySettingsJson, JsonOptions),
            CustomNodes = string.IsNullOrEmpty(entity.CustomNodesJson)
                ? null
                : JsonSerializer.Deserialize<List<CustomNodeDto>>(entity.CustomNodesJson, JsonOptions),
            CustomZones = string.IsNullOrEmpty(entity.CustomZonesJson)
                ? null
                : JsonSerializer.Deserialize<List<CustomZoneDto>>(entity.CustomZonesJson, JsonOptions),
            CustomLines = string.IsNullOrEmpty(entity.CustomLinesJson)
                ? null
                : JsonSerializer.Deserialize<List<CustomLineDto>>(entity.CustomLinesJson, JsonOptions),
            IsDefault = entity.IsDefault,
            CreatedBy = entity.CreatedBy,
            CreatedUtc = entity.CreatedUtc,
            LastUpdatedBy = entity.LastUpdatedBy,
            LastUpdatedUtc = entity.LastUpdatedUtc
        };
    }

    private static List<PointDto> ParsePoints(string? pointsJson)
    {
        if (string.IsNullOrEmpty(pointsJson))
            return new List<PointDto>();

        try
        {
            // Points are stored as JSON array of arrays: [[x1,y1],[x2,y2],...]
            var points = JsonSerializer.Deserialize<List<List<double>>>(pointsJson);
            if (points == null)
                return new List<PointDto>();

            return points.Select(p => new PointDto
            {
                X = p.Count > 0 ? p[0] : 0,
                Y = p.Count > 1 ? p[1] : 0
            }).ToList();
        }
        catch
        {
            return new List<PointDto>();
        }
    }

    private static List<string> ParseNodeLabels(string? nodesJson)
    {
        if (string.IsNullOrEmpty(nodesJson))
            return new List<string>();

        try
        {
            var nodes = JsonSerializer.Deserialize<List<string>>(nodesJson);
            return nodes ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }
}
