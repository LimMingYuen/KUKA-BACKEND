using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QES_KUKA_AMR_API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSavedMissionIdToScheduleLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add the new column with default value 0 initially
            migrationBuilder.AddColumn<int>(
                name: "SavedMissionId",
                table: "SavedMissionScheduleLogs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Populate existing rows with SavedMissionId from the related SavedMissionSchedule
            migrationBuilder.Sql(@"
                UPDATE sml
                SET sml.SavedMissionId = sms.SavedMissionId
                FROM SavedMissionScheduleLogs sml
                INNER JOIN SavedMissionSchedules sms ON sml.ScheduleId = sms.Id
            ");

            // Create index for efficient querying
            migrationBuilder.CreateIndex(
                name: "IX_SavedMissionScheduleLogs_SavedMissionId_CreatedUtc",
                table: "SavedMissionScheduleLogs",
                columns: new[] { "SavedMissionId", "CreatedUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SavedMissionScheduleLogs_SavedMissionId_CreatedUtc",
                table: "SavedMissionScheduleLogs");

            migrationBuilder.DropColumn(
                name: "SavedMissionId",
                table: "SavedMissionScheduleLogs");
        }
    }
}
