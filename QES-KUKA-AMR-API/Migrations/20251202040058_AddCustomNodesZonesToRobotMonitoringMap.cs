using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QES_KUKA_AMR_API.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomNodesZonesToRobotMonitoringMap : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CustomNodesJson",
                table: "RobotMonitoringMaps",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomZonesJson",
                table: "RobotMonitoringMaps",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomNodesJson",
                table: "RobotMonitoringMaps");

            migrationBuilder.DropColumn(
                name: "CustomZonesJson",
                table: "RobotMonitoringMaps");
        }
    }
}
