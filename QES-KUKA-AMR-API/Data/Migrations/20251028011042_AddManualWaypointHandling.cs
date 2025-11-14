using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QES_KUKA_AMR_API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddManualWaypointHandling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CurrentManualWaypointPosition",
                table: "MissionQueues",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsWaitingForManualResume",
                table: "MissionQueues",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ManualWaypointsJson",
                table: "MissionQueues",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentManualWaypointPosition",
                table: "MissionQueues");

            migrationBuilder.DropColumn(
                name: "IsWaitingForManualResume",
                table: "MissionQueues");

            migrationBuilder.DropColumn(
                name: "ManualWaypointsJson",
                table: "MissionQueues");
        }
    }
}
