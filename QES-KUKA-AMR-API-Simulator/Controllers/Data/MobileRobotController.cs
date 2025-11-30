using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QES_KUKA_AMR_API_Simulator.Models;
using QES_KUKA_AMR_API_Simulator.Models.MobileRobot;
using QES_KUKA_AMR_API_Simulator.Services;

namespace QES_KUKA_AMR_API_Simulator.Controllers.Data;

[ApiController]
[Authorize]
[Route("api/v1/data/mobile-robot")]
public class MobileRobotController : ControllerBase
{
    private readonly RobotSimulationService _simulationService;

    public MobileRobotController(RobotSimulationService simulationService)
    {
        _simulationService = simulationService;
    }

    private static readonly IReadOnlyList<RobotRealtimeDto> RobotRealtimeList = new[]
    {
        new RobotRealtimeDto
        {
            Id = 7, Index = 798, RobotId = "1005", JobId = "5",
            RobotOrientation = 0, Velocity = 0, AccelerationVelocity = 0, DecelerationVelocity = 0,
            AngularVelocity = 0, AngularAccelerationVelocity = 0, AngularDecelerationVelocity = 0,
            BatteryTemperature = 0, BatteryCurrent = 0, BatteryVoltage = 0, BatteryLevel = 1, BatteryIsCharging = false,
            RobotTypeCode = "KMP 400i diffDrive", RobotStatus = 3, MapCode = "Sim1", FloorNumber = "1",
            ConnectionState = 1, IpAddress = "127.0.0.1:50422", WarningLevel = 0, WarningCode = "",
            WarningMessage = "0x1 小车空闲", MissionErrorCode = "NO_ERROR", MissionErrorLevel = 0,
            CollisionType = 0, ControllerWarningLevel = 0, ControllerWarningCode = "", ControllerWarningMessage = "",
            MissionCode = "", LastNodeNumber = 53, Reliability = 1, RunTime = 5, KarOsVersion = "",
            NavigateMode = "SLAM", RunningMode = "AUTO", XCoordinate = 22.251, YCoordinate = 63.943
        },
        new RobotRealtimeDto
        {
            Id = 3, Index = 795, RobotId = "1003", JobId = "0",
            RobotOrientation = 0, Velocity = 0, AccelerationVelocity = 0, DecelerationVelocity = 0,
            AngularVelocity = 0, AngularAccelerationVelocity = 0, AngularDecelerationVelocity = 0,
            BatteryTemperature = 0, BatteryCurrent = 0, BatteryVoltage = 0, BatteryLevel = 1, BatteryIsCharging = false,
            RobotTypeCode = "KMP 400i diffDrive", RobotStatus = 3, MapCode = "Sim1", FloorNumber = "1",
            ConnectionState = 1, IpAddress = "127.0.0.1:50424", WarningLevel = 0, WarningCode = "",
            WarningMessage = "0x1 小车空闲", MissionErrorCode = "NO_ERROR", MissionErrorLevel = 0,
            CollisionType = 0, ControllerWarningLevel = 0, ControllerWarningCode = "", ControllerWarningMessage = "",
            MissionCode = "", LastNodeNumber = 8, Reliability = 1, RunTime = 0, KarOsVersion = "",
            NavigateMode = "SLAM", RunningMode = "AUTO", XCoordinate = 19.003, YCoordinate = 32.231
        },
        new RobotRealtimeDto
        {
            Id = 1, Index = 793, RobotId = "1001", JobId = "0",
            RobotOrientation = 0, Velocity = 0, AccelerationVelocity = 0, DecelerationVelocity = 0,
            AngularVelocity = 0, AngularAccelerationVelocity = 0, AngularDecelerationVelocity = 0,
            BatteryTemperature = 0, BatteryCurrent = 0, BatteryVoltage = 0, BatteryLevel = 1, BatteryIsCharging = false,
            RobotTypeCode = "KMP 400i diffDrive", RobotStatus = 3, MapCode = "Sim1", FloorNumber = "1",
            ConnectionState = 1, IpAddress = "127.0.0.1:50426", WarningLevel = 0, WarningCode = "",
            WarningMessage = "0x1 小车空闲", MissionErrorCode = "NO_ERROR", MissionErrorLevel = 0,
            CollisionType = 0, ControllerWarningLevel = 0, ControllerWarningCode = "", ControllerWarningMessage = "",
            MissionCode = "", LastNodeNumber = 12, Reliability = 1, RunTime = 0, KarOsVersion = "",
            NavigateMode = "SLAM", RunningMode = "AUTO", XCoordinate = 51.135, YCoordinate = 32.231
        }
    };

