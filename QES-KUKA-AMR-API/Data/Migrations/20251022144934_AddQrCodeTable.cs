using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QES_KUKA_AMR_API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddQrCodeTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QrCodes",
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
                    NodeLabel = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Reliability = table.Column<int>(type: "int", nullable: false),
                    MapCode = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    FloorNumber = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    NodeNumber = table.Column<int>(type: "int", nullable: false),
                    ReportTimes = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QrCodes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QrCodes_MapCode",
                table: "QrCodes",
                column: "MapCode");

            migrationBuilder.CreateIndex(
                name: "IX_QrCodes_NodeLabel_MapCode",
                table: "QrCodes",
                columns: new[] { "NodeLabel", "MapCode" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QrCodes");
        }
    }
}
