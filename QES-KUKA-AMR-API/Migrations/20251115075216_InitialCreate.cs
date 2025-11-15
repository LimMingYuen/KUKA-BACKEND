using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QES_KUKA_AMR_API.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Areas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DisplayName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ActualValue = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Areas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MapZones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreateBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CreateApp = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUpdateBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    LastUpdateApp = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ZoneName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ZoneCode = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ZoneDescription = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ZoneColor = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    MapCode = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    FloorNumber = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Points = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Nodes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Edges = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CustomerUi = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ZoneType = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    BeginTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Configs = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MapZones", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MissionHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MissionCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RequestId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    WorkflowId = table.Column<int>(type: "int", nullable: true),
                    WorkflowName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SavedMissionId = table.Column<int>(type: "int", nullable: true),
                    TriggerSource = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MissionType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProcessedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SubmittedToAmrDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AssignedRobotId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MissionHistories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MissionTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DisplayName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ActualValue = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MissionTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MobileRobots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreateBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreateApp = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUpdateBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastUpdateApp = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RobotId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RobotTypeCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    BuildingCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MapCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FloorNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LastNodeNumber = table.Column<int>(type: "int", nullable: false),
                    LastNodeDeleteFlag = table.Column<bool>(type: "bit", nullable: false),
                    ContainerCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ActuatorType = table.Column<int>(type: "int", nullable: false),
                    ActuatorStatusInfo = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    WarningInfo = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ConfigVersion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SendConfigVersion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SendConfigTime = table.Column<DateTime>(type: "datetime2", maxLength: 100, nullable: false),
                    FirmwareVersion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SendFirmwareVersion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SendFirmwareTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    OccupyStatus = table.Column<int>(type: "int", nullable: false),
                    BatteryLevel = table.Column<double>(type: "float", nullable: false),
                    Mileage = table.Column<double>(type: "float", nullable: false),
                    MissionCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MeetObstacleStatus = table.Column<int>(type: "int", nullable: false),
                    RobotOrientation = table.Column<double>(type: "float", nullable: true),
                    Reliability = table.Column<int>(type: "int", nullable: false),
                    RunTime = table.Column<int>(type: "int", nullable: true),
                    RobotTypeClass = table.Column<int>(type: "int", nullable: true),
                    TrailerNum = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TractionStatus = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    XCoordinate = table.Column<double>(type: "float", nullable: true),
                    YCoordinate = table.Column<double>(type: "float", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MobileRobots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "QrCodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreateBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CreateApp = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUpdateBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    LastUpdateApp = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    NodeLabel = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Reliability = table.Column<int>(type: "int", nullable: false),
                    MapCode = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    FloorNumber = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    NodeNumber = table.Column<int>(type: "int", nullable: false),
                    ReportTimes = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QrCodes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ResumeStrategies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DisplayName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ActualValue = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResumeStrategies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RobotManualPauses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RobotId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MissionCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    WaypointCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PauseStartUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PauseEndUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RobotManualPauses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RobotTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DisplayName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ActualValue = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RobotTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SavedCustomMissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MissionName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MissionType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RobotType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    RobotModels = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RobotIds = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ContainerModelCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ContainerCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IdleNode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    MissionStepsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedCustomMissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ShelfDecisionRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DisplayName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ActualValue = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShelfDecisionRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemSetting",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemSetting", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowDiagrams",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkflowCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    WorkflowOuterCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    WorkflowName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    WorkflowModel = table.Column<int>(type: "int", nullable: false),
                    RobotTypeClass = table.Column<int>(type: "int", nullable: false),
                    MapCode = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ButtonName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    CreateUsername = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CreateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdateUsername = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    UpdateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    NeedConfirm = table.Column<int>(type: "int", nullable: false),
                    LockRobotAfterFinish = table.Column<int>(type: "int", nullable: false),
                    WorkflowPriority = table.Column<int>(type: "int", nullable: false),
                    TargetAreaCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    PreSelectedRobotCellCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    PreSelectedRobotId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowDiagrams", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Areas_ActualValue",
                table: "Areas",
                column: "ActualValue",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MapZones_MapCode",
                table: "MapZones",
                column: "MapCode");

            migrationBuilder.CreateIndex(
                name: "IX_MapZones_ZoneCode",
                table: "MapZones",
                column: "ZoneCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MissionHistories_AssignedRobotId_CompletedDate",
                table: "MissionHistories",
                columns: new[] { "AssignedRobotId", "CompletedDate" });

            migrationBuilder.CreateIndex(
                name: "IX_MissionHistories_MissionCode",
                table: "MissionHistories",
                column: "MissionCode");

            migrationBuilder.CreateIndex(
                name: "IX_MissionHistories_SavedMissionId",
                table: "MissionHistories",
                column: "SavedMissionId");

            migrationBuilder.CreateIndex(
                name: "IX_MissionHistories_TriggerSource_CompletedDate",
                table: "MissionHistories",
                columns: new[] { "TriggerSource", "CompletedDate" });

            migrationBuilder.CreateIndex(
                name: "IX_MissionTypes_ActualValue",
                table: "MissionTypes",
                column: "ActualValue",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QrCodes_MapCode",
                table: "QrCodes",
                column: "MapCode");

            migrationBuilder.CreateIndex(
                name: "IX_QrCodes_NodeLabel_MapCode",
                table: "QrCodes",
                columns: new[] { "NodeLabel", "MapCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResumeStrategies_ActualValue",
                table: "ResumeStrategies",
                column: "ActualValue",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RobotManualPauses_MissionCode_PauseStartUtc",
                table: "RobotManualPauses",
                columns: new[] { "MissionCode", "PauseStartUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_RobotManualPauses_RobotId_PauseStartUtc",
                table: "RobotManualPauses",
                columns: new[] { "RobotId", "PauseStartUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_RobotTypes_ActualValue",
                table: "RobotTypes",
                column: "ActualValue",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SavedCustomMissions_CreatedBy_IsDeleted",
                table: "SavedCustomMissions",
                columns: new[] { "CreatedBy", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_SavedCustomMissions_MissionName",
                table: "SavedCustomMissions",
                column: "MissionName");

            migrationBuilder.CreateIndex(
                name: "IX_ShelfDecisionRules_ActualValue",
                table: "ShelfDecisionRules",
                column: "ActualValue",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SystemSetting_Key",
                table: "SystemSetting",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowDiagrams_WorkflowCode",
                table: "WorkflowDiagrams",
                column: "WorkflowCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Areas");

            migrationBuilder.DropTable(
                name: "MapZones");

            migrationBuilder.DropTable(
                name: "MissionHistories");

            migrationBuilder.DropTable(
                name: "MissionTypes");

            migrationBuilder.DropTable(
                name: "MobileRobots");

            migrationBuilder.DropTable(
                name: "QrCodes");

            migrationBuilder.DropTable(
                name: "ResumeStrategies");

            migrationBuilder.DropTable(
                name: "RobotManualPauses");

            migrationBuilder.DropTable(
                name: "RobotTypes");

            migrationBuilder.DropTable(
                name: "SavedCustomMissions");

            migrationBuilder.DropTable(
                name: "ShelfDecisionRules");

            migrationBuilder.DropTable(
                name: "SystemSetting");

            migrationBuilder.DropTable(
                name: "WorkflowDiagrams");
        }
    }
}
