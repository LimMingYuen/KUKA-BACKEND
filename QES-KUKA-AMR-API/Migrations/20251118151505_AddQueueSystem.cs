using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QES_KUKA_AMR_API.Migrations
{
    /// <inheritdoc />
    public partial class AddQueueSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MissionQueueItems");

            migrationBuilder.DropTable(
                name: "RobotJobOpportunities");

            migrationBuilder.DropColumn(
                name: "AverageJobWaitTimeSeconds",
                table: "MapCodeQueueConfigurations");

            migrationBuilder.DropColumn(
                name: "AverageOpportunisticJobDistanceMeters",
                table: "MapCodeQueueConfigurations");

            migrationBuilder.DropColumn(
                name: "EnableCrossMapOptimization",
                table: "MapCodeQueueConfigurations");

            migrationBuilder.DropColumn(
                name: "MaxConcurrentRobotsOnMap",
                table: "MapCodeQueueConfigurations");

            migrationBuilder.DropColumn(
                name: "MaxConsecutiveOpportunisticJobs",
                table: "MapCodeQueueConfigurations");

            migrationBuilder.DropColumn(
                name: "OpportunisticJobsChained",
                table: "MapCodeQueueConfigurations");

            migrationBuilder.RenameColumn(
                name: "TotalJobsProcessed",
                table: "MapCodeQueueConfigurations",
                newName: "MaxQueueDepth");

            migrationBuilder.RenameColumn(
                name: "QueueProcessingIntervalSeconds",
                table: "MapCodeQueueConfigurations",
                newName: "MaxConcurrentTasks");

            migrationBuilder.RenameColumn(
                name: "EnableQueue",
                table: "MapCodeQueueConfigurations",
                newName: "IsEnabled");

            migrationBuilder.RenameIndex(
                name: "IX_MapCodeQueueConfigurations_MapCode",
                table: "MapCodeQueueConfigurations",
                newName: "IX_MapCodeQueueConfig_MapCode");

            migrationBuilder.AddColumn<string>(
                name: "DisplayName",
                table: "MapCodeQueueConfigurations",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "QueueItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MapCode = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    QueuePosition = table.Column<int>(type: "int", nullable: false),
                    SavedMissionId = table.Column<int>(type: "int", nullable: true),
                    MissionCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RequestId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MissionDataJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MissionType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RobotIds = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AssignedRobotId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RobotType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TargetNodeCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TargetX = table.Column<double>(type: "float", nullable: true),
                    TargetY = table.Column<double>(type: "float", nullable: true),
                    RequiresOptimization = table.Column<bool>(type: "bit", nullable: false),
                    OrgId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProcessingStartedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SubmittedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QueueItems", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QueueItem_MapProcessing",
                table: "QueueItems",
                columns: new[] { "MapCode", "Status", "Priority", "QueuePosition" });

            migrationBuilder.CreateIndex(
                name: "IX_QueueItem_MapStatus",
                table: "QueueItems",
                columns: new[] { "MapCode", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_QueueItem_MissionCode",
                table: "QueueItems",
                column: "MissionCode");

            migrationBuilder.CreateIndex(
                name: "IX_QueueItem_RobotStatus",
                table: "QueueItems",
                columns: new[] { "AssignedRobotId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QueueItems");

            migrationBuilder.DropColumn(
                name: "DisplayName",
                table: "MapCodeQueueConfigurations");

            migrationBuilder.RenameColumn(
                name: "MaxQueueDepth",
                table: "MapCodeQueueConfigurations",
                newName: "TotalJobsProcessed");

            migrationBuilder.RenameColumn(
                name: "MaxConcurrentTasks",
                table: "MapCodeQueueConfigurations",
                newName: "QueueProcessingIntervalSeconds");

            migrationBuilder.RenameColumn(
                name: "IsEnabled",
                table: "MapCodeQueueConfigurations",
                newName: "EnableQueue");

            migrationBuilder.RenameIndex(
                name: "IX_MapCodeQueueConfig_MapCode",
                table: "MapCodeQueueConfigurations",
                newName: "IX_MapCodeQueueConfigurations_MapCode");

            migrationBuilder.AddColumn<double>(
                name: "AverageJobWaitTimeSeconds",
                table: "MapCodeQueueConfigurations",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "AverageOpportunisticJobDistanceMeters",
                table: "MapCodeQueueConfigurations",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<bool>(
                name: "EnableCrossMapOptimization",
                table: "MapCodeQueueConfigurations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "MaxConcurrentRobotsOnMap",
                table: "MapCodeQueueConfigurations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MaxConsecutiveOpportunisticJobs",
                table: "MapCodeQueueConfigurations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "OpportunisticJobsChained",
                table: "MapCodeQueueConfigurations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "MissionQueueItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssignedRobotId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CancelledUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ContainerCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ContainerModelCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EnqueuedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IdleNode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsCrossMapMission = table.Column<bool>(type: "bit", nullable: false),
                    IsOpportunisticJob = table.Column<bool>(type: "bit", nullable: false),
                    LastRetryUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LockRobotAfterFinish = table.Column<bool>(type: "bit", nullable: false),
                    MissionCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MissionStepsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MissionType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    NextQueueItemId = table.Column<int>(type: "int", nullable: true),
                    OrgId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ParentQueueItemId = table.Column<int>(type: "int", nullable: true),
                    PrimaryMapCode = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    QueueItemCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RequestId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    RobotAssignedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RobotIds = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RobotModels = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RobotType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SavedMissionId = table.Column<int>(type: "int", nullable: true),
                    SecondaryMapCode = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    StartNodeLabel = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    StartXCoordinate = table.Column<double>(type: "float", nullable: true),
                    StartYCoordinate = table.Column<double>(type: "float", nullable: true),
                    StartedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    TemplateCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TriggerSource = table.Column<int>(type: "int", nullable: false),
                    UnlockMissionCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UnlockRobotId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ViewBoardType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    WorkflowId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MissionQueueItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RobotJobOpportunities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConsecutiveJobsInMapCode = table.Column<int>(type: "int", nullable: false),
                    CurrentMapCode = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CurrentQueueItemId = table.Column<int>(type: "int", nullable: false),
                    CurrentXCoordinate = table.Column<double>(type: "float", nullable: false),
                    CurrentYCoordinate = table.Column<double>(type: "float", nullable: false),
                    Decision = table.Column<int>(type: "int", nullable: false),
                    DecisionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EnteredMapCodeUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MissionCompletedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OpportunityCheckPending = table.Column<bool>(type: "bit", nullable: false),
                    OpportunityEvaluatedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OriginalMapCode = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    PositionUpdatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RobotId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SelectedOpportunisticJobId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RobotJobOpportunities", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MissionQueueItems_AssignedRobotId_Status",
                table: "MissionQueueItems",
                columns: new[] { "AssignedRobotId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_MissionQueueItems_MissionCode",
                table: "MissionQueueItems",
                column: "MissionCode");

            migrationBuilder.CreateIndex(
                name: "IX_MissionQueueItems_NextQueueItemId",
                table: "MissionQueueItems",
                column: "NextQueueItemId");

            migrationBuilder.CreateIndex(
                name: "IX_MissionQueueItems_ParentQueueItemId",
                table: "MissionQueueItems",
                column: "ParentQueueItemId");

            migrationBuilder.CreateIndex(
                name: "IX_MissionQueueItems_PrimaryMapCode_Status_Priority",
                table: "MissionQueueItems",
                columns: new[] { "PrimaryMapCode", "Status", "Priority" });

            migrationBuilder.CreateIndex(
                name: "IX_MissionQueueItems_QueueItemCode",
                table: "MissionQueueItems",
                column: "QueueItemCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RobotJobOpportunities_CurrentQueueItemId",
                table: "RobotJobOpportunities",
                column: "CurrentQueueItemId");

            migrationBuilder.CreateIndex(
                name: "IX_RobotJobOpportunities_RobotId_CurrentMapCode",
                table: "RobotJobOpportunities",
                columns: new[] { "RobotId", "CurrentMapCode" });

            migrationBuilder.CreateIndex(
                name: "IX_RobotJobOpportunities_SelectedOpportunisticJobId",
                table: "RobotJobOpportunities",
                column: "SelectedOpportunisticJobId");
        }
    }
}
