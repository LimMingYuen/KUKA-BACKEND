using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QES_KUKA_AMR_API.Migrations
{
    /// <inheritdoc />
    public partial class AddSaveCustomMissionTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "LockRobotAfterFinish",
                table: "SavedCustomMissions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "OrgId",
                table: "SavedCustomMissions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TemplateCode",
                table: "SavedCustomMissions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UnlockMissionCode",
                table: "SavedCustomMissions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UnlockRobotId",
                table: "SavedCustomMissions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ViewBoardType",
                table: "SavedCustomMissions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LockRobotAfterFinish",
                table: "SavedCustomMissions");

            migrationBuilder.DropColumn(
                name: "OrgId",
                table: "SavedCustomMissions");

            migrationBuilder.DropColumn(
                name: "TemplateCode",
                table: "SavedCustomMissions");

            migrationBuilder.DropColumn(
                name: "UnlockMissionCode",
                table: "SavedCustomMissions");

            migrationBuilder.DropColumn(
                name: "UnlockRobotId",
                table: "SavedCustomMissions");

            migrationBuilder.DropColumn(
                name: "ViewBoardType",
                table: "SavedCustomMissions");
        }
    }
}
