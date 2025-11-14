using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QES_KUKA_AMR_API_Simulator.Models;
using QES_KUKA_AMR_API_Simulator.Models.QrCode;
using System;

namespace QES_KUKA_AMR_API_Simulator.Controllers.Data;

[ApiController]
[Authorize]
[Route("api/v1/data/qr-code")]
public class QrCodeController : ControllerBase
{
    private static readonly IReadOnlyList<QrCodeDto> QrCodes = new[]
    {
        new QrCodeDto
        {
            Id = 8,
            CreateTime = "2025-08-25 11:52:10",
            CreateBy = "kuka",
            CreateApp = "QrCodeManager:85",
            LastUpdateTime = "2025-08-25 11:52:10",
            LastUpdateBy = "kuka",
            LastUpdateApp = "QrCodeManager:85",
            NodeLabel = "16",
            Reliability = 100,
            MapCode = "Sim1",
            FloorNumber = "1",
            NodeNumber = 16,
            ReportTimes = 1
        },
        new QrCodeDto
        {
            Id = 9,
            CreateTime = "2025-08-25 11:52:19",
            CreateBy = "kuka",
            CreateApp = "QrCodeManager:85",
            LastUpdateTime = "2025-08-25 11:52:19",
            LastUpdateBy = "kuka",
            LastUpdateApp = "QrCodeManager:85",
            NodeLabel = "17",
            Reliability = 100,
            MapCode = "Sim1",
            FloorNumber = "1",
            NodeNumber = 17,
            ReportTimes = 1
        },
        new QrCodeDto
        {
            Id = 10,
            CreateTime = "2025-08-25 11:52:31",
            CreateBy = "kuka",
            CreateApp = "QrCodeManager:85",
            LastUpdateTime = "2025-08-25 11:52:31",
            LastUpdateBy = "kuka",
            LastUpdateApp = "QrCodeManager:85",
            NodeLabel = "6",
            Reliability = 100,
            MapCode = "Sim1",
            FloorNumber = "1",
            NodeNumber = 6,
            ReportTimes = 1
        },
        new QrCodeDto
        {
            Id = 11,
            CreateTime = "2025-08-25 11:56:32",
            CreateBy = "kuka",
            CreateApp = "QrCodeManager:85",
            LastUpdateTime = "2025-08-25 11:56:32",
            LastUpdateBy = "kuka",
            LastUpdateApp = "QrCodeManager:85",
            NodeLabel = "5",
            Reliability = 100,
            MapCode = "Sim1",
            FloorNumber = "1",
            NodeNumber = 5,
            ReportTimes = 1
        },
        new QrCodeDto
        {
            Id = 31,
            CreateTime = "2025-08-25 13:33:46",
            CreateBy = "kuka",
            CreateApp = "QrCodeManager:85",
            LastUpdateTime = "2025-10-22 13:41:51",
            LastUpdateBy = "1003",
            LastUpdateApp = "QrCodeManager:85",
            NodeLabel = "34",
            Reliability = 100,
            MapCode = "Sim1",
            FloorNumber = "1",
            NodeNumber = 34,
            ReportTimes = 70
        },
        new QrCodeDto
        {
            Id = 5,
            CreateTime = "2025-08-25 11:51:34",
            CreateBy = "kuka",
            CreateApp = "QrCodeManager:85",
            LastUpdateTime = "2025-08-25 11:56:42",
            LastUpdateBy = "1001",
            LastUpdateApp = "QrCodeManager:85",
            NodeLabel = "3",
            Reliability = 100,
            MapCode = "Sim1",
            FloorNumber = "1",
            NodeNumber = 3,
            ReportTimes = 2
        },
        new QrCodeDto
        {
            Id = 12,
            CreateTime = "2025-08-25 11:56:51",
            CreateBy = "kuka",
            CreateApp = "QrCodeManager:85",
            LastUpdateTime = "2025-08-25 11:56:51",
            LastUpdateBy = "kuka",
            LastUpdateApp = "QrCodeManager:85",
            NodeLabel = "2",
            Reliability = 100,
            MapCode = "Sim1",
            FloorNumber = "1",
            NodeNumber = 2,
            ReportTimes = 1
        },
        new QrCodeDto
        {
            Id = 15,
            CreateTime = "2025-08-25 12:12:14",
            CreateBy = "kuka",
            CreateApp = "QrCodeManager:85",
            LastUpdateTime = "2025-10-21 09:18:34",
            LastUpdateBy = "1001",
            LastUpdateApp = "QrCodeManager:85",
            NodeLabel = "RackPark6",
            Reliability = 100,
            MapCode = "Sim1",
            FloorNumber = "1",
            NodeNumber = 17,
            ReportTimes = 53
        },
        new QrCodeDto
        {
            Id = 23,
            CreateTime = "2025-08-25 12:16:40",
            CreateBy = "kuka",
            CreateApp = "QrCodeManager:85",
            LastUpdateTime = "2025-10-22 13:54:05",
            LastUpdateBy = "1003",
            LastUpdateApp = "QrCodeManager:85",
            NodeLabel = "RackPark13",
            Reliability = 100,
            MapCode = "Sim1",
            FloorNumber = "1",
            NodeNumber = 22,
            ReportTimes = 57
        },
        new QrCodeDto
        {
            Id = 22,
            CreateTime = "2025-08-25 12:16:30",
            CreateBy = "kuka",
            CreateApp = "QrCodeManager:85",
            LastUpdateTime = "2025-10-22 11:56:07",
            LastUpdateBy = "1001",
            LastUpdateApp = "QrCodeManager:85",
            NodeLabel = "RackPark14",
            Reliability = 100,
            MapCode = "Sim1",
            FloorNumber = "1",
            NodeNumber = 23,
            ReportTimes = 63
        },
        new QrCodeDto
        {
            Id = 24,
            CreateTime = "2025-08-25 12:16:45",
            CreateBy = "kuka",
            CreateApp = "QrCodeManager:85",
            LastUpdateTime = "2025-10-22 13:54:14",
            LastUpdateBy = "1003",
            LastUpdateApp = "QrCodeManager:85",
            NodeLabel = "21",
            Reliability = 100,
            MapCode = "Sim1",
            FloorNumber = "1",
            NodeNumber = 21,
            ReportTimes = 59
        },
        new QrCodeDto
        {
            Id = 14,
            CreateTime = "2025-08-25 12:12:05",
            CreateBy = "kuka",
            CreateApp = "QrCodeManager:85",
            LastUpdateTime = "2025-10-22 11:54:53",
            LastUpdateBy = "1001",
            LastUpdateApp = "QrCodeManager:85",
            NodeLabel = "RackPark8",
            Reliability = 100,
            MapCode = "Sim1",
            FloorNumber = "1",
            NodeNumber = 6,
            ReportTimes = 99
        },
        new QrCodeDto
        {
            Id = 17,
            CreateTime = "2025-08-25 12:12:34",
            CreateBy = "kuka",
            CreateApp = "QrCodeManager:85",
            LastUpdateTime = "2025-10-22 13:53:28",
            LastUpdateBy = "1003",
            LastUpdateApp = "QrCodeManager:85",
            NodeLabel = "RackPark2",
            Reliability = 100,
            MapCode = "Sim1",
            FloorNumber = "1",
            NodeNumber = 14,
            ReportTimes = 49
        },
        new QrCodeDto
        {
            Id = 7,
            CreateTime = "2025-08-25 11:52:01",
            CreateBy = "kuka",
            CreateApp = "QrCodeManager:85",
            LastUpdateTime = "2025-10-22 13:53:34",
            LastUpdateBy = "1003",
            LastUpdateApp = "QrCodeManager:85",
            NodeLabel = "15",
            Reliability = 100,
            MapCode = "Sim1",
            FloorNumber = "1",
            NodeNumber = 15,
            ReportTimes = 57
        },
        new QrCodeDto
        {
            Id = 1,
            CreateTime = "2025-08-25 11:44:58",
            CreateBy = "kuka",
            CreateApp = "QrCodeManager:85",
            LastUpdateTime = "2025-10-22 13:42:43",
            LastUpdateBy = "1003",
            LastUpdateApp = "QrCodeManager:85",
            NodeLabel = "9",
            Reliability = 100,
            MapCode = "Sim1",
            FloorNumber = "1",
            NodeNumber = 9,
            ReportTimes = 87
        },
        new QrCodeDto
        {
            Id = 16,
            CreateTime = "2025-08-25 12:12:23",
            CreateBy = "kuka",
            CreateApp = "QrCodeManager:85",
            LastUpdateTime = "2025-10-22 13:53:39",
            LastUpdateBy = "1003",
            LastUpdateApp = "QrCodeManager:85",
            NodeLabel = "RackPark5",
            Reliability = 100,
            MapCode = "Sim1",
            FloorNumber = "1",
            NodeNumber = 16,
            ReportTimes = 67
        },
        new QrCodeDto
        {
            Id = 21,
            CreateTime = "2025-08-25 12:16:22",
            CreateBy = "kuka",
            CreateApp = "QrCodeManager:85",
            LastUpdateTime = "2025-10-22 11:56:15",
            LastUpdateBy = "1001",
            LastUpdateApp = "QrCodeManager:85",
            NodeLabel = "32",
            Reliability = 100,
            MapCode = "Sim1",
            FloorNumber = "1",
            NodeNumber = 32,
            ReportTimes = 80
        },
        new QrCodeDto
        {
            Id = 25,
            CreateTime = "2025-08-25 12:16:53",
            CreateBy = "kuka",
            CreateApp = "QrCodeManager:85",
            LastUpdateTime = "2025-10-22 13:54:19",
            LastUpdateBy = "1003",
            LastUpdateApp = "QrCodeManager:85",
            NodeLabel = "RackPark10",
            Reliability = 100,
            MapCode = "Sim1",
            FloorNumber = "1",
            NodeNumber = 20,
            ReportTimes = 77
        },
        new QrCodeDto
        {
            Id = 4,
            CreateTime = "2025-08-25 11:47:58",
            CreateBy = "kuka",
            CreateApp = "QrCodeManager:85",
            LastUpdateTime = "2025-10-22 11:56:21",
            LastUpdateBy = "1001",
            LastUpdateApp = "QrCodeManager:85",
            NodeLabel = "ParkingAMR1001",
            Reliability = 100,
            MapCode = "Sim1",
            FloorNumber = "1",
            NodeNumber = 12,
            ReportTimes = 135
        },
        new QrCodeDto
        {
            Id = 19,
            CreateTime = "2025-08-25 12:15:54",
            CreateBy = "kuka",
            CreateApp = "QrCodeManager:85",
            LastUpdateTime = "2025-10-22 13:23:21",
            LastUpdateBy = "1003",
            LastUpdateApp = "QrCodeManager:85",
            NodeLabel = "RackPark4",
            Reliability = 100,
            MapCode = "Sim1",
            FloorNumber = "1",
            NodeNumber = 3,
            ReportTimes = 68
        },
        new QrCodeDto
        {
            Id = 34,
            CreateTime = "2025-08-25 13:34:08",
            CreateBy = "kuka",
            CreateApp = "QrCodeManager:85",
            LastUpdateTime = "2025-10-22 13:54:22",
            LastUpdateBy = "1003",
            LastUpdateApp = "QrCodeManager:85",
            NodeLabel = "37",
            Reliability = 100,
            MapCode = "Sim1",
            FloorNumber = "1",
            NodeNumber = 37,
            ReportTimes = 81
        },
        new QrCodeDto
        {
            Id = 39,
            CreateTime = "2025-09-07 12:56:21",
            CreateBy = "kuka",
            CreateApp = "QrCodeManager:85",
            LastUpdateTime = "2025-10-16 00:14:45",
            LastUpdateBy = "1001",
            LastUpdateApp = "QrCodeManager:85",
            NodeLabel = "RackPark12",
            Reliability = 100,
            MapCode = "Sim1",
            FloorNumber = "1",
            NodeNumber = 25,
            ReportTimes = 19
        },
        new QrCodeDto
        {
            Id = 27,
            CreateTime = "2025-08-25 12:18:50",
            CreateBy = "kuka",
            CreateApp = "QrCodeManager:85",
            LastUpdateTime = "2025-10-22 13:53:47",
            LastUpdateBy = "1003",
            LastUpdateApp = "QrCodeManager:85",
            NodeLabel = "RackPark7",
            Reliability = 100,
            MapCode = "Sim1",
            FloorNumber = "1",
            NodeNumber = 5,
            ReportTimes = 82
        },
        new QrCodeDto
        {
            Id = 33,
            CreateTime = "2025-08-25 13:34:02",
            CreateBy = "kuka",
            CreateApp = "QrCodeManager:85",
            LastUpdateTime = "2025-10-22 13:42:03",
            LastUpdateBy = "1003",
            LastUpdateApp = "QrCodeManager:85",
            NodeLabel = "35",
            Reliability = 100,
            MapCode = "Sim1",
            FloorNumber = "1",
            NodeNumber = 35,
            ReportTimes = 48
        },
        new QrCodeDto
        {
            Id = 32,
            CreateTime = "2025-08-25 13:33:57",
            CreateBy = "kuka",
            CreateApp = "QrCodeManager:85",
            LastUpdateTime = "2025-10-22 13:42:48",
            LastUpdateBy = "1003",
            LastUpdateApp = "QrCodeManager:85",
            NodeLabel = "36",
            Reliability = 100,
            MapCode = "Sim1",
            FloorNumber = "1",
            NodeNumber = 36,
            ReportTimes = 77
        },
        new QrCodeDto
        {
            Id = 18,
            CreateTime = "2025-08-25 12:12:42",
            CreateBy = "kuka",
            CreateApp = "QrCodeManager:85",
            LastUpdateTime = "2025-10-22 13:54:50",
            LastUpdateBy = "1003",
            LastUpdateApp = "QrCodeManager:85",
            NodeLabel = "RackPark1",
            Reliability = 100,
            MapCode = "Sim1",
            FloorNumber = "1",
            NodeNumber = 1,
            ReportTimes = 52
        },
        new QrCodeDto
        {
            Id = 30,
            CreateTime = "2025-08-25 13:27:12",
            CreateBy = "kuka",
            CreateApp = "QrCodeManager:85",
            LastUpdateTime = "2025-10-22 13:54:25",
            LastUpdateBy = "1003",
            LastUpdateApp = "QrCodeManager:85",
            NodeLabel = "RackPark9",
            Reliability = 100,
            MapCode = "Sim1",
            FloorNumber = "1",
            NodeNumber = 19,
            ReportTimes = 82
        },
        new QrCodeDto
        {
            Id = 3,
            CreateTime = "2025-08-25 11:47:50",
            CreateBy = "kuka",
            CreateApp = "QrCodeManager:85",
            LastUpdateTime = "2025-10-22 13:53:53",
            LastUpdateBy = "1003",
            LastUpdateApp = "QrCodeManager:85",
            NodeLabel = "11",
            Reliability = 100,
            MapCode = "Sim1",
            FloorNumber = "1",
            NodeNumber = 11,
            ReportTimes = 99
        },
        new QrCodeDto
        {
            Id = 20,
            CreateTime = "2025-08-25 12:16:02",
            CreateBy = "kuka",
            CreateApp = "QrCodeManager:85",
            LastUpdateTime = "2025-10-22 13:55:14",
            LastUpdateBy = "1003",
            LastUpdateApp = "QrCodeManager:85",
            NodeLabel = "RackPark3",
            Reliability = 100,
            MapCode = "Sim1",
            FloorNumber = "1",
            NodeNumber = 2,
            ReportTimes = 87
        },
        new QrCodeDto
        {
            Id = 13,
            CreateTime = "2025-08-25 11:57:03",
            CreateBy = "kuka",
            CreateApp = "QrCodeManager:85",
            LastUpdateTime = "2025-10-22 13:55:20",
            LastUpdateBy = "1003",
            LastUpdateApp = "QrCodeManager:85",
            NodeLabel = "8",
            Reliability = 100,
            MapCode = "Sim1",
            FloorNumber = "1",
            NodeNumber = 8,
            ReportTimes = 132
        },
        new QrCodeDto
        {
            Id = 38,
            CreateTime = "2025-09-06 23:05:50",
            CreateBy = "kuka",
            CreateApp = "QrCodeManager:85",
            LastUpdateTime = "2025-10-22 13:53:59",
            LastUpdateBy = "1003",
            LastUpdateApp = "QrCodeManager:85",
            NodeLabel = "31",
            Reliability = 100,
            MapCode = "Sim1",
            FloorNumber = "1",
            NodeNumber = 31,
            ReportTimes = 39
        },
        new QrCodeDto
        {
            Id = 40,
            CreateTime = "2025-09-07 12:56:26",
            CreateBy = "kuka",
            CreateApp = "QrCodeManager:85",
            LastUpdateTime = "2025-10-16 00:14:50",
            LastUpdateBy = "1001",
            LastUpdateApp = "QrCodeManager:85",
            NodeLabel = "26",
            Reliability = 100,
            MapCode = "Sim1",
            FloorNumber = "1",
            NodeNumber = 26,
            ReportTimes = 18
        },
        new QrCodeDto
        {
            Id = 2,
            CreateTime = "2025-08-25 11:45:29",
            CreateBy = "kuka",
            CreateApp = "QrCodeManager:85",
            LastUpdateTime = "2025-10-16 00:18:58",
            LastUpdateBy = "1001",
            LastUpdateApp = "QrCodeManager:85",
            NodeLabel = "10",
            Reliability = 100,
            MapCode = "Sim1",
            FloorNumber = "1",
            NodeNumber = 10,
            ReportTimes = 78
        },
        new QrCodeDto
        {
            Id = 41,
            CreateTime = "2025-09-07 12:56:32",
            CreateBy = "kuka",
            CreateApp = "QrCodeManager:85",
            LastUpdateTime = "2025-10-16 00:14:56",
            LastUpdateBy = "1001",
            LastUpdateApp = "QrCodeManager:85",
            NodeLabel = "RackPark15",
            Reliability = 100,
            MapCode = "Sim1",
            FloorNumber = "1",
            NodeNumber = 27,
            ReportTimes = 13
        },
        new QrCodeDto
        {
            Id = 6,
            CreateTime = "2025-08-25 11:51:47",
            CreateBy = "kuka",
            CreateApp = "QrCodeManager:85",
            LastUpdateTime = "2025-10-22 13:23:27",
            LastUpdateBy = "1003",
            LastUpdateApp = "QrCodeManager:85",
            NodeLabel = "4",
            Reliability = 100,
            MapCode = "Sim1",
            FloorNumber = "1",
            NodeNumber = 4,
            ReportTimes = 66
        },
        new QrCodeDto
        {
            Id = 37,
            CreateTime = "2025-08-25 13:34:57",
            CreateBy = "kuka",
            CreateApp = "QrCodeManager:85",
            LastUpdateTime = "2025-10-22 13:53:26",
            LastUpdateBy = "1003",
            LastUpdateApp = "QrCodeManager:85",
            NodeLabel = "33",
            Reliability = 100,
            MapCode = "Sim1",
            FloorNumber = "1",
            NodeNumber = 33,
            ReportTimes = 63
        },
        new QrCodeDto
        {
            Id = 36,
            CreateTime = "2025-08-25 13:34:26",
            CreateBy = "kuka",
            CreateApp = "QrCodeManager:85",
            LastUpdateTime = "2025-10-20 22:43:55",
            LastUpdateBy = "1003",
            LastUpdateApp = "QrCodeManager:85",
            NodeLabel = "RackPark11",
            Reliability = 100,
            MapCode = "Sim1",
            FloorNumber = "1",
            NodeNumber = 24,
            ReportTimes = 21
        },
        new QrCodeDto
        {
            Id = 35,
            CreateTime = "2025-08-25 13:34:16",
            CreateBy = "kuka",
            CreateApp = "QrCodeManager:85",
            LastUpdateTime = "2025-10-20 22:44:03",
            LastUpdateBy = "1003",
            LastUpdateApp = "QrCodeManager:85",
            NodeLabel = "38",
            Reliability = 100,
            MapCode = "Sim1",
            FloorNumber = "1",
            NodeNumber = 38,
            ReportTimes = 26
        },
        new QrCodeDto
        {
            Id = 28,
            CreateTime = "2025-08-25 13:26:21",
            CreateBy = "kuka",
            CreateApp = "QrCodeManager:85",
            LastUpdateTime = "2025-10-04 19:08:46",
            LastUpdateBy = "10011",
            LastUpdateApp = "QrCodeManager:85",
            NodeLabel = "30",
            Reliability = 100,
            MapCode = "Sim1",
            FloorNumber = "1",
            NodeNumber = 30,
            ReportTimes = 31
        },
        new QrCodeDto
        {
            Id = 42,
            CreateTime = "2025-09-07 12:56:40",
            CreateBy = "kuka",
            CreateApp = "QrCodeManager:85",
            LastUpdateTime = "2025-10-16 00:15:01",
            LastUpdateBy = "1001",
            LastUpdateApp = "QrCodeManager:85",
            NodeLabel = "RackPark16",
            Reliability = 100,
            MapCode = "Sim1",
            FloorNumber = "1",
            NodeNumber = 28,
            ReportTimes = 13
        },
        new QrCodeDto
        {
            Id = 29,
            CreateTime = "2025-08-25 13:26:59",
            CreateBy = "kuka",
            CreateApp = "QrCodeManager:85",
            LastUpdateTime = "2025-10-22 13:54:33",
            LastUpdateBy = "1003",
            LastUpdateApp = "QrCodeManager:85",
            NodeLabel = "18",
            Reliability = 100,
            MapCode = "Sim1",
            FloorNumber = "1",
            NodeNumber = 18,
            ReportTimes = 92
        },
        new QrCodeDto
        {
            Id = 26,
            CreateTime = "2025-08-25 12:17:15",
            CreateBy = "kuka",
            CreateApp = "QrCodeManager:85",
            LastUpdateTime = "2025-10-22 13:42:37",
            LastUpdateBy = "1003",
            LastUpdateApp = "QrCodeManager:85",
            NodeLabel = "29",
            Reliability = 100,
            MapCode = "Sim1",
            FloorNumber = "1",
            NodeNumber = 29,
            ReportTimes = 58
        }

    };

    [HttpPost("list")]
    public ActionResult<ApiResponse<QrCodePage>> GetQrCodeList([FromBody] QueryQrCodesRequest request)
    {
        if (request.PageSize <= 0)
        {
            request.PageSize = 10;
        }

        if (request.PageNum <= 0)
        {
            request.PageNum = 1;
        }

        var totalRecords = QrCodes.Count;
        var totalPages = (int)Math.Ceiling((double)totalRecords / request.PageSize);

        var skip = (request.PageNum - 1) * request.PageSize;
        var pagedQrCodes = QrCodes.Skip(skip).Take(request.PageSize).ToList();

        var response = new ApiResponse<QrCodePage>
        {
            Succ = true,
            Code = 200,
            Msg = string.Empty,
            Data = new QrCodePage
            {
                TotalPages = totalPages,
                TotalRecords = totalRecords,
                Page = request.PageNum,
                Size = request.PageSize,
                Content = pagedQrCodes
            }
        };

        return Ok(response);
    }
}
