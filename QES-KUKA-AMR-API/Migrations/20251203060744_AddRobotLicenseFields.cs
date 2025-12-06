using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QES_KUKA_AMR_API.Migrations
{
    /// <inheritdoc />
    public partial class AddRobotLicenseFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsLicensed",
                table: "MobileRobots",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LicenseError",
                table: "MobileRobots",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MobileRobots_RobotId",
                table: "MobileRobots",
                column: "RobotId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MobileRobots_RobotId",
                table: "MobileRobots");

            migrationBuilder.DropColumn(
                name: "IsLicensed",
                table: "MobileRobots");

            migrationBuilder.DropColumn(
                name: "LicenseError",
                table: "MobileRobots");
        }
    }
}
