using System.Text.Json.Serialization;

namespace QES_KUKA_AMR_API.Models.Missions;

public class RobotDataDto
{
    [JsonPropertyName("robotId")]
    public string RobotId { get; set; } = string.Empty;

    [JsonPropertyName("robotType")]
    public string? RobotType { get; set; }

    [JsonPropertyName("mapCode")]
    public string? MapCode { get; set; }

    [JsonPropertyName("floorNumber")]
    public string? FloorNumber { get; set; }

    [JsonPropertyName("buildingCode")]
    public string? BuildingCode { get; set; }

    [JsonPropertyName("containerCode")]
    public string? ContainerCode { get; set; }

    [JsonPropertyName("status")]
    public int Status { get; set; }

    [JsonPropertyName("occupyStatus")]
    public int? OccupyStatus { get; set; }

    [JsonPropertyName("batteryLevel")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int? BatteryLevel { get; set; }

    [JsonPropertyName("nodeCode")]
    public string? NodeCode { get; set; }

    [JsonPropertyName("nodeLabel")]
    public string? NodeLabel { get; set; }

    [JsonPropertyName("nodeNumber")]
    public int? NodeNumber { get; set; }

    [JsonPropertyName("x")]
    public string? X { get; set; }

    [JsonPropertyName("y")]
    public string? Y { get; set; }

    [JsonPropertyName("robotOrientation")]
    public string? RobotOrientation { get; set; }

    [JsonPropertyName("missionCode")]
    public string? MissionCode { get; set; }

    [JsonPropertyName("liftStatus")]
    public int? LiftStatus { get; set; }

    [JsonPropertyName("reliability")]
    public int? Reliability { get; set; }

    [JsonPropertyName("runTime")]
    public string? RunTime { get; set; }

    [JsonPropertyName("karOsVersion")]
    public string? KarOsVersion { get; set; }

    [JsonPropertyName("mileage")]
    public string? Mileage { get; set; }

    [JsonPropertyName("leftMotorTemperature")]
    public string? LeftMotorTemperature { get; set; }

    [JsonPropertyName("rightMotorTemperature")]
    public string? RightMotorTemperature { get; set; }

    [JsonPropertyName("rotateMotorTemperature")]
    public string? RotateMotorTemperature { get; set; }

    [JsonPropertyName("liftMtrTemp")]
    public string? LiftMtrTemp { get; set; }

    [JsonPropertyName("leftFrtMovMtrTemp")]
    public string? LeftFrtMovMtrTemp { get; set; }

    [JsonPropertyName("rightFrtMovMtrTemp")]
    public string? RightFrtMovMtrTemp { get; set; }

    [JsonPropertyName("leftReMovMtrTemp")]
    public string? LeftReMovMtrTemp { get; set; }

    [JsonPropertyName("rightReMovMtrTemp")]
    public string? RightReMovMtrTemp { get; set; }

    [JsonPropertyName("rotateTimes")]
    public int? RotateTimes { get; set; }

    [JsonPropertyName("liftTimes")]
    public int? LiftTimes { get; set; }

    [JsonPropertyName("nodeForeignCode")]
    public string? NodeForeignCode { get; set; }

    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; set; }
}
