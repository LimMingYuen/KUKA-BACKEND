using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QES_KUKA_AMR_API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSavedMissionScheduleLogIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_SavedMissionScheduleLogs_ScheduleId_CreatedUtc",
                table: "SavedMissionScheduleLogs",
                columns: new[] { "ScheduleId", "CreatedUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SavedMissionScheduleLogs_ScheduleId_CreatedUtc",
                table: "SavedMissionScheduleLogs");
        }
    }
}
