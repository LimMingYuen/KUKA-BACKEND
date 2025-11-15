using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QES_KUKA_AMR_API.Migrations
{
    /// <inheritdoc />
    public partial class AddExternalIdsToQrCodeMapZoneMobileRobot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ExternalQrCodeId",
                table: "QrCodes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ExternalMapZoneId",
                table: "MapZones",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ExternalMobileRobotId",
                table: "MobileRobots",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExternalQrCodeId",
                table: "QrCodes");

            migrationBuilder.DropColumn(
                name: "ExternalMapZoneId",
                table: "MapZones");

            migrationBuilder.DropColumn(
                name: "ExternalMobileRobotId",
                table: "MobileRobots");
        }
    }
}
