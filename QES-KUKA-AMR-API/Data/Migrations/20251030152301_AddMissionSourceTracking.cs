using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QES_KUKA_AMR_API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMissionSourceTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create RobotManualPauses table if it doesn't exist
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'RobotManualPauses')
                BEGIN
                    CREATE TABLE [RobotManualPauses] (
                        [Id] int NOT NULL IDENTITY(1,1),
                        [RobotId] nvarchar(50) NOT NULL,
                        [MissionCode] nvarchar(100) NOT NULL,
                        [WaypointCode] nvarchar(100) NULL,
                        [PauseStartUtc] datetime2 NOT NULL,
                        [PauseEndUtc] datetime2 NULL,
                        [Reason] nvarchar(200) NULL,
                        [CreatedUtc] datetime2 NOT NULL,
                        [UpdatedUtc] datetime2 NOT NULL,
                        CONSTRAINT [PK_RobotManualPauses] PRIMARY KEY ([Id])
                    );
                    CREATE INDEX [IX_RobotManualPauses_RobotId] ON [RobotManualPauses] ([RobotId]);
                    CREATE INDEX [IX_RobotManualPauses_MissionCode] ON [RobotManualPauses] ([MissionCode]);
                END
            ");

            // Create SavedMissionSchedules table if it doesn't exist
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SavedMissionSchedules')
                BEGIN
                    CREATE TABLE [SavedMissionSchedules] (
                        [Id] int NOT NULL IDENTITY(1,1),
                        [SavedMissionId] int NOT NULL,
                        [TriggerType] int NOT NULL,
                        [CronExpression] nvarchar(120) NULL,
                        [OneTimeRunUtc] datetime2 NULL,
                        [TimezoneId] nvarchar(100) NULL DEFAULT N'UTC',
                        [IsEnabled] bit NOT NULL DEFAULT 1,
                        [CreatedUtc] datetime2 NOT NULL,
                        [UpdatedUtc] datetime2 NOT NULL,
                        [LastRunUtc] datetime2 NULL,
                        [LastStatus] nvarchar(30) NULL,
                        [LastError] nvarchar(500) NULL,
                        [NextRunUtc] datetime2 NULL,
                        [QueueLockToken] nvarchar(80) NULL,
                        CONSTRAINT [PK_SavedMissionSchedules] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_SavedMissionSchedules_SavedCustomMissions_SavedMissionId] FOREIGN KEY ([SavedMissionId]) REFERENCES [SavedCustomMissions] ([Id]) ON DELETE CASCADE
                    );
                    CREATE INDEX [IX_SavedMissionSchedules_NextRunUtc] ON [SavedMissionSchedules] ([NextRunUtc]);
                    CREATE INDEX [IX_SavedMissionSchedules_SavedMissionId_IsEnabled] ON [SavedMissionSchedules] ([SavedMissionId], [IsEnabled]);
                END
            ");

            // Create SavedMissionScheduleLogs table if it doesn't exist
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SavedMissionScheduleLogs')
                BEGIN
                    CREATE TABLE [SavedMissionScheduleLogs] (
                        [Id] int NOT NULL IDENTITY(1,1),
                        [ScheduleId] int NOT NULL,
                        [ScheduledForUtc] datetime2 NOT NULL,
                        [EnqueuedUtc] datetime2 NULL,
                        [QueueId] int NULL,
                        [ResultStatus] nvarchar(30) NULL DEFAULT N'Pending',
                        [Error] nvarchar(500) NULL,
                        [CreatedUtc] datetime2 NOT NULL,
                        CONSTRAINT [PK_SavedMissionScheduleLogs] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_SavedMissionScheduleLogs_SavedMissionSchedules_ScheduleId] FOREIGN KEY ([ScheduleId]) REFERENCES [SavedMissionSchedules] ([Id]) ON DELETE CASCADE
                    );
                    CREATE INDEX [IX_SavedMissionScheduleLogs_ScheduleId] ON [SavedMissionScheduleLogs] ([ScheduleId]);
                END
            ");

            migrationBuilder.AlterColumn<string>(
                name: "TimezoneId",
                table: "SavedMissionSchedules",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true,
                oldDefaultValue: "UTC");

            migrationBuilder.AlterColumn<bool>(
                name: "IsEnabled",
                table: "SavedMissionSchedules",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<string>(
                name: "ResultStatus",
                table: "SavedMissionScheduleLogs",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30,
                oldNullable: true,
                oldDefaultValue: "Pending");

            migrationBuilder.AddColumn<int>(
                name: "SavedMissionId",
                table: "MissionQueues",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TriggerSource",
                table: "MissionQueues",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_MissionQueues_AssignedRobotId_CompletedDate",
                table: "MissionQueues",
                columns: new[] { "AssignedRobotId", "CompletedDate" });

            migrationBuilder.CreateIndex(
                name: "IX_MissionQueues_SavedMissionId",
                table: "MissionQueues",
                column: "SavedMissionId");

            migrationBuilder.CreateIndex(
                name: "IX_MissionQueues_TriggerSource_CompletedDate",
                table: "MissionQueues",
                columns: new[] { "TriggerSource", "CompletedDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MissionQueues_AssignedRobotId_CompletedDate",
                table: "MissionQueues");

            migrationBuilder.DropIndex(
                name: "IX_MissionQueues_SavedMissionId",
                table: "MissionQueues");

            migrationBuilder.DropIndex(
                name: "IX_MissionQueues_TriggerSource_CompletedDate",
                table: "MissionQueues");

            migrationBuilder.DropColumn(
                name: "SavedMissionId",
                table: "MissionQueues");

            migrationBuilder.DropColumn(
                name: "TriggerSource",
                table: "MissionQueues");

            migrationBuilder.AlterColumn<string>(
                name: "TimezoneId",
                table: "SavedMissionSchedules",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                defaultValue: "UTC",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<bool>(
                name: "IsEnabled",
                table: "SavedMissionSchedules",
                type: "bit",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<string>(
                name: "ResultStatus",
                table: "SavedMissionScheduleLogs",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true,
                defaultValue: "Pending",
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30);
        }
    }
}
