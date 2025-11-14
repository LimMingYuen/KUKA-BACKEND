using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QES_KUKA_AMR_API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRobotTrackingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AssignedRobotId",
                table: "MissionQueues",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastRobotQueryTime",
                table: "MissionQueues",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RobotBatteryLevel",
                table: "MissionQueues",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RobotNodeCode",
                table: "MissionQueues",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RobotStatusCode",
                table: "MissionQueues",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AssignedRobotId",
                table: "MissionQueues");

            migrationBuilder.DropColumn(
                name: "LastRobotQueryTime",
                table: "MissionQueues");

            migrationBuilder.DropColumn(
                name: "RobotBatteryLevel",
                table: "MissionQueues");

            migrationBuilder.DropColumn(
                name: "RobotNodeCode",
                table: "MissionQueues");

            migrationBuilder.DropColumn(
                name: "RobotStatusCode",
                table: "MissionQueues");
        }
    }
}
