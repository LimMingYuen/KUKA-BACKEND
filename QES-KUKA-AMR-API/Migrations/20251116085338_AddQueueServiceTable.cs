using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QES_KUKA_AMR_API.Migrations
{
    /// <inheritdoc />
    public partial class AddQueueServiceTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MapCodeQueueConfigurations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MapCode = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    EnableQueue = table.Column<bool>(type: "bit", nullable: false),
                    DefaultPriority = table.Column<int>(type: "int", nullable: false),
                    MaxConsecutiveOpportunisticJobs = table.Column<int>(type: "int", nullable: false),
                    EnableCrossMapOptimization = table.Column<bool>(type: "bit", nullable: false),
                    MaxConcurrentRobotsOnMap = table.Column<int>(type: "int", nullable: false),
                    QueueProcessingIntervalSeconds = table.Column<int>(type: "int", nullable: false),
                    TotalJobsProcessed = table.Column<int>(type: "int", nullable: false),
                    OpportunisticJobsChained = table.Column<int>(type: "int", nullable: false),
                    AverageJobWaitTimeSeconds = table.Column<double>(type: "float", nullable: false),
                    AverageOpportunisticJobDistanceMeters = table.Column<double>(type: "float", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MapCodeQueueConfigurations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MissionQueueItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QueueItemCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MissionCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RequestId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    PrimaryMapCode = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    SecondaryMapCode = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    IsCrossMapMission = table.Column<bool>(type: "bit", nullable: false),
                    WorkflowId = table.Column<int>(type: "int", nullable: true),
                    SavedMissionId = table.Column<int>(type: "int", nullable: true),
                    TriggerSource = table.Column<int>(type: "int", nullable: false),
                    MissionStepsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RobotModelsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RobotIdsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    EnqueuedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelledUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AssignedRobotId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RobotAssignedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ParentQueueItemId = table.Column<int>(type: "int", nullable: true),
                    NextQueueItemId = table.Column<int>(type: "int", nullable: true),
                    IsOpportunisticJob = table.Column<bool>(type: "bit", nullable: false),
                    StartNodeLabel = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    StartXCoordinate = table.Column<double>(type: "float", nullable: true),
                    StartYCoordinate = table.Column<double>(type: "float", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    LastRetryUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
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
                    RobotId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CurrentMapCode = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CurrentXCoordinate = table.Column<double>(type: "float", nullable: false),
                    CurrentYCoordinate = table.Column<double>(type: "float", nullable: false),
                    PositionUpdatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CurrentQueueItemId = table.Column<int>(type: "int", nullable: false),
                    MissionCompletedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ConsecutiveJobsInMapCode = table.Column<int>(type: "int", nullable: false),
                    OriginalMapCode = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    EnteredMapCodeUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OpportunityCheckPending = table.Column<bool>(type: "bit", nullable: false),
                    OpportunityEvaluatedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SelectedOpportunisticJobId = table.Column<int>(type: "int", nullable: true),
                    Decision = table.Column<int>(type: "int", nullable: false),
                    DecisionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RobotJobOpportunities", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MapCodeQueueConfigurations_MapCode",
                table: "MapCodeQueueConfigurations",
                column: "MapCode",
                unique: true);

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MapCodeQueueConfigurations");

            migrationBuilder.DropTable(
                name: "MissionQueueItems");

            migrationBuilder.DropTable(
                name: "RobotJobOpportunities");
        }
    }
}
