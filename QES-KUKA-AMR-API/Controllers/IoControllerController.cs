using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QES_KUKA_AMR_API.Data.Entities;
using QES_KUKA_AMR_API.Models.IoController;
using QES_KUKA_AMR_API.Services.IoController;
using System.Security.Claims;

namespace QES_KUKA_AMR_API.Controllers;

/// <summary>
/// Controller for managing IO controller devices and channels.
/// Provides CRUD operations for devices, channel control, and audit logging.
/// </summary>
[ApiController]
[Authorize]
[Route("api/[controller]")]
public class IoControllerController : ControllerBase
{
    private readonly IIoControllerDeviceService _deviceService;
    private readonly IIoChannelService _channelService;
    private readonly IIoStateLogService _logService;
    private readonly ILogger<IoControllerController> _logger;

    public IoControllerController(
        IIoControllerDeviceService deviceService,
        IIoChannelService channelService,
        IIoStateLogService logService,
        ILogger<IoControllerController> logger)
    {
        _deviceService = deviceService;
        _channelService = channelService;
        _logService = logService;
        _logger = logger;
    }

    private string CurrentUsername => User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";

    #region Device CRUD

    /// <summary>
    /// Get all IO controller devices.
    /// </summary>
    [HttpGet("devices")]
    public async Task<ActionResult<List<IoControllerDeviceDto>>> GetAllDevices(CancellationToken ct)
    {
        var devices = await _deviceService.GetAllAsync(ct);
        return Ok(devices.Select(IoControllerDeviceDto.FromEntity).ToList());
    }

    /// <summary>
    /// Get a specific device by ID.
    /// </summary>
    [HttpGet("devices/{id}")]
    public async Task<ActionResult<IoControllerDeviceDto>> GetDevice(int id, CancellationToken ct)
    {
        var device = await _deviceService.GetByIdAsync(id, ct);
        if (device == null)
        {
            return NotFound($"Device with ID {id} not found.");
        }
        return Ok(IoControllerDeviceDto.FromEntity(device));
    }

