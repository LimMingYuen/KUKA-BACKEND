using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QES_KUKA_AMR_API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMapZoneTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MapZones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreateBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CreateApp = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUpdateBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    LastUpdateApp = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ZoneName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ZoneCode = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ZoneDescription = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ZoneColor = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    MapCode = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    FloorNumber = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Points = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Nodes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Edges = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CustomerUi = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ZoneType = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    BeginTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Configs = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MapZones", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MapZones_MapCode",
                table: "MapZones",
                column: "MapCode");

            migrationBuilder.CreateIndex(
                name: "IX_MapZones_ZoneCode",
                table: "MapZones",
                column: "ZoneCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MapZones");
        }
    }
}
