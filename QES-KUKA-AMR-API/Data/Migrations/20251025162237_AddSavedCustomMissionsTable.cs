using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QES_KUKA_AMR_API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSavedCustomMissionsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SavedCustomMissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MissionName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MissionType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RobotType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    RobotModels = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RobotIds = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ContainerModelCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ContainerCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IdleNode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    MissionStepsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedCustomMissions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SavedCustomMissions_CreatedBy_IsDeleted",
                table: "SavedCustomMissions",
                columns: new[] { "CreatedBy", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_SavedCustomMissions_MissionName",
                table: "SavedCustomMissions",
                column: "MissionName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SavedCustomMissions");
        }
    }
}
