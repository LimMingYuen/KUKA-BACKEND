using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QES_KUKA_AMR_API_Simulator.Models.Missions;

namespace QES_KUKA_AMR_API_Simulator.Controllers.Data;

[ApiController]
[Authorize]
[Route("api/v1/data/mission")]
public class MissionListController : ControllerBase
{
    // Mirror MobileRobotController by using a deterministic, hand-crafted payload
    private static readonly IReadOnlyList<MissionListItem> Missions =
        new[]
        {
            new MissionListItem
            {
                Id = 1082,
                CreateTime = "2025-10-31 21:23:47",
                CreateBy = "kuka",
                CreateApp = "MissionManager:80",
                LastUpdateTime = "2025-10-31 21:23:57",
                LastUpdateBy = "kuka",
                LastUpdateApp = "MissionManager:98",
                Code = "eacdd49f-e202-485d-af8f-c1937819c913",
                Label = "",
                MissionType = 4,
                RobotId = "1003",
                RobotTypeCode = "KMP 400i diffDrive",
                TemplateCode = "REST",
                TemplateName = "休息",
                MapCode = "Sim1",
                FloorNumber = "1",
                ExtraInfo = "",
                Priority = 99,
                Status = "FINISHED",
                StatusName = "任务已完成",
                StatusInfo = "",
                StatusInfoI18n = null,
                Source = -1,
                BeginTime = "2025-10-31 21:23:47",
                EndTime = "2025-10-31 21:23:57",
                ErrorCode = "NO_ERROR",
                ErrorInfo = "",
                LockStatus = 0,
                ManualStatus = 0,
                ErrorLevel = 0,
                TargetContainerCode = "",
                TargetNodeNumber = 12,
                JobCode = null
            },
            new MissionListItem
            {
                Id = 1077,
                CreateTime = "2025-10-31 21:22:01",
                CreateBy = "kuka",
                CreateApp = "MissionManager:80",
                LastUpdateTime = "2025-10-31 21:23:09",
                LastUpdateBy = "kuka",
                LastUpdateApp = "MissionManager:98",
                Code = "d7936572-d495-44ab-8bd0-a6c236213f8c",
                Label = "",
                MissionType = 2,
                RobotId = "1001",
                RobotTypeCode = "KMP 400i diffDrive",
                TemplateCode = "autoCharging",
                TemplateName = "自动充电",
                MapCode = "Sim1",
                FloorNumber = "1",
                ExtraInfo = "",
                Priority = 4,
                Status = "FINISHED",
                StatusName = "任务已完成",
                StatusInfo = "",
                StatusInfoI18n = null,
                Source = -1,
                BeginTime = "2025-10-31 21:22:01",
                EndTime = "2025-10-31 21:23:09",
                ErrorCode = "NO_ERROR",
                ErrorInfo = "",
                LockStatus = 0,
                ManualStatus = 0,
                ErrorLevel = 0,
                TargetContainerCode = "",
                TargetNodeNumber = 40,
                JobCode = null
            },
            new MissionListItem
            {
                Id = 1074,
                CreateTime = "2025-10-31 20:56:59",
                CreateBy = "kuka",
                CreateApp = "MissionManager:80",
                LastUpdateTime = "2025-10-31 20:57:42",
                LastUpdateBy = "kuka",
                LastUpdateApp = "MissionManager:98",
                Code = "4b047330-ed50-4980-8363-5111b7a0a86a",
                Label = "",
                MissionType = 2,
                RobotId = "1003",
                RobotTypeCode = "KMP 400i diffDrive",
                TemplateCode = "autoCharging",
                TemplateName = "自动充电",
                MapCode = "Sim1",
                FloorNumber = "1",
                ExtraInfo = "",
                Priority = 4,
                Status = "FINISHED",
                StatusName = "任务已完成",
                StatusInfo = "",
                StatusInfoI18n = null,
                Source = -1,
                BeginTime = "2025-10-31 20:56:59",
                EndTime = "2025-10-31 20:57:42",
                ErrorCode = "NO_ERROR",
                ErrorInfo = "",
                LockStatus = 0,
                ManualStatus = 0,
                ErrorLevel = 0,
                TargetContainerCode = "",
                TargetNodeNumber = 40,
                JobCode = null
            },
            new MissionListItem
            {
                Id = 1036,
                CreateTime = "2025-10-31 18:24:21",
                CreateBy = "kuka",
                CreateApp = "MissionManager:80",
                LastUpdateTime = "2025-10-31 18:26:17",
                LastUpdateBy = "kuka",
                LastUpdateApp = "MissionManager:98",
                Code = "8f074695-f96f-4585-a872-614bf81ff07f",
                Label = "",
                MissionType = 2,
                RobotId = "1005",
                RobotTypeCode = "KMP 400i diffDrive",
                TemplateCode = "manualCharging",
                TemplateName = "手动充电",
                MapCode = "Sim1",
                FloorNumber = "1",
                ExtraInfo = "",
                Priority = 0,
                Status = "FINISHED",
                StatusName = "任务已完成",
                StatusInfo = "",
                StatusInfoI18n = null,
                Source = -1,
                BeginTime = "2025-10-31 18:24:21",
                EndTime = "2025-10-31 18:26:17",
                ErrorCode = "NO_ERROR",
                ErrorInfo = "",
                LockStatus = 0,
                ManualStatus = 0,
                ErrorLevel = 0,
                TargetContainerCode = "",
                TargetNodeNumber = 40,
                JobCode = null
            },
            new MissionListItem
            {
                Id = 1030,
                CreateTime = "2025-10-31 18:00:10",
                CreateBy = "kuka",
                CreateApp = "MissionManager:80",
                LastUpdateTime = "2025-10-31 18:00:58",
                LastUpdateBy = "kuka",
                LastUpdateApp = "MissionManager:98",
                Code = "db3667ed-256c-4350-beeb-0c4b5c82369c",
                Label = "",
                MissionType = 2,
                RobotId = "1001",
                RobotTypeCode = "KMP 400i diffDrive",
                TemplateCode = "manualCharging",
                TemplateName = "手动充电",
                MapCode = "Sim1",
                FloorNumber = "1",
                ExtraInfo = "",
                Priority = 0,
                Status = "FINISHED",
                StatusName = "任务已完成",
                StatusInfo = "",
                StatusInfoI18n = null,
                Source = -1,
                BeginTime = "2025-10-31 18:00:10",
                EndTime = "2025-10-31 18:00:58",
                ErrorCode = "NO_ERROR",
                ErrorInfo = "",
                LockStatus = 0,
                ManualStatus = 0,
                ErrorLevel = 0,
                TargetContainerCode = "",
                TargetNodeNumber = 40,
                JobCode = null
            },
            new MissionListItem
            {
                Id = 1028,
                CreateTime = "2025-10-31 17:53:59",
                CreateBy = "kuka",
                CreateApp = "MissionManager:80",
                LastUpdateTime = "2025-10-31 17:54:25",
                LastUpdateBy = "kuka",
                LastUpdateApp = "MissionManager:98",
                Code = "fe8c2fe2-111b-47aa-800f-e5af8051e29f",
                Label = "",
                MissionType = 2,
                RobotId = "1003",
                RobotTypeCode = "KMP 400i diffDrive",
                TemplateCode = "manualCharging",
                TemplateName = "手动充电",
                MapCode = "Sim1",
                FloorNumber = "1",
                ExtraInfo = "",
                Priority = 0,
                Status = "FINISHED",
                StatusName = "任务已完成",
                StatusInfo = "",
                StatusInfoI18n = null,
                Source = -1,
                BeginTime = "2025-10-31 17:53:59",
                EndTime = "2025-10-31 17:54:25",
                ErrorCode = "NO_ERROR",
                ErrorInfo = "",
                LockStatus = 0,
                ManualStatus = 0,
                ErrorLevel = 0,
                TargetContainerCode = "",
                TargetNodeNumber = 40,
                JobCode = null
            },
            new MissionListItem
            {
                Id = 1024,
                CreateTime = "2025-10-31 17:47:25",
                CreateBy = "kuka",
                CreateApp = "MissionManager:80",
                LastUpdateTime = "2025-10-31 17:50:46",
                LastUpdateBy = "kuka",
                LastUpdateApp = "MissionManager:98",
                Code = "c5923267-f2c1-4f47-96ff-3abf9b9fdb0c",
                Label = "",
                MissionType = 2,
                RobotId = "1003",
                RobotTypeCode = "KMP 400i diffDrive",
                TemplateCode = "autoCharging",
                TemplateName = "自动充电",
                MapCode = "Sim1",
                FloorNumber = "1",
                ExtraInfo = "",
                Priority = 4,
                Status = "FINISHED",
                StatusName = "任务已完成",
                StatusInfo = "",
                StatusInfoI18n = null,
                Source = -1,
                BeginTime = "2025-10-31 17:48:21",
                EndTime = "2025-10-31 17:50:45",
                ErrorCode = "NO_ERROR",
                ErrorInfo = "",
                LockStatus = 0,
                ManualStatus = 0,
                ErrorLevel = 0,
                TargetContainerCode = "",
                TargetNodeNumber = 40,
                JobCode = null
            },
            new MissionListItem
            {
                Id = 1023,
                CreateTime = "2025-10-31 17:46:15",
                CreateBy = "kuka",
                CreateApp = "MissionManager:80",
                LastUpdateTime = "2025-10-31 17:48:18",
                LastUpdateBy = "kuka",
                LastUpdateApp = "MissionManager:98",
                Code = "c9c4afd0-bc9e-4965-b001-9f05ffa0ed31",
                Label = "",
                MissionType = 2,
                RobotId = "1001",
                RobotTypeCode = "KMP 400i diffDrive",
                TemplateCode = "manualCharging",
                TemplateName = "手动充电",
                MapCode = "Sim1",
                FloorNumber = "1",
                ExtraInfo = "",
                Priority = 0,
                Status = "FINISHED",
                StatusName = "任务已完成",
                StatusInfo = "",
                StatusInfoI18n = null,
                Source = -1,
                BeginTime = "2025-10-31 17:46:16",
                EndTime = "2025-10-31 17:48:18",
                ErrorCode = "NO_ERROR",
                ErrorInfo = "",
                LockStatus = 0,
                ManualStatus = 0,
                ErrorLevel = 0,
                TargetContainerCode = "",
                TargetNodeNumber = 40,
                JobCode = null
            }
        };

