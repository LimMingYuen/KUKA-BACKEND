using System.ComponentModel.DataAnnotations;

namespace QES_KUKA_AMR_API.Models.QrCode;

/// <summary>
/// Detailed QR code response for CRUD operations
/// </summary>
public class QrCodeDetailDto
{
    public int Id { get; set; }
    public string NodeLabel { get; set; } = string.Empty;
    public string MapCode { get; set; } = string.Empty;
    public string FloorNumber { get; set; } = string.Empty;
    public int NodeNumber { get; set; }
    public double? XCoordinate { get; set; }
    public double? YCoordinate { get; set; }
    public string? NodeUuid { get; set; }
    public int? NodeType { get; set; }
    public string? TransitOrientations { get; set; }
    public int Reliability { get; set; }
    public int ReportTimes { get; set; }
    public DateTime CreateTime { get; set; }
    public DateTime LastUpdateTime { get; set; }
}

/// <summary>
/// Request to create a new QR code node
/// </summary>
public class CreateQrCodeRequest
{
    [Required]
    [MaxLength(64)]
    public string NodeLabel { get; set; } = string.Empty;

    [Required]
    [MaxLength(128)]
    public string MapCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(16)]
    public string FloorNumber { get; set; } = string.Empty;

    [Required]
    public int NodeNumber { get; set; }

    [Required]
    public double XCoordinate { get; set; }

    [Required]
    public double YCoordinate { get; set; }

    [MaxLength(128)]
    public string? NodeUuid { get; set; }

    public int? NodeType { get; set; }

    [MaxLength(64)]
    public string? TransitOrientations { get; set; }
}

/// <summary>
/// Request to update an existing QR code node
/// </summary>
public class UpdateQrCodeRequest
{
    [MaxLength(64)]
    public string? NodeLabel { get; set; }

    public int? NodeNumber { get; set; }

    public double? XCoordinate { get; set; }

    public double? YCoordinate { get; set; }

    [MaxLength(128)]
    public string? NodeUuid { get; set; }

    public int? NodeType { get; set; }

    [MaxLength(64)]
    public string? TransitOrientations { get; set; }
}
