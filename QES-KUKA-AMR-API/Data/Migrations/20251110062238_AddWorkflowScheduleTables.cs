using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QES_KUKA_AMR_API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkflowScheduleTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WorkflowSchedules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkflowId = table.Column<int>(type: "int", nullable: false),
                    TriggerType = table.Column<int>(type: "int", nullable: false),
                    CronExpression = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    OneTimeRunUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TimezoneId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_WorkflowSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowSchedules_WorkflowDiagrams_WorkflowId",
                        column: x => x.WorkflowId,
                        principalTable: "WorkflowDiagrams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowScheduleLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ScheduleId = table.Column<int>(type: "int", nullable: false),
                    WorkflowId = table.Column<int>(type: "int", nullable: false),
                    ScheduledForUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EnqueuedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    QueueId = table.Column<int>(type: "int", nullable: true),
                    ResultStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Error = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowScheduleLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowScheduleLogs_WorkflowSchedules_ScheduleId",
                        column: x => x.ScheduleId,
                        principalTable: "WorkflowSchedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowScheduleLogs_ScheduleId",
                table: "WorkflowScheduleLogs",
                column: "ScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowScheduleLogs_ScheduleId_CreatedUtc",
                table: "WorkflowScheduleLogs",
                columns: new[] { "ScheduleId", "CreatedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowScheduleLogs_WorkflowId_CreatedUtc",
                table: "WorkflowScheduleLogs",
                columns: new[] { "WorkflowId", "CreatedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowSchedules_NextRunUtc",
                table: "WorkflowSchedules",
                column: "NextRunUtc");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowSchedules_WorkflowId_IsEnabled",
                table: "WorkflowSchedules",
                columns: new[] { "WorkflowId", "IsEnabled" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkflowScheduleLogs");

            migrationBuilder.DropTable(
                name: "WorkflowSchedules");
        }
    }
}
