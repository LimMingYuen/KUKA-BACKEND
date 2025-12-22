using System.ComponentModel.DataAnnotations;
using QES_KUKA_AMR_API.Data.Entities;

namespace QES_KUKA_AMR_API.Models.IoController;

/// <summary>
/// DTO for creating a new IO controller device.
/// </summary>
public class CreateIoDeviceRequest
{
    [Required]
    [MaxLength(100)]
    public string DeviceName { get; set; } = string.Empty;

    [Required]
    [MaxLength(45)]
    public string IpAddress { get; set; } = string.Empty;

    public int Port { get; set; } = 502;

    public byte UnitId { get; set; } = 1;

    [MaxLength(500)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public int PollingIntervalMs { get; set; } = 1000;

    public int ConnectionTimeoutMs { get; set; } = 3000;
}

/// <summary>
/// DTO for updating an existing IO controller device.
/// </summary>
public class UpdateIoDeviceRequest
{
    [Required]
    [MaxLength(100)]
    public string DeviceName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public int PollingIntervalMs { get; set; } = 1000;

    public int ConnectionTimeoutMs { get; set; } = 3000;
}

/// <summary>
/// DTO for IO controller device response.
/// </summary>
public class IoControllerDeviceDto
{
    public int Id { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public int Port { get; set; }
    public byte UnitId { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public int PollingIntervalMs { get; set; }
    public int ConnectionTimeoutMs { get; set; }
    public DateTime? LastPollUtc { get; set; }
    public bool? LastConnectionSuccess { get; set; }
    public string? LastErrorMessage { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }

    public static IoControllerDeviceDto FromEntity(IoControllerDevice entity)
    {
        return new IoControllerDeviceDto
        {
            Id = entity.Id,
            DeviceName = entity.DeviceName,
            IpAddress = entity.IpAddress,
            Port = entity.Port,
            UnitId = entity.UnitId,
            Description = entity.Description,
            IsActive = entity.IsActive,
            PollingIntervalMs = entity.PollingIntervalMs,
            ConnectionTimeoutMs = entity.ConnectionTimeoutMs,
            LastPollUtc = entity.LastPollUtc,
            LastConnectionSuccess = entity.LastConnectionSuccess,
            LastErrorMessage = entity.LastErrorMessage,
            CreatedUtc = entity.CreatedUtc,
            UpdatedUtc = entity.UpdatedUtc,
            CreatedBy = entity.CreatedBy,
            UpdatedBy = entity.UpdatedBy
        };
    }
}

/// <summary>
/// DTO for IO channel response.
/// </summary>
public class IoChannelResponseDto
{
    public int Id { get; set; }
    public int DeviceId { get; set; }
    public int ChannelNumber { get; set; }
    public string ChannelType { get; set; } = string.Empty;
    public string? Label { get; set; }
    public bool CurrentState { get; set; }
    public bool? FailSafeValue { get; set; }
    public bool FsvEnabled { get; set; }
    public DateTime? LastStateChangeUtc { get; set; }

    public static IoChannelResponseDto FromEntity(IoChannel entity)
    {
        return new IoChannelResponseDto
        {
            Id = entity.Id,
            DeviceId = entity.DeviceId,
            ChannelNumber = entity.ChannelNumber,
            ChannelType = entity.ChannelType.ToString(),
            Label = entity.Label,
            CurrentState = entity.CurrentState,
            FailSafeValue = entity.FailSafeValue,
            FsvEnabled = entity.FsvEnabled,
            LastStateChangeUtc = entity.LastStateChangeUtc
        };
    }
}

/// <summary>
/// DTO for full device status including all channels.
/// </summary>
public class IoDeviceFullStatusDto
{
    public IoControllerDeviceDto Device { get; set; } = null!;
    public List<IoChannelResponseDto> Channels { get; set; } = new();
    public bool IsConnected { get; set; }
    public DateTime? LastPollUtc { get; set; }
}

/// <summary>
/// Request to set a Digital Output value.
/// </summary>
public class SetDigitalOutputRequest
{
    [Required]
    public bool Value { get; set; }

    [MaxLength(500)]
    public string? Reason { get; set; }
}

/// <summary>
/// Request to set FSV (Fail-Safe Value) for a DO channel.
/// </summary>
public class SetFsvRequest
{
    public bool Enabled { get; set; }
    public bool Value { get; set; }
}

/// <summary>
/// Request to update channel label.
/// </summary>
public class UpdateChannelLabelRequest
{
    [MaxLength(100)]
    public string? Label { get; set; }
}

/// <summary>
/// DTO for state log entry.
/// </summary>
public class IoStateLogDto
{
    public int Id { get; set; }
    public int DeviceId { get; set; }
    public int ChannelNumber { get; set; }
    public string ChannelType { get; set; } = string.Empty;
    public bool PreviousState { get; set; }
    public bool NewState { get; set; }
    public string ChangeSource { get; set; } = string.Empty;
    public string? ChangedBy { get; set; }
    public DateTime ChangedUtc { get; set; }
    public string? Reason { get; set; }

    public static IoStateLogDto FromEntity(IoStateLog entity)
    {
        return new IoStateLogDto
        {
            Id = entity.Id,
            DeviceId = entity.DeviceId,
            ChannelNumber = entity.ChannelNumber,
            ChannelType = entity.ChannelType.ToString(),
            PreviousState = entity.PreviousState,
            NewState = entity.NewState,
            ChangeSource = entity.ChangeSource.ToString(),
            ChangedBy = entity.ChangedBy,
            ChangedUtc = entity.ChangedUtc,
            Reason = entity.Reason
        };
    }
}

/// <summary>
/// Paged response wrapper.
/// </summary>
public class PagedResponse<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
}
