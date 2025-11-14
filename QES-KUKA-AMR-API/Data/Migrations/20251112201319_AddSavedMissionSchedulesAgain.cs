using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QES_KUKA_AMR_API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSavedMissionSchedulesAgain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserRoles");

            // migrationBuilder.RenameColumn(
            //     name: "PermissionId",
            //     table: "RolePermissions",
            //     newName: "PageId");

            // SavedMissionId column already exists from migration 20251105014923_AddSavedMissionIdToScheduleLog
            // migrationBuilder.AddColumn<int>(
            //     name: "SavedMissionId",
            //     table: "SavedMissionScheduleLogs",
            //     type: "int",
            //     nullable: false,
            //     defaultValue: 0);

            // SystemSettings table - keeping for potential future use if needed
            migrationBuilder.CreateTable(
                name: "SystemSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Key = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemSettings", x => x.Id);
                });

            // WorkflowSchedules and WorkflowScheduleLogs tables already exist from migration 20251110062238_AddWorkflowScheduleTables
            /*
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
            */

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);

            // Index already exists from migration 20251105014923_AddSavedMissionIdToScheduleLog
            // migrationBuilder.CreateIndex(
            //     name: "IX_SavedMissionScheduleLogs_SavedMissionId_CreatedUtc",
            //     table: "SavedMissionScheduleLogs",
            //     columns: new[] { "SavedMissionId", "CreatedUtc" });

            // Index already exists from migration 20251105012747_AddSavedMissionScheduleLogIndexes
            // migrationBuilder.CreateIndex(
            //     name: "IX_SavedMissionScheduleLogs_ScheduleId_CreatedUtc",
            //     table: "SavedMissionScheduleLogs",
            //     columns: new[] { "ScheduleId", "CreatedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Roles_RoleCode",
                table: "Roles",
                column: "RoleCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_RoleId_PageId",
                table: "RolePermissions",
                columns: new[] { "RoleId", "PageId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Page_PagePath",
                table: "Page",
                column: "PagePath",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SystemSettings_Key",
                table: "SystemSettings",
                column: "Key",
                unique: true);

            // Indexes for WorkflowSchedules and WorkflowScheduleLogs already exist from migration 20251110062238_AddWorkflowScheduleTables
            /*
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
            */
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SystemSettings");

            // WorkflowSchedules and WorkflowScheduleLogs managed by migration 20251110062238_AddWorkflowScheduleTables
            /*
            migrationBuilder.DropTable(
                name: "WorkflowScheduleLogs");

            migrationBuilder.DropTable(
                name: "WorkflowSchedules");
            */

            migrationBuilder.DropIndex(
                name: "IX_Users_Username",
                table: "Users");

            // Index managed by migration 20251105014923_AddSavedMissionIdToScheduleLog
            // migrationBuilder.DropIndex(
            //     name: "IX_SavedMissionScheduleLogs_SavedMissionId_CreatedUtc",
            //     table: "SavedMissionScheduleLogs");

            // Index managed by migration 20251105012747_AddSavedMissionScheduleLogIndexes
            // migrationBuilder.DropIndex(
            //     name: "IX_SavedMissionScheduleLogs_ScheduleId_CreatedUtc",
            //     table: "SavedMissionScheduleLogs");

            migrationBuilder.DropIndex(
                name: "IX_Roles_RoleCode",
                table: "Roles");

            migrationBuilder.DropIndex(
                name: "IX_RolePermissions_RoleId_PageId",
                table: "RolePermissions");

            migrationBuilder.DropIndex(
                name: "IX_Page_PagePath",
                table: "Page");

            // Column managed by migration 20251105014923_AddSavedMissionIdToScheduleLog
            // migrationBuilder.DropColumn(
            //     name: "SavedMissionId",
            //     table: "SavedMissionScheduleLogs");

            migrationBuilder.RenameColumn(
                name: "PageId",
                table: "RolePermissions",
                newName: "PermissionId");

            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    RolesId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => new { x.RolesId, x.UserId });
                    table.ForeignKey(
                        name: "FK_UserRoles_Roles_RolesId",
                        column: x => x.RolesId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_UserId",
                table: "UserRoles",
                column: "UserId");
        }
    }
}
