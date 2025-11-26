using System.Text.Json.Serialization;

namespace QES_KUKA_AMR_API_Simulator.Models.MobileRobot;

public class RobotRealtimeDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("robotId")]
    public string RobotId { get; set; } = string.Empty;

    [JsonPropertyName("jobId")]
    public string? JobId { get; set; }

    [JsonPropertyName("robotOrientation")]
    public double RobotOrientation { get; set; }

    [JsonPropertyName("velocity")]
    public double Velocity { get; set; }

    [JsonPropertyName("accelerationVelocity")]
    public double AccelerationVelocity { get; set; }

    [JsonPropertyName("decelerationVelocity")]
    public double DecelerationVelocity { get; set; }

    [JsonPropertyName("angularVelocity")]
    public double AngularVelocity { get; set; }

    [JsonPropertyName("angularAccelerationVelocity")]
    public double AngularAccelerationVelocity { get; set; }

    [JsonPropertyName("angularDecelerationVelocity")]
    public double AngularDecelerationVelocity { get; set; }

    [JsonPropertyName("batteryTemperature")]
    public double BatteryTemperature { get; set; }

    [JsonPropertyName("batteryCurrent")]
    public double BatteryCurrent { get; set; }

    [JsonPropertyName("batteryVoltage")]
    public double BatteryVoltage { get; set; }

    [JsonPropertyName("batteryLevel")]
    public double BatteryLevel { get; set; }

    [JsonPropertyName("batteryIsCharging")]
    public bool BatteryIsCharging { get; set; }

    [JsonPropertyName("containerOrientation")]
    public double? ContainerOrientation { get; set; }

    [JsonPropertyName("containerCode")]
    public string? ContainerCode { get; set; }

    [JsonPropertyName("containerModelCode")]
    public string? ContainerModelCode { get; set; }

    [JsonPropertyName("containerType")]
    public int? ContainerType { get; set; }

    [JsonPropertyName("robotTypeCode")]
    public string? RobotTypeCode { get; set; }

    [JsonPropertyName("robotStatus")]
    public int RobotStatus { get; set; }

    [JsonPropertyName("mapCode")]
    public string? MapCode { get; set; }

    [JsonPropertyName("floorNumber")]
    public string? FloorNumber { get; set; }

    [JsonPropertyName("connectionState")]
    public int ConnectionState { get; set; }

    [JsonPropertyName("ipAddress")]
    public string? IpAddress { get; set; }

    [JsonPropertyName("warningLevel")]
    public int WarningLevel { get; set; }

    [JsonPropertyName("warningCode")]
    public string? WarningCode { get; set; }

    [JsonPropertyName("warningMessage")]
    public string? WarningMessage { get; set; }

    [JsonPropertyName("missionErrorCode")]
    public string? MissionErrorCode { get; set; }

    [JsonPropertyName("missionErrorLevel")]
    public int MissionErrorLevel { get; set; }

    [JsonPropertyName("forwardEdges")]
    public List<object> ForwardEdges { get; set; } = new();

    [JsonPropertyName("obstacleCoordinateX")]
    public double ObstacleCoordinateX { get; set; }

    [JsonPropertyName("obstacleCoordinateY")]
    public double ObstacleCoordinateY { get; set; }

    [JsonPropertyName("obstacleCoordinateZ")]
    public double ObstacleCoordinateZ { get; set; }

    [JsonPropertyName("collisionType")]
    public int CollisionType { get; set; }

    [JsonPropertyName("controllerWarningLevel")]
    public int ControllerWarningLevel { get; set; }

    [JsonPropertyName("controllerWarningCode")]
    public string? ControllerWarningCode { get; set; }

    [JsonPropertyName("controllerWarningMessage")]
    public string? ControllerWarningMessage { get; set; }

    [JsonPropertyName("missionCode")]
    public string? MissionCode { get; set; }

    [JsonPropertyName("missionTemplateCode")]
    public string? MissionTemplateCode { get; set; }

    [JsonPropertyName("missionTemplateName")]
    public string? MissionTemplateName { get; set; }

    [JsonPropertyName("lastNodeNumber")]
    public int LastNodeNumber { get; set; }

    [JsonPropertyName("reliability")]
    public double Reliability { get; set; }

    [JsonPropertyName("runTime")]
    public int RunTime { get; set; }

    [JsonPropertyName("karOsVersion")]
    public string? KarOsVersion { get; set; }

    [JsonPropertyName("mileage")]
    public double Mileage { get; set; }

    [JsonPropertyName("leftMotorTemperature")]
    public double LeftMotorTemperature { get; set; }

    [JsonPropertyName("rightMotorTemperature")]
    public double RightMotorTemperature { get; set; }

    [JsonPropertyName("liftMotorTemperature")]
    public double LiftMotorTemperature { get; set; }

    [JsonPropertyName("rotateMotorTemperature")]
    public double RotateMotorTemperature { get; set; }

    [JsonPropertyName("rotateTimes")]
    public int RotateTimes { get; set; }

    [JsonPropertyName("liftTimes")]
    public int LiftTimes { get; set; }

    [JsonPropertyName("errorTime")]
    public string? ErrorTime { get; set; }

    [JsonPropertyName("areaName")]
    public string? AreaName { get; set; }

    [JsonPropertyName("navigateMode")]
    public string? NavigateMode { get; set; }

    [JsonPropertyName("runningMode")]
    public string? RunningMode { get; set; }

    [JsonPropertyName("palletOrientation")]
    public double PalletOrientation { get; set; }

    [JsonPropertyName("liftState")]
    public bool LiftState { get; set; }

    [JsonPropertyName("emergency")]
    public bool Emergency { get; set; }

    [JsonPropertyName("chargingRemaining")]
    public int ChargingRemaining { get; set; }

    [JsonPropertyName("chargeTimes")]
    public int ChargeTimes { get; set; }

    [JsonPropertyName("wifiSignalQuality")]
    public int WifiSignalQuality { get; set; }

    [JsonPropertyName("dipAngleSensorAstatus")]
    public int? DipAngleSensorAstatus { get; set; }

    [JsonPropertyName("dipAngleSensorBstatus")]
    public int? DipAngleSensorBstatus { get; set; }

    [JsonPropertyName("liftContactSensorAstatus")]
    public int? LiftContactSensorAstatus { get; set; }

    [JsonPropertyName("liftContactSensorBstatus")]
    public int? LiftContactSensorBstatus { get; set; }

    [JsonPropertyName("leftMoveMotorTemperature")]
    public double? LeftMoveMotorTemperature { get; set; }

    [JsonPropertyName("rightMoveMotorTemperature")]
    public double? RightMoveMotorTemperature { get; set; }

    [JsonPropertyName("batteryHealth")]
    public int BatteryHealth { get; set; }

    [JsonPropertyName("obliquitySensorStatus")]
    public int ObliquitySensorStatus { get; set; }

    [JsonPropertyName("liftContactSensorStatus")]
    public int LiftContactSensorStatus { get; set; }

    [JsonPropertyName("leftFrontMoveMotorTemperature")]
    public double LeftFrontMoveMotorTemperature { get; set; }

    [JsonPropertyName("rightFrontMoveMotorTemperature")]
    public double RightFrontMoveMotorTemperature { get; set; }

    [JsonPropertyName("leftRearMoveMotorTemperature")]
    public double LeftRearMoveMotorTemperature { get; set; }

    [JsonPropertyName("rightRearMoveMotorTemperature")]
    public double RightRearMoveMotorTemperature { get; set; }

    [JsonPropertyName("actuatorStatusInfo")]
    public string? ActuatorStatusInfo { get; set; }

    [JsonPropertyName("trailerNum")]
    public int? TrailerNum { get; set; }

    [JsonPropertyName("tractionStatus")]
    public int? TractionStatus { get; set; }

    [JsonPropertyName("formationStatus")]
    public bool FormationStatus { get; set; }

    [JsonPropertyName("xCoordinate")]
    public double XCoordinate { get; set; }

    [JsonPropertyName("yCoordinate")]
    public double YCoordinate { get; set; }
}
