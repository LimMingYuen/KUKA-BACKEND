using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QES_KUKA_AMR_API.Migrations
{
    /// <inheritdoc />
    public partial class AddMissionQueueJobOptimization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReservedByMissionCode",
                table: "MissionQueues",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReservedForRobotId",
                table: "MissionQueues",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReservedUtc",
                table: "MissionQueues",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReservedByMissionCode",
                table: "MissionQueues");

            migrationBuilder.DropColumn(
                name: "ReservedForRobotId",
                table: "MissionQueues");

            migrationBuilder.DropColumn(
                name: "ReservedUtc",
                table: "MissionQueues");
        }
    }
}
