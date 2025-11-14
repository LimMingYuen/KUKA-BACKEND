using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QES_KUKA_AMR_API_Simulator.Models;
using QES_KUKA_AMR_API_Simulator.Models.MapZone;

namespace QES_KUKA_AMR_API_Simulator.Controllers.Data;

[ApiController]
[Authorize]
[Route("api/v1/data/map-zone/cascade")]
public class MapZoneController : ControllerBase
{
    public static readonly IReadOnlyList<MapZoneDto> MapZones = new[]
    {
      new MapZoneDto
        {
            Id = 2,
            CreateTime = "2025-08-25 11:50:39",
            CreateBy = "admin",
            CreateApp = "MapZoneManager:61",
            LastUpdateTime = "2025-08-25 11:50:39",
            LastUpdateBy = "admin",
            LastUpdateApp = "MapZoneManager:61",
            ZoneName = "AreaPark1001",
            ZoneCode = "Sim1-1-1756093839812",
            ZoneDescription = "",
            ZoneColor = "",
            MapCode = "Sim1",
            FloorNumber = "1",
            Points = "[{\"x1\":49.287,\"y1\":33.598,\"x3\":52.72,\"y3\":30.837,\"x2\":52.72,\"y2\":33.598,\"x4\":49.287,\"y4\":30.837}]",
            Nodes = "12",
            Edges = "47,48,57,58,127,128",
            CustomerUi = "{\"zoneColor\":\"red\",\"nameCustomStyle\":{\"fontSize\":16,\"fontColor\":\"rgba(255,0,0,0.25)\",\"hori\":\"left\",\"verti\":\"top\"},\"descCustomStyle\":{\"fontSize\":12,\"fontColor\":\"rgba(255,0,0,0.25)\",\"hori\":\"right\",\"verti\":\"bottom\"}}",
            ZoneType = "6",
            Status = 1,
            BeginTime = null,
            EndTime = null,
            Configs = new MapZoneConfigsDto
            {
                SpecifiedRobotIds = "1001",
                AgvSelection = "2",
                RobotZoneMode = "1",
                RestSelection = "2",
                ChargeSelection = "2",
                SelectRobotStrategy = "3",
                Interval = "",
                Radius = "",
                Attempts = "99999"
            }
        },
        new MapZoneDto
        {
            Id = 9,
            CreateTime = "2025-08-25 12:20:32",
            CreateBy = "admin",
            CreateApp = "MapZoneManager:61",
            LastUpdateTime = "2025-08-25 13:28:53",
            LastUpdateBy = "admin",
            LastUpdateApp = "MapZoneRecalculateObserver:86",
            ZoneName = "Area3",
            ZoneCode = "Sim1-1-1756095632271",
            ZoneDescription = "",
            ZoneColor = "",
            MapCode = "Sim1",
            FloorNumber = "1",
            Points = "[{\"x1\":17.197,\"y1\":17.111,\"x3\":28.503,\"y3\":4.372,\"x2\":28.503,\"y2\":17.111,\"x4\":17.197,\"y4\":4.372}]",
            Nodes = "19,20,24,25,37,38",
            Edges = "75,76,79,80,87,88,99,100,107,108,109,110,139,140,143,144,161,162,163,164,165,166,167,168",
            CustomerUi = "{\"zoneColor\":\"red\",\"nameCustomStyle\":{\"fontSize\":16,\"fontColor\":\"rgba(255,0,0,0.25)\",\"hori\":\"left\",\"verti\":\"top\"},\"descCustomStyle\":{\"fontSize\":12,\"fontColor\":\"rgba(255,0,0,0.25)\",\"hori\":\"right\",\"verti\":\"bottom\"}}",
            ZoneType = "3",
            Status = 1,
            BeginTime = null,
            EndTime = null,
            Configs = new MapZoneConfigsDto
            {
                CarryStrategy = "1",
                AreaType = "1",
                AreaDefaultContainerModelCode = "1",
                AreaNodeList = "[{\"cellCode\":\"Sim1-1-19\",\"sort\":1},{\"cellCode\":\"Sim1-1-20\",\"sort\":2},{\"cellCode\":\"Sim1-1-24\",\"sort\":3},{\"cellCode\":\"Sim1-1-25\",\"sort\":4}]"
            }
        },
        new MapZoneDto
        {
            Id = 8,
            CreateTime = "2025-08-25 12:18:36",
            CreateBy = "admin",
            CreateApp = "MapZoneManager:61",
            LastUpdateTime = "2025-08-25 12:18:36",
            LastUpdateBy = "admin",
            LastUpdateApp = "MapZoneManager:61",
            ZoneName = "Area2",
            ZoneCode = "Sim1-1-1756095516769",
            ZoneDescription = "",
            ZoneColor = "",
            MapCode = "Sim1",
            FloorNumber = "1",
            Points = "[{\"x1\":40.764,\"y1\":51.347,\"x3\":52.548,\"y3\":39.404,\"x2\":52.548,\"y2\":51.347,\"x4\":40.764,\"y4\":39.404}]",
            Nodes = "5,6,16,17",
            Edges = "37,38,39,40,51,52,53,54,57,58,59,60,71,72,73,74",
            CustomerUi = "{\"zoneColor\":\"red\",\"nameCustomStyle\":{\"fontSize\":16,\"fontColor\":\"rgba(255,0,0,0.25)\",\"hori\":\"left\",\"verti\":\"top\"},\"descCustomStyle\":{\"fontSize\":12,\"fontColor\":\"rgba(255,0,0,0.25)\",\"hori\":\"right\",\"verti\":\"bottom\"}}",
            ZoneType = "3",
            Status = 1,
            BeginTime = null,
            EndTime = null,
            Configs = new MapZoneConfigsDto
            {
                CarryStrategy = "1",
                AreaType = "1",
                AreaDefaultContainerModelCode = "1",
                AreaNodeList = "[{\"cellCode\":\"Sim1-1-5\",\"sort\":1},{\"cellCode\":\"Sim1-1-6\",\"sort\":2},{\"cellCode\":\"Sim1-1-16\",\"sort\":3},{\"cellCode\":\"Sim1-1-17\",\"sort\":4}]"
            }
        },
        new MapZoneDto
        {
            Id = 10,
            CreateTime = "2025-08-25 12:20:53",
            CreateBy = "admin",
            CreateApp = "MapZoneManager:61",
            LastUpdateTime = "2025-08-25 12:20:53",
            LastUpdateBy = "admin",
            LastUpdateApp = "MapZoneManager:61",
            ZoneName = "Area4",
            ZoneCode = "Sim1-1-1756095653574",
            ZoneDescription = "",
            ZoneColor = "",
            MapCode = "Sim1",
            FloorNumber = "1",
            Points = "[{\"x1\":41.561,\"y1\":16.474,\"x3\":53.185,\"y3\":4.69,\"x2\":53.185,\"y2\":16.474,\"x4\":41.561,\"y4\":4.69}]",
            Nodes = "22,23,27,28",
            Edges = "81,82,83,84,89,90,91,92,101,102,103,104,115,116,117,118",
            CustomerUi = "{\"zoneColor\":\"red\",\"nameCustomStyle\":{\"fontSize\":16,\"fontColor\":\"rgba(255,0,0,0.25)\",\"hori\":\"left\",\"verti\":\"top\"},\"descCustomStyle\":{\"fontSize\":12,\"fontColor\":\"rgba(255,0,0,0.25)\",\"hori\":\"right\",\"verti\":\"bottom\"}}",
            ZoneType = "3",
            Status = 1,
            BeginTime = null,
            EndTime = null,
            Configs = new MapZoneConfigsDto
            {
                CarryStrategy = "1",
                AreaType = "1",
                AreaDefaultContainerModelCode = "1",
                AreaNodeList = "[{\"cellCode\":\"Sim1-1-22\",\"sort\":1},{\"cellCode\":\"Sim1-1-23\",\"sort\":1},{\"cellCode\":\"Sim1-1-27\",\"sort\":1},{\"cellCode\":\"Sim1-1-28\",\"sort\":1}]"
            }
        },
        new MapZoneDto
        {
            Id = 6,
            CreateTime = "2025-08-25 12:00:39",
            CreateBy = "admin",
            CreateApp = "MapZoneManager:61",
            LastUpdateTime = "2025-08-25 13:28:53",
            LastUpdateBy = "admin",
            LastUpdateApp = "MapZoneRecalculateObserver:86",
            ZoneName = "AreaPark1002",
            ZoneCode = "Sim1-1-1756094439802",
            ZoneDescription = "",
            ZoneColor = "",
            MapCode = "Sim1",
            FloorNumber = "1",
            Points = "[{\"x1\":17.519,\"y1\":32.95,\"x3\":20.841,\"y3\":31.119,\"x2\":20.841,\"y2\":32.95,\"x4\":17.519,\"y4\":31.119}]",
            Nodes = "8",
            Edges = "55,56,119,120,153,154",
            CustomerUi = "{\"zoneColor\":\"red\",\"nameCustomStyle\":{\"fontSize\":16,\"fontColor\":\"rgba(255,0,0,0.25)\",\"hori\":\"left\",\"verti\":\"top\"},\"descCustomStyle\":{\"fontSize\":12,\"fontColor\":\"rgba(255,0,0,0.25)\",\"hori\":\"right\",\"verti\":\"bottom\"}}",
            ZoneType = "6",
            Status = 1,
            BeginTime = null,
            EndTime = null,
            Configs = new MapZoneConfigsDto
            {
                SpecifiedRobotIds = "1002",
                AgvSelection = "2",
                RobotZoneMode = "1",
                RestSelection = "2",
                ChargeSelection = "2",
                SelectRobotStrategy = "3",
                Interval = "1",
                Radius = "9999",
                Attempts = "99999"
            }
        },
        new MapZoneDto
        {
            Id = 7,
            CreateTime = "2025-08-25 12:17:03",
            CreateBy = "admin",
            CreateApp = "MapZoneManager:61",
            LastUpdateTime = "2025-08-25 13:28:53",
            LastUpdateBy = "admin",
            LastUpdateApp = "MapZoneRecalculateObserver:86",
            ZoneName = "Area1",
            ZoneCode = "Sim1-1-1756095423769",
            ZoneDescription = "",
            ZoneColor = "",
            MapCode = "Sim1",
            FloorNumber = "1",
            Points = "[{\"x1\":16.952,\"y1\":51.107,\"x3\":28.491,\"y3\":39.116,\"x2\":28.491,\"y2\":51.107,\"x4\":16.952,\"y4\":39.116}]",
            Nodes = "1,2,3,14,33,34",
            Edges = "31,32,35,36,49,50,55,56,63,64,65,66,135,136,137,138,145,146,147,148,149,150,151,152",
            CustomerUi = "{\"zoneColor\":\"red\",\"nameCustomStyle\":{\"fontSize\":16,\"fontColor\":\"rgba(255,0,0,0.25)\",\"hori\":\"left\",\"verti\":\"top\"},\"descCustomStyle\":{\"fontSize\":12,\"fontColor\":\"rgba(255,0,0,0.25)\",\"hori\":\"right\",\"verti\":\"bottom\"}}",
            ZoneType = "3",
            Status = 1,
            BeginTime = null,
            EndTime = null,
            Configs = new MapZoneConfigsDto
            {
                CarryStrategy = "1",
                AreaType = "1",
                AreaDefaultContainerModelCode = "1",
                AreaNodeList = "[{\"cellCode\":\"Sim1-1-1\",\"sort\":1},{\"cellCode\":\"Sim1-1-2\",\"sort\":2},{\"cellCode\":\"Sim1-1-3\",\"sort\":3},{\"cellCode\":\"Sim1-1-14\",\"sort\":4}]"
            }
        }
    };

    [HttpPost("list")]
    public ActionResult<ApiResponse<MapZonePage>> GetMapZoneList([FromBody] QueryMapZonesRequest request)
    {
        if (request.PageSize <= 0)
        {
            request.PageSize = 10;
        }

        if (request.PageNum <= 0)
        {
            request.PageNum = 1;
        }

        var totalRecords = MapZones.Count;
        var totalPages = (int)Math.Ceiling((double)totalRecords / request.PageSize);

        var skip = (request.PageNum - 1) * request.PageSize;
        var pagedMapZones = MapZones.Skip(skip).Take(request.PageSize).ToList();

        var response = new ApiResponse<MapZonePage>
        {
            Succ = true,
            Code = 200,
            Msg = string.Empty,
            Data = new MapZonePage
            {
                TotalPages = totalPages,
                TotalRecords = totalRecords,
                Page = request.PageNum,
                Size = request.PageSize,
                Content = pagedMapZones
            }
        };

        return Ok(response);
    }
}
