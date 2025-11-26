using System.Text.Json.Serialization;

namespace QES_KUKA_AMR_API.Models.MobileRobot;

public class ContainerRealtimeDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("containerType")]
    public int ContainerType { get; set; }

    [JsonPropertyName("modelId")]
    public int ModelId { get; set; }

    [JsonPropertyName("modelCode")]
    public string? ModelCode { get; set; }

    [JsonPropertyName("stayNodeNumber")]
    public int StayNodeNumber { get; set; }

    [JsonPropertyName("stayNodeX")]
    public double StayNodeX { get; set; }

    [JsonPropertyName("stayNodeY")]
    public double StayNodeY { get; set; }

    [JsonPropertyName("orientation")]
    public double Orientation { get; set; }

    [JsonPropertyName("status")]
    public int Status { get; set; }

    [JsonPropertyName("mapCode")]
    public string? MapCode { get; set; }

    [JsonPropertyName("floorNumber")]
    public string? FloorNumber { get; set; }

    [JsonPropertyName("isCarry")]
    public int IsCarry { get; set; }

    [JsonPropertyName("loadStatus")]
    public int LoadStatus { get; set; }

    [JsonPropertyName("entranceNodeNumber")]
    public int EntranceNodeNumber { get; set; }

    [JsonPropertyName("enterNodeNumber")]
    public int EnterNodeNumber { get; set; }

    [JsonPropertyName("defaultNodeNumber")]
    public int DefaultNodeNumber { get; set; }

    [JsonPropertyName("inMapStatus")]
    public int InMapStatus { get; set; }

    [JsonPropertyName("checkCode")]
    public string? CheckCode { get; set; }

    [JsonPropertyName("buildingCode")]
    public string? BuildingCode { get; set; }

    [JsonPropertyName("shelfSlot")]
    public string? ShelfSlot { get; set; }
}
