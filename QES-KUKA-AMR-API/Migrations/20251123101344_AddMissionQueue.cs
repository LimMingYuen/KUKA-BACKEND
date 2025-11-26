using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QES_KUKA_AMR_API.Migrations
{
    /// <inheritdoc />
    public partial class AddMissionQueue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MissionQueues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MissionCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RequestId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SavedMissionId = table.Column<int>(type: "int", nullable: true),
                    MissionName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MissionRequestJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    QueuePosition = table.Column<int>(type: "int", nullable: false),
                    AssignedRobotId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProcessingStartedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AssignedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    MaxRetries = table.Column<int>(type: "int", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RobotTypeFilter = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PreferredRobotIds = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MissionQueues", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MissionQueues_AssignedRobotId",
                table: "MissionQueues",
                column: "AssignedRobotId");

            migrationBuilder.CreateIndex(
                name: "IX_MissionQueues_MissionCode",
                table: "MissionQueues",
                column: "MissionCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MissionQueues_SavedMissionId",
                table: "MissionQueues",
                column: "SavedMissionId");

            migrationBuilder.CreateIndex(
                name: "IX_MissionQueues_Status_Priority_CreatedUtc",
                table: "MissionQueues",
                columns: new[] { "Status", "Priority", "CreatedUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MissionQueues");
        }
    }
}