    private static readonly IReadOnlyList<ContainerRealtimeDto> ContainerRealtimeList = new[]
    {
        new ContainerRealtimeDto
        {
            Id = 1, Code = "1-1", ContainerType = 1, ModelId = 1, ModelCode = "1",
            StayNodeNumber = 19, StayNodeX = 19.003, StayNodeY = 14.914, Orientation = 0, Status = 0,
            MapCode = "Sim1", FloorNumber = "1", IsCarry = 0, LoadStatus = 1, EntranceNodeNumber = 3,
            EnterNodeNumber = -1, DefaultNodeNumber = -1, InMapStatus = 1, BuildingCode = "sim1"
        },
        new ContainerRealtimeDto
        {
            Id = 2, Code = "1-2", ContainerType = 1, ModelId = 1, ModelCode = "1",
            StayNodeNumber = 24, StayNodeX = 19.003, StayNodeY = 6.256, Orientation = 0, Status = 0,
            MapCode = "Sim1", FloorNumber = "1", IsCarry = 0, LoadStatus = 0, EntranceNodeNumber = 14,
            EnterNodeNumber = -1, DefaultNodeNumber = -1, InMapStatus = 1, BuildingCode = "sim1"
        },
        new ContainerRealtimeDto
        {
            Id = 3, Code = "1-3", ContainerType = 1, ModelId = 1, ModelCode = "1",
            StayNodeNumber = 6, StayNodeX = 51.135, StayNodeY = 40.89, Orientation = 0, Status = 0,
            MapCode = "Sim1", FloorNumber = "1", IsCarry = 0, LoadStatus = 0, EntranceNodeNumber = 2,
            EnterNodeNumber = -1, DefaultNodeNumber = -1, InMapStatus = 1, BuildingCode = "sim1"
        },
        new ContainerRealtimeDto
        {
            Id = 4, Code = "1-4", ContainerType = 1, ModelId = 1, ModelCode = "1",
            StayNodeNumber = 52, StayNodeX = 20.31, StayNodeY = 62.145, Orientation = 0, Status = 0,
            MapCode = "Sim1", FloorNumber = "1", IsCarry = 0, LoadStatus = 0, EntranceNodeNumber = 50,
            EnterNodeNumber = -1, DefaultNodeNumber = -1, InMapStatus = 1, BuildingCode = "sim1"
        },
        new ContainerRealtimeDto
        {
            Id = 8, Code = "1-8", ContainerType = 1, ModelId = 1, ModelCode = "1",
            StayNodeNumber = 20, StayNodeX = 27.036, StayNodeY = 14.914, Orientation = 0, Status = 0,
            MapCode = "Sim1", FloorNumber = "1", IsCarry = 0, LoadStatus = 0, EntranceNodeNumber = 25,
            EnterNodeNumber = -1, DefaultNodeNumber = -1, InMapStatus = 1, CheckCode = "1-8", BuildingCode = "sim1"
        },
        new ContainerRealtimeDto
        {
            Id = 11, Code = "1-10", ContainerType = 1, ModelId = 1, ModelCode = "1",
            StayNodeNumber = 25, StayNodeX = 27.036, StayNodeY = 6.256, Orientation = 0, Status = 0,
            MapCode = "Sim1", FloorNumber = "1", IsCarry = 0, LoadStatus = 0, EntranceNodeNumber = 10,
            EnterNodeNumber = -1, DefaultNodeNumber = -1, InMapStatus = 1, CheckCode = "1-10", BuildingCode = "sim1"
        }
    };

