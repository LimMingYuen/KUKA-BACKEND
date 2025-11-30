using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QES_KUKA_AMR_API.Migrations
{
    /// <inheritdoc />
    public partial class RemoveWarehouseLiveMap : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FloorMaps");

            migrationBuilder.DropTable(
                name: "MapEdges");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FloorMaps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EdgeCount = table.Column<int>(type: "int", nullable: false),
                    FloorLength = table.Column<double>(type: "float", nullable: false),
                    FloorLevel = table.Column<int>(type: "int", nullable: false),
                    FloorMapVersion = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    FloorName = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    FloorNumber = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    FloorWidth = table.Column<double>(type: "float", nullable: false),
                    LaserMapId = table.Column<int>(type: "int", nullable: true),
                    LastUpdateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MapCode = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    NodeCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FloorMaps", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MapEdges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BeginNodeLabel = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CreateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EdgeLength = table.Column<double>(type: "float", nullable: false),
                    EdgeType = table.Column<int>(type: "int", nullable: false),
                    EdgeWeight = table.Column<double>(type: "float", nullable: false),
                    EdgeWidth = table.Column<double>(type: "float", nullable: false),
                    EndNodeLabel = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    FloorNumber = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MapCode = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    MaxAccelerationVelocity = table.Column<double>(type: "float", nullable: false),
                    MaxDecelerationVelocity = table.Column<double>(type: "float", nullable: false),
                    MaxVelocity = table.Column<double>(type: "float", nullable: false),
                    Orientation = table.Column<int>(type: "int", nullable: false),
                    Radius = table.Column<double>(type: "float", nullable: false),
                    RoadType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MapEdges", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FloorMaps_MapCode_FloorNumber",
                table: "FloorMaps",
                columns: new[] { "MapCode", "FloorNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MapEdges_BeginNodeLabel_EndNodeLabel_MapCode",
                table: "MapEdges",
                columns: new[] { "BeginNodeLabel", "EndNodeLabel", "MapCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MapEdges_MapCode_FloorNumber",
                table: "MapEdges",
                columns: new[] { "MapCode", "FloorNumber" });
        }
    }
}
