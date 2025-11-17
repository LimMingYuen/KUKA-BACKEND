using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QES_KUKA_AMR_API.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveQueueSystemTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop queue system tables if they exist
            migrationBuilder.Sql(@"
                IF OBJECT_ID(N'[dbo].[MissionQueueItems]', N'U') IS NOT NULL
                    DROP TABLE [dbo].[MissionQueueItems];
            ");

            migrationBuilder.Sql(@"
                IF OBJECT_ID(N'[dbo].[RobotJobOpportunities]', N'U') IS NOT NULL
                    DROP TABLE [dbo].[RobotJobOpportunities];
            ");

            migrationBuilder.Sql(@"
                IF OBJECT_ID(N'[dbo].[MapCodeQueueConfigurations]', N'U') IS NOT NULL
                    DROP TABLE [dbo].[MapCodeQueueConfigurations];
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // This migration is not reversible as we've removed the entity definitions
            // If you need to restore the queue system, revert the code changes first
            throw new NotSupportedException("Cannot revert queue system removal. The entity definitions have been deleted.");
        }
    }
}