    private static readonly IReadOnlyList<MobileRobotDto> MobileRobots = new[]
    {
        new MobileRobotDto
        {
           Id = 3,
           CreateTime = "2025-09-06 20:28:01",
           CreateBy = "kuka",
           CreateApp = "OptionalCollection:312",
           LastUpdateTime = "2025-10-28 11:58:04",
           LastUpdateBy = "kuka",
           LastUpdateApp = "MobileRobotManager:106",
           RobotId = "1003",
           RobotTypeCode = "KMP 400i diffDrive",
           BuildingCode = "sim1",
           MapCode = "Sim1",
           FloorNumber = "1",
           LastNodeNumber = 8,
           LastNodeDeleteFlag = false,
           ContainerCode = "",
           ActuatorType = -1,
           ActuatorStatusInfo = "",
           IpAddress = "127.0.0.1:49889",
           WarningInfo = "noError-idle",
           ConfigVersion = "",
           SendConfigVersion = "",
           SendConfigTime = null,
           FirmwareVersion = "",
           SendFirmwareVersion = "",
           SendFirmwareTime = null,
           Status = 3,
           OccupyStatus = 0,
           BatteryLevel = 1,
           Mileage = 0,
           MissionCode = "",
           MeetObstacleStatus = 0,
           RobotOrientation = 90,
           Reliability = 1,
           RunTime = 18641,
           RobotTypeClass = 1,
           TrailerNum = null,
           TractionStatus = null,
           XCoordinate = 18.996,
           YCoordinate = 32.231
        },
        new MobileRobotDto
        {
           Id = 1,
           CreateTime = "2025-08-25 11:44:58",
           CreateBy = "kuka",
           CreateApp = "OptionalCollection:312",
           LastUpdateTime = "2025-10-28 11:52:18",
           LastUpdateBy = "kuka",
           LastUpdateApp = "MobileRobotManager:106",
           RobotId = "1001",
           RobotTypeCode = "KMP 400i diffDrive",
           BuildingCode = "sim1",
           MapCode = "Sim1",
           FloorNumber = "1",
           LastNodeNumber = 12,
           LastNodeDeleteFlag = false,
           ContainerCode = "",
           ActuatorType = -1,
           ActuatorStatusInfo = "",
           IpAddress = "127.0.0.1:49893",
           WarningInfo = "noError-idle",
           ConfigVersion = "",
           SendConfigVersion = "",
           SendConfigTime = null,
           FirmwareVersion = "",
           SendFirmwareVersion = "",
           SendFirmwareTime = null,
           Status = 3,
           OccupyStatus = 0,
           BatteryLevel = 1,
           Mileage = 0,
           MissionCode = "",
           MeetObstacleStatus = 0,
           RobotOrientation = -90,
           Reliability = 1,
           RunTime = 18635,
           RobotTypeClass = 1,
           TrailerNum = null,
           TractionStatus = null,
           XCoordinate = 51.131,
           YCoordinate = 32.231
        },
        new MobileRobotDto
        {
           Id = 2,
           CreateTime = "2025-08-25 11:45:29",
           CreateBy = "kuka",
           CreateApp = "OptionalCollection:312",
           LastUpdateTime = "2025-10-15 21:37:31",
           LastUpdateBy = "kuka",
           LastUpdateApp = "MobileRobotManager:106",
           RobotId = "1002",
           RobotTypeCode = "KMP 400i diffDrive",
           BuildingCode = "sim1",
           MapCode = "Sim1",
           FloorNumber = "1",
           LastNodeNumber = 8,
           LastNodeDeleteFlag = false,
           ContainerCode = "",
           ActuatorType = -1,
           ActuatorStatusInfo = "",
           IpAddress = "127.0.0.1:49891",
           WarningInfo = "noError-idle",
           ConfigVersion = "",
           SendConfigVersion = "",
           SendConfigTime = null,
           FirmwareVersion = "",
           SendFirmwareVersion = "",
           SendFirmwareTime = null,
           Status = 3,
           OccupyStatus = 0,
           BatteryLevel = 1,
           Mileage = 0,
           MissionCode = "",
           MeetObstacleStatus = 0,
           RobotOrientation = null,
           Reliability = null,
           RunTime = null,
           RobotTypeClass = 1,
           TrailerNum = null,
           TractionStatus = null,
           XCoordinate = null,
           YCoordinate = null
        },
        new MobileRobotDto
        {
           Id = 4,
           CreateTime = "2025-09-11 14:56:37",
           CreateBy = "kuka",
           CreateApp = "OptionalCollection:312",
           LastUpdateTime = "2025-10-15 21:37:31",
           LastUpdateBy = "kuka",
           LastUpdateApp = "MobileRobotManager:106",
           RobotId = "1004",
           RobotTypeCode = "KMP 400i diffDrive",
           BuildingCode = "sim1",
           MapCode = "Sim1",
           FloorNumber = "1",
           LastNodeNumber = 8,
           LastNodeDeleteFlag = false,
           ContainerCode = "",
           ActuatorType = -1,
           ActuatorStatusInfo = "",
           IpAddress = "127.0.0.1:49887",
           WarningInfo = "noError-idle",
           ConfigVersion = "",
           SendConfigVersion = "",
           SendConfigTime = null,
           FirmwareVersion = "",
           SendFirmwareVersion = "",
           SendFirmwareTime = null,
           Status = 3,
           OccupyStatus = 0,
           BatteryLevel = 1,
           Mileage = 0,
           MissionCode = "",
           MeetObstacleStatus = 0,
           RobotOrientation = null,
           Reliability = null,
           RunTime = null,
           RobotTypeClass = 1,
           TrailerNum = null,
           TractionStatus = null,
           XCoordinate = null,
           YCoordinate = null
        },
        new MobileRobotDto
        {
           Id = 6,
           CreateTime = "2025-09-30 10:09:07",
           CreateBy = "kuka",
           CreateApp = "OptionalCollection:312",
           LastUpdateTime = "2025-10-15 13:18:05",
           LastUpdateBy = "kuka",
           LastUpdateApp = "MobileRobotManager:106",
           RobotId = "10011",
           RobotTypeCode = "KMP 400i diffDrive",
           BuildingCode = "sim1",
           MapCode = "Sim1",
           FloorNumber = "1",
           LastNodeNumber = 12,
           LastNodeDeleteFlag = false,
           ContainerCode = "",
           ActuatorType = -1,
           ActuatorStatusInfo = "",
           IpAddress = "127.0.0.1:49895",
           WarningInfo = "noError-idle",
           ConfigVersion = "",
           SendConfigVersion = "",
           SendConfigTime = null,
           FirmwareVersion = "",
           SendFirmwareVersion = "",
           SendFirmwareTime = null,
           Status = 3,
           OccupyStatus = 0,
           BatteryLevel = 1,
           Mileage = 0,
           MissionCode = "",
           MeetObstacleStatus = 0,
           RobotOrientation = null,
           Reliability = null,
           RunTime = null,
           RobotTypeClass = 1,
           TrailerNum = null,
           TractionStatus = null,
           XCoordinate = null,
           YCoordinate = null
        }
    };

