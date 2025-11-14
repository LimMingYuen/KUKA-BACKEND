using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QES_KUKA_AMR_API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSavedMissionSchedules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SavedMissionSchedules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SavedMissionId = table.Column<int>(type: "int", nullable: false),
                    TriggerType = table.Column<int>(type: "int", nullable: false),
                    CronExpression = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    OneTimeRunUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TimezoneId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "UTC"),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastRunUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    LastError = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    NextRunUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    QueueLockToken = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedMissionSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SavedMissionSchedules_SavedCustomMissions_SavedMissionId",
                        column: x => x.SavedMissionId,
                        principalTable: "SavedCustomMissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SavedMissionScheduleLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ScheduleId = table.Column<int>(type: "int", nullable: false),
                    ScheduledForUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EnqueuedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    QueueId = table.Column<int>(type: "int", nullable: true),
                    ResultStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "Pending"),
                    Error = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedMissionScheduleLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SavedMissionScheduleLogs_SavedMissionSchedules_ScheduleId",
                        column: x => x.ScheduleId,
                        principalTable: "SavedMissionSchedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SavedMissionScheduleLogs_ScheduleId",
                table: "SavedMissionScheduleLogs",
                column: "ScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_SavedMissionSchedules_NextRunUtc",
                table: "SavedMissionSchedules",
                column: "NextRunUtc");

            migrationBuilder.CreateIndex(
                name: "IX_SavedMissionSchedules_SavedMissionId_IsEnabled",
                table: "SavedMissionSchedules",
                columns: new[] { "SavedMissionId", "IsEnabled" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SavedMissionScheduleLogs");

            migrationBuilder.DropTable(
                name: "SavedMissionSchedules");
        }
    }
}
