using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QES_KUKA_AMR_API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTrackingFieldsToMissionHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AssignedRobotId",
                table: "MissionHistories",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ProcessedDate",
                table: "MissionHistories",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SavedMissionId",
                table: "MissionHistories",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SubmittedToAmrDate",
                table: "MissionHistories",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TriggerSource",
                table: "MissionHistories",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_MissionHistories_AssignedRobotId_CompletedDate",
                table: "MissionHistories",
                columns: new[] { "AssignedRobotId", "CompletedDate" });

            migrationBuilder.CreateIndex(
                name: "IX_MissionHistories_SavedMissionId",
                table: "MissionHistories",
                column: "SavedMissionId");

            migrationBuilder.CreateIndex(
                name: "IX_MissionHistories_TriggerSource_CompletedDate",
                table: "MissionHistories",
                columns: new[] { "TriggerSource", "CompletedDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MissionHistories_AssignedRobotId_CompletedDate",
                table: "MissionHistories");

            migrationBuilder.DropIndex(
                name: "IX_MissionHistories_SavedMissionId",
                table: "MissionHistories");

            migrationBuilder.DropIndex(
                name: "IX_MissionHistories_TriggerSource_CompletedDate",
                table: "MissionHistories");

            migrationBuilder.DropColumn(
                name: "AssignedRobotId",
                table: "MissionHistories");

            migrationBuilder.DropColumn(
                name: "ProcessedDate",
                table: "MissionHistories");

            migrationBuilder.DropColumn(
                name: "SavedMissionId",
                table: "MissionHistories");

            migrationBuilder.DropColumn(
                name: "SubmittedToAmrDate",
                table: "MissionHistories");

            migrationBuilder.DropColumn(
                name: "TriggerSource",
                table: "MissionHistories");
        }
    }
}