    [HttpPost("list")]
    public ActionResult<ApiResponse<MobileRobotPage>> GetMobileRobotList([FromBody] MobileRobotRequest request)
    {
        if (request.PageSize <= 0)
        {
            request.PageSize = 10000;
        }

        if (request.PageNum <= 0)
        {
            request.PageNum = 1;
        }
        var totalRecords = MobileRobots.Count;
        var totalPages = (int)Math.Ceiling((double)totalRecords / request.PageSize);

        var skip = (request.PageNum - 1) * request.PageSize;
        var pagedMobileRobot = MobileRobots.Skip(skip).Take(request.PageSize).ToList();

        var response = new ApiResponse<MobileRobotPage>
        {
            Succ = true,
            Code = 200,
            Msg = string.Empty,
            Data= new MobileRobotPage
            {
                TotalPages = totalPages,
                TotalRecords = totalRecords,
                Page = request.PageNum,
                Size = request.PageSize,
                Content = pagedMobileRobot
            }
        };

        return Ok(response);
    }

    [HttpGet("getRealtimeInfo")]
    public ActionResult<RealtimeInfoResponse> GetRealtimeInfo(
        [FromQuery] string? floorNumber,
        [FromQuery] bool isFirst = false,
        [FromQuery] string? mapCode = null)
    {
        // Get dynamic robot positions from simulation service
        var dynamicRobots = _simulationService.GetRobotRealtimeList(mapCode, floorNumber);

        // Filter containers by mapCode and floorNumber if provided
        var filteredContainers = ContainerRealtimeList.AsEnumerable();

        if (!string.IsNullOrEmpty(mapCode))
        {
            filteredContainers = filteredContainers.Where(c =>
                string.Equals(c.MapCode, mapCode, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrEmpty(floorNumber))
        {
            filteredContainers = filteredContainers.Where(c =>
                string.Equals(c.FloorNumber, floorNumber, StringComparison.OrdinalIgnoreCase));
        }

        var response = new RealtimeInfoResponse
        {
            Success = true,
            Code = null,
            Message = null,
            Data = new RealtimeInfoData
            {
                RobotRealtimeList = dynamicRobots,
                ContainerRealtimeList = filteredContainers.ToList(),
                ErrorRobotList = new List<RobotRealtimeDto>()
            }
        };

        return Ok(response);
    }
}
