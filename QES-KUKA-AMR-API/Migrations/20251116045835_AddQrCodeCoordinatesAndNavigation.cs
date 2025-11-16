using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QES_KUKA_AMR_API.Migrations
{
    /// <inheritdoc />
    public partial class AddQrCodeCoordinatesAndNavigation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AngularAccuracy",
                table: "QrCodes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "DistanceAccuracy",
                table: "QrCodes",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FunctionListJson",
                table: "QrCodes",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GoalAngularAccuracy",
                table: "QrCodes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "GoalDistanceAccuracy",
                table: "QrCodes",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NodeType",
                table: "QrCodes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NodeUuid",
                table: "QrCodes",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SpecialConfig",
                table: "QrCodes",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TransitOrientations",
                table: "QrCodes",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "XCoordinate",
                table: "QrCodes",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "YCoordinate",
                table: "QrCodes",
                type: "float",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AngularAccuracy",
                table: "QrCodes");

            migrationBuilder.DropColumn(
                name: "DistanceAccuracy",
                table: "QrCodes");

            migrationBuilder.DropColumn(
                name: "FunctionListJson",
                table: "QrCodes");

            migrationBuilder.DropColumn(
                name: "GoalAngularAccuracy",
                table: "QrCodes");

            migrationBuilder.DropColumn(
                name: "GoalDistanceAccuracy",
                table: "QrCodes");

            migrationBuilder.DropColumn(
                name: "NodeType",
                table: "QrCodes");

            migrationBuilder.DropColumn(
                name: "NodeUuid",
                table: "QrCodes");

            migrationBuilder.DropColumn(
                name: "SpecialConfig",
                table: "QrCodes");

            migrationBuilder.DropColumn(
                name: "TransitOrientations",
                table: "QrCodes");

            migrationBuilder.DropColumn(
                name: "XCoordinate",
                table: "QrCodes");

            migrationBuilder.DropColumn(
                name: "YCoordinate",
                table: "QrCodes");
        }
    }
}