    [HttpPost("list")]
    public ActionResult<MissionListApiResponse> GetMissionList([FromBody] MissionListRequest request)
    {
        if (request.PageSize <= 0)
        {
            request.PageSize = 1000000;
        }

        if (request.PageNum <= 0)
        {
            request.PageNum = 1;
        }

        var filteredMissions = Missions.AsEnumerable();

        // Apply filters
        if (request.Query != null)
        {
            if (!string.IsNullOrEmpty(request.Query.BeginTimeStart))
            {
                filteredMissions = filteredMissions.Where(m => string.Compare(m.BeginTime, request.Query!.BeginTimeStart, StringComparison.Ordinal) >= 0);
            }

            if (!string.IsNullOrEmpty(request.Query.BeginTimeEnd))
            {
                filteredMissions = filteredMissions.Where(m => string.Compare(m.BeginTime, request.Query!.BeginTimeEnd, StringComparison.Ordinal) <= 0);
            }

            if (!string.IsNullOrEmpty(request.Query.RobotId))
            {
                filteredMissions = filteredMissions.Where(m => m.RobotId.Contains(request.Query!.RobotId, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(request.Query.RobotTypeCode))
            {
                filteredMissions = filteredMissions.Where(m => m.RobotTypeCode.Contains(request.Query!.RobotTypeCode, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(request.Query.TemplateCode))
            {
                filteredMissions = filteredMissions.Where(m => m.TemplateCode.Contains(request.Query!.TemplateCode, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(request.Query.TemplateName))
            {
                filteredMissions = filteredMissions.Where(m => m.TemplateName.Contains(request.Query!.TemplateName, StringComparison.OrdinalIgnoreCase));
            }
        }

        var missionList = filteredMissions.ToList();
        var totalRecords = missionList.Count;
        var totalPages = (int)Math.Ceiling((double)totalRecords / request.PageSize);

        var skip = (request.PageNum - 1) * request.PageSize;
        var pagedMissions = missionList.Skip(skip).Take(request.PageSize).ToList();

        var response = new MissionListApiResponse
        {
            Success = true,
            Code = null,
            Message = null,
            Data = new MissionListData
            {
                TotalPages = totalPages,
                TotalRecords = totalRecords,
                Page = request.PageNum,
                Size = request.PageSize,
                Content = pagedMissions
            }
        };

        return Ok(response);
    }
}

