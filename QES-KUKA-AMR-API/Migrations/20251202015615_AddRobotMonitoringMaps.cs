using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QES_KUKA_AMR_API.Migrations
{
    /// <inheritdoc />
    public partial class AddRobotMonitoringMaps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RobotMonitoringMaps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    MapCode = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    FloorNumber = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    BackgroundImagePath = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    BackgroundImageOriginalName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ImageWidth = table.Column<int>(type: "int", nullable: true),
                    ImageHeight = table.Column<int>(type: "int", nullable: true),
                    CoordinateSettingsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DisplaySettingsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RefreshIntervalMs = table.Column<int>(type: "int", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LastUpdatedUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RobotMonitoringMaps", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RobotMonitoringMaps_CreatedBy",
                table: "RobotMonitoringMaps",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_RobotMonitoringMaps_IsDefault",
                table: "RobotMonitoringMaps",
                column: "IsDefault");

            migrationBuilder.CreateIndex(
                name: "IX_RobotMonitoringMaps_MapCode",
                table: "RobotMonitoringMaps",
                column: "MapCode");

            migrationBuilder.CreateIndex(
                name: "IX_RobotMonitoringMaps_Name",
                table: "RobotMonitoringMaps",
                column: "Name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RobotMonitoringMaps");
        }
    }
}
