using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QES_KUKA_AMR_API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRobotManualPauseTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RobotManualPauses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RobotId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MissionCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    WaypointCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PauseStartUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PauseEndUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RobotManualPauses", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RobotManualPauses_MissionCode_PauseStartUtc",
                table: "RobotManualPauses",
                columns: new[] { "MissionCode", "PauseStartUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_RobotManualPauses_RobotId_PauseStartUtc",
                table: "RobotManualPauses",
                columns: new[] { "RobotId", "PauseStartUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RobotManualPauses");
        }
    }
}
