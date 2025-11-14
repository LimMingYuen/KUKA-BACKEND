using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QES_KUKA_AMR_API_Simulator.Models;
using QES_KUKA_AMR_API_Simulator.Models.MobileRobot;

namespace QES_KUKA_AMR_API_Simulator.Controllers.Data;

[ApiController]
[Authorize]
[Route("api/v1/data/mobile-robot")]
public class MobileRobotController : ControllerBase
{
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
           Status = 1,
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
           Status = 1,
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
           Status = 1,
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
}
