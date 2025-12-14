using System.Text.Json.Serialization;

namespace QES_KUKA_AMR_API.Models.MobileRobot;

public class QueryMobileRobotRequest
{
    [JsonPropertyName("query")]
    public MobileRobotQuery? Query { get; set; } = new MobileRobotQuery();

    [JsonPropertyName("pageNum")]
    public int PageNum { get; set; } = 1;

    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; } = 100000;

    [JsonPropertyName("orderBy")]
    public string OrderBy { get; set; } = "lastUpdateTime";

    [JsonPropertyName("asc")]
    public bool Asc { get; set; } = false;
}

/// <summary>
/// Query filter object for mobile robot list API.
/// All properties are optional - set only those you want to filter by.
/// </summary>
public class MobileRobotQuery
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
