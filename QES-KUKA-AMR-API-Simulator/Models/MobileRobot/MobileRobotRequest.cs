using System.Text.Json.Serialization;

namespace QES_KUKA_AMR_API_Simulator.Models.MobileRobot;

public class MobileRobotRequest
{
    [JsonPropertyName("query")]
    public MobileRobotQueryFilter? Query { get; set; }

    [JsonPropertyName("pageNum")]
    public int PageNum { get; set; }

    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; }

    [JsonPropertyName("orderBy")]
    public string? OrderBy { get; set; }

    [JsonPropertyName("asc")]
    public bool Asc { get; set; }
}

/// <summary>
/// Query filter object for mobile robot list API.
/// All properties are optional - set only those you want to filter by.
/// </summary>
public class MobileRobotQueryFilter
{
    [JsonPropertyName("robotId")]
    public string? RobotId { get; set; }

    [JsonPropertyName("robotTypeCode")]
    public string? RobotTypeCode { get; set; }

    [JsonPropertyName("mapCode")]
    public string? MapCode { get; set; }

    [JsonPropertyName("floorNumber")]
    public string? FloorNumber { get; set; }

    [JsonPropertyName("buildingCode")]
    public string? BuildingCode { get; set; }

    [JsonPropertyName("status")]
    public int? Status { get; set; }
}