    /// <summary>
    /// Create a new IO controller device.
    /// </summary>
    [HttpPost("devices")]
    public async Task<ActionResult<IoControllerDeviceDto>> CreateDevice(
        [FromBody] CreateIoDeviceRequest request,
        CancellationToken ct)
    {
        try
        {
            var device = new IoControllerDevice
            {
                DeviceName = request.DeviceName,
                IpAddress = request.IpAddress,
                Port = request.Port,
                UnitId = request.UnitId,
                Description = request.Description,
                IsActive = request.IsActive,
                PollingIntervalMs = request.PollingIntervalMs,
                ConnectionTimeoutMs = request.ConnectionTimeoutMs
            };

            var created = await _deviceService.CreateAsync(device, CurrentUsername, ct);
            return CreatedAtAction(nameof(GetDevice), new { id = created.Id }, IoControllerDeviceDto.FromEntity(created));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }

    /// <summary>
    /// Update an existing device.
    /// </summary>
    [HttpPut("devices/{id}")]
    public async Task<ActionResult<IoControllerDeviceDto>> UpdateDevice(
        int id,
        [FromBody] UpdateIoDeviceRequest request,
        CancellationToken ct)
    {
        var device = new IoControllerDevice
        {
            DeviceName = request.DeviceName,
            Description = request.Description,
            IsActive = request.IsActive,
            PollingIntervalMs = request.PollingIntervalMs,
            ConnectionTimeoutMs = request.ConnectionTimeoutMs
        };

        var updated = await _deviceService.UpdateAsync(id, device, CurrentUsername, ct);
        if (updated == null)
        {
            return NotFound($"Device with ID {id} not found.");
        }
        return Ok(IoControllerDeviceDto.FromEntity(updated));
    }

    /// <summary>
    /// Delete a device.
    /// </summary>
    [HttpDelete("devices/{id}")]
    public async Task<ActionResult> DeleteDevice(int id, CancellationToken ct)
    {
        var deleted = await _deviceService.DeleteAsync(id, ct);
        if (!deleted)
        {
            return NotFound($"Device with ID {id} not found.");
        }
        return NoContent();
    }

    /// <summary>
    /// Test connection to a device.
    /// </summary>
    [HttpPost("devices/{id}/test")]
    public async Task<ActionResult<IoConnectionResult>> TestConnection(int id, CancellationToken ct)
    {
        var result = await _deviceService.TestConnectionAsync(id, ct);
        return Ok(result);
    }

    /// <summary>
    /// Get full device status including all channels.
    /// </summary>
    [HttpGet("devices/{id}/status")]
    public async Task<ActionResult<IoDeviceFullStatusDto>> GetDeviceStatus(int id, CancellationToken ct)
    {
        var status = await _deviceService.GetDeviceStatusAsync(id, ct);
        if (status == null)
        {
            return NotFound($"Device with ID {id} not found.");
        }

        return Ok(new IoDeviceFullStatusDto
        {
            Device = IoControllerDeviceDto.FromEntity(status.Device),
            Channels = status.Channels.Select(IoChannelResponseDto.FromEntity).ToList(),
            IsConnected = status.IsConnected,
            LastPollUtc = status.LastPollUtc
        });
    }

    #endregion

    #region Channel Operations

    /// <summary>
    /// Get all channels for a device.
    /// </summary>
    [HttpGet("devices/{deviceId}/channels")]
    public async Task<ActionResult<List<IoChannelResponseDto>>> GetChannels(int deviceId, CancellationToken ct)
    {
        var channels = await _channelService.GetChannelsByDeviceAsync(deviceId, ct);
        return Ok(channels.Select(IoChannelResponseDto.FromEntity).ToList());
    }

    /// <summary>
    /// Update a channel's label.
    /// </summary>
    [HttpPut("devices/{deviceId}/channels/{channelType}/{channelNumber}/label")]
    public async Task<ActionResult<IoChannelResponseDto>> UpdateChannelLabel(
        int deviceId,
        string channelType,
        int channelNumber,
        [FromBody] UpdateChannelLabelRequest request,
        CancellationToken ct)
    {
        if (!Enum.TryParse<IoChannelType>(channelType, true, out var type))
        {
            return BadRequest($"Invalid channel type: {channelType}. Use 'DigitalInput' or 'DigitalOutput'.");
        }

        var channel = await _channelService.UpdateChannelLabelAsync(deviceId, channelNumber, type, request.Label, ct);
        if (channel == null)
        {
            return NotFound($"Channel {channelType} {channelNumber} not found for device {deviceId}.");
        }
        return Ok(IoChannelResponseDto.FromEntity(channel));
    }

    /// <summary>
    /// Set a Digital Output channel value.
    /// </summary>
    [HttpPost("devices/{deviceId}/do/{channelNumber}")]
    public async Task<ActionResult<IoWriteResult>> SetDigitalOutput(
        int deviceId,
        int channelNumber,
        [FromBody] SetDigitalOutputRequest request,
        CancellationToken ct)
    {
        var result = await _channelService.SetDigitalOutputAsync(
            deviceId, channelNumber, request.Value, CurrentUsername, request.Reason, ct);

        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    /// <summary>
    /// Set FSV (Fail-Safe Value) for a DO channel.
    /// </summary>
    [HttpPost("devices/{deviceId}/do/{channelNumber}/fsv")]
    public async Task<ActionResult<IoWriteResult>> SetFsv(
        int deviceId,
        int channelNumber,
        [FromBody] SetFsvRequest request,
        CancellationToken ct)
    {
        var result = await _channelService.SetFsvAsync(
            deviceId, channelNumber, request.Enabled, request.Value, CurrentUsername, ct);

        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    #endregion

    #region Logs

    /// <summary>
    /// Get recent logs for a specific device.
    /// </summary>
    [HttpGet("devices/{deviceId}/logs")]
    public async Task<ActionResult<List<IoStateLogDto>>> GetDeviceLogs(
        int deviceId,
        [FromQuery] int count = 50,
        CancellationToken ct = default)
    {
        var logs = await _logService.GetRecentLogsForDeviceAsync(deviceId, count, ct);
        return Ok(logs.Select(IoStateLogDto.FromEntity).ToList());
    }

    /// <summary>
    /// Get all logs with filtering and pagination.
    /// </summary>
    [HttpGet("logs")]
    public async Task<ActionResult<PagedResponse<IoStateLogDto>>> GetLogs(
        [FromQuery] int? deviceId,
        [FromQuery] int? channelNumber,
        [FromQuery] string? channelType,
        [FromQuery] string? changeSource,
        [FromQuery] string? changedBy,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var query = new IoStateLogQuery
        {
            DeviceId = deviceId,
            ChannelNumber = channelNumber,
            ChangedBy = changedBy,
            FromUtc = fromUtc,
            ToUtc = toUtc,
            Page = page,
            PageSize = Math.Min(pageSize, 200) // Cap at 200
        };

        if (!string.IsNullOrEmpty(channelType) && Enum.TryParse<IoChannelType>(channelType, true, out var ct2))
        {
            query.ChannelType = ct2;
        }

        if (!string.IsNullOrEmpty(changeSource) && Enum.TryParse<IoStateChangeSource>(changeSource, true, out var source))
        {
            query.ChangeSource = source;
        }

        var result = await _logService.GetLogsAsync(query, ct);

        return Ok(new PagedResponse<IoStateLogDto>
        {
            Items = result.Items.Select(IoStateLogDto.FromEntity).ToList(),
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize,
            TotalPages = result.TotalPages,
            HasNextPage = result.HasNextPage,
            HasPreviousPage = result.HasPreviousPage
        });
    }

    #endregion
}
