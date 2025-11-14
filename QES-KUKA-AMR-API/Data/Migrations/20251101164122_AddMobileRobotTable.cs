using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QES_KUKA_AMR_API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMobileRobotTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "WorkflowName",
                table: "MissionQueues",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<int>(
                name: "WorkflowId",
                table: "MissionQueues",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "WorkflowCode",
                table: "MissionQueues",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "TemplateCode",
                table: "MissionQueues",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            // Check if column exists before adding it to avoid duplicate column error
            // Using raw SQL to check if column exists
            var checkColumnExistsSql = @"
                IF NOT EXISTS (
                    SELECT * FROM sys.columns 
                    WHERE object_id = OBJECT_ID(N'[MissionQueues]') 
                    AND name = N'AssignedRobotId'
                )
                BEGIN
                    ALTER TABLE [MissionQueues] ADD [AssignedRobotId] nvarchar(50) NULL
                END";
            migrationBuilder.Sql(checkColumnExistsSql);

            // Using raw SQL to check if column exists before adding it to avoid duplicate column error
            var checkContainerCodeSql = @"
                IF NOT EXISTS (
                    SELECT * FROM sys.columns 
                    WHERE object_id = OBJECT_ID(N'[MissionQueues]') 
                    AND name = N'ContainerCode'
                )
                BEGIN
                    ALTER TABLE [MissionQueues] ADD [ContainerCode] nvarchar(100) NULL
                END";
            migrationBuilder.Sql(checkContainerCodeSql);

            // Using raw SQL to check if column exists before adding it to avoid duplicate column error
            var checkContainerModelCodeSql = @"
                IF NOT EXISTS (
                    SELECT * FROM sys.columns 
                    WHERE object_id = OBJECT_ID(N'[MissionQueues]') 
                    AND name = N'ContainerModelCode'
                )
                BEGIN
                    ALTER TABLE [MissionQueues] ADD [ContainerModelCode] nvarchar(100) NULL
                END";
            migrationBuilder.Sql(checkContainerModelCodeSql);

            // Using raw SQL to check if column exists before adding it to avoid duplicate column error
            var checkCurrentManualWaypointPositionSql = @"
                IF NOT EXISTS (
                    SELECT * FROM sys.columns 
                    WHERE object_id = OBJECT_ID(N'[MissionQueues]') 
                    AND name = N'CurrentManualWaypointPosition'
                )
                BEGIN
                    ALTER TABLE [MissionQueues] ADD [CurrentManualWaypointPosition] nvarchar(100) NULL
                END";
            migrationBuilder.Sql(checkCurrentManualWaypointPositionSql);

            // Using raw SQL to check if column exists before adding it to avoid duplicate column error
            var checkIdleNodeSql = @"
                IF NOT EXISTS (
                    SELECT * FROM sys.columns 
                    WHERE object_id = OBJECT_ID(N'[MissionQueues]') 
                    AND name = N'IdleNode'
                )
                BEGIN
                    ALTER TABLE [MissionQueues] ADD [IdleNode] nvarchar(100) NULL
                END";
            migrationBuilder.Sql(checkIdleNodeSql);

            // Using raw SQL to check if column exists before adding it to avoid duplicate column error
            var checkIsWaitingForManualResumeSql = @"
                IF NOT EXISTS (
                    SELECT * FROM sys.columns 
                    WHERE object_id = OBJECT_ID(N'[MissionQueues]') 
                    AND name = N'IsWaitingForManualResume'
                )
                BEGIN
                    ALTER TABLE [MissionQueues] ADD [IsWaitingForManualResume] bit NOT NULL DEFAULT 0
                END";
            migrationBuilder.Sql(checkIsWaitingForManualResumeSql);

            // Using raw SQL to check if column exists before adding it to avoid duplicate column error
            var checkLastRobotQueryTimeSql = @"
                IF NOT EXISTS (
                    SELECT * FROM sys.columns 
                    WHERE object_id = OBJECT_ID(N'[MissionQueues]') 
                    AND name = N'LastRobotQueryTime'
                )
                BEGIN
                    ALTER TABLE [MissionQueues] ADD [LastRobotQueryTime] datetime2 NULL
                END";
            migrationBuilder.Sql(checkLastRobotQueryTimeSql);

            // Using raw SQL to check if column exists before adding it to avoid duplicate column error
            var checkLockRobotAfterFinishSql = @"
                IF NOT EXISTS (
                    SELECT * FROM sys.columns 
                    WHERE object_id = OBJECT_ID(N'[MissionQueues]') 
                    AND name = N'LockRobotAfterFinish'
                )
                BEGIN
                    ALTER TABLE [MissionQueues] ADD [LockRobotAfterFinish] bit NOT NULL DEFAULT 0
                END";
            migrationBuilder.Sql(checkLockRobotAfterFinishSql);

            // Using raw SQL to check if column exists before adding it to avoid duplicate column error
            var checkManualWaypointsJsonSql = @"
                IF NOT EXISTS (
                    SELECT * FROM sys.columns 
                    WHERE object_id = OBJECT_ID(N'[MissionQueues]') 
                    AND name = N'ManualWaypointsJson'
                )
                BEGIN
                    ALTER TABLE [MissionQueues] ADD [ManualWaypointsJson] nvarchar(max) NULL
                END";
            migrationBuilder.Sql(checkManualWaypointsJsonSql);

            // Using raw SQL to check if column exists before adding it to avoid duplicate column error
            var checkMissionDataJsonSql = @"
                IF NOT EXISTS (
                    SELECT * FROM sys.columns 
                    WHERE object_id = OBJECT_ID(N'[MissionQueues]') 
                    AND name = N'MissionDataJson'
                )
                BEGIN
                    ALTER TABLE [MissionQueues] ADD [MissionDataJson] nvarchar(max) NULL
                END";
            migrationBuilder.Sql(checkMissionDataJsonSql);

            // Using raw SQL to check if column exists before adding it to avoid duplicate column error
            var checkMissionTypeSql = @"
                IF NOT EXISTS (
                    SELECT * FROM sys.columns 
                    WHERE object_id = OBJECT_ID(N'[MissionQueues]') 
                    AND name = N'MissionType'
                )
                BEGIN
                    ALTER TABLE [MissionQueues] ADD [MissionType] nvarchar(50) NULL
                END";
            migrationBuilder.Sql(checkMissionTypeSql);

            // Using raw SQL to check if column exists before adding it to avoid duplicate column error
            var checkRobotBatteryLevelSql = @"
                IF NOT EXISTS (
                    SELECT * FROM sys.columns 
                    WHERE object_id = OBJECT_ID(N'[MissionQueues]') 
                    AND name = N'RobotBatteryLevel'
                )
                BEGIN
                    ALTER TABLE [MissionQueues] ADD [RobotBatteryLevel] int NULL
                END";
            migrationBuilder.Sql(checkRobotBatteryLevelSql);

            // Using raw SQL to check if column exists before adding it to avoid duplicate column error
            var checkRobotIdsSql = @"
                IF NOT EXISTS (
                    SELECT * FROM sys.columns 
                    WHERE object_id = OBJECT_ID(N'[MissionQueues]') 
                    AND name = N'RobotIds'
                )
                BEGIN
                    ALTER TABLE [MissionQueues] ADD [RobotIds] nvarchar(500) NULL
                END";
            migrationBuilder.Sql(checkRobotIdsSql);

            // Using raw SQL to check if column exists before adding it to avoid duplicate column error
            var checkRobotModelsSql = @"
                IF NOT EXISTS (
                    SELECT * FROM sys.columns 
                    WHERE object_id = OBJECT_ID(N'[MissionQueues]') 
                    AND name = N'RobotModels'
                )
                BEGIN
                    ALTER TABLE [MissionQueues] ADD [RobotModels] nvarchar(500) NULL
                END";
            migrationBuilder.Sql(checkRobotModelsSql);

            // Using raw SQL to check if column exists before adding it to avoid duplicate column error
            var checkRobotNodeCodeSql = @"
                IF NOT EXISTS (
                    SELECT * FROM sys.columns 
                    WHERE object_id = OBJECT_ID(N'[MissionQueues]') 
                    AND name = N'RobotNodeCode'
                )
                BEGIN
                    ALTER TABLE [MissionQueues] ADD [RobotNodeCode] nvarchar(100) NULL
                END";
            migrationBuilder.Sql(checkRobotNodeCodeSql);

            // Using raw SQL to check if column exists before adding it to avoid duplicate column error
            var checkRobotStatusCodeSql = @"
                IF NOT EXISTS (
                    SELECT * FROM sys.columns 
                    WHERE object_id = OBJECT_ID(N'[MissionQueues]') 
                    AND name = N'RobotStatusCode'
                )
                BEGIN
                    ALTER TABLE [MissionQueues] ADD [RobotStatusCode] int NULL
                END";
            migrationBuilder.Sql(checkRobotStatusCodeSql);

            // Using raw SQL to check if column exists before adding it to avoid duplicate column error
            var checkRobotTypeSql = @"
                IF NOT EXISTS (
                    SELECT * FROM sys.columns 
                    WHERE object_id = OBJECT_ID(N'[MissionQueues]') 
                    AND name = N'RobotType'
                )
                BEGIN
                    ALTER TABLE [MissionQueues] ADD [RobotType] nvarchar(50) NULL
                END";
            migrationBuilder.Sql(checkRobotTypeSql);

            // Using raw SQL to check if column exists before adding it to avoid duplicate column error
            var checkSavedMissionIdSql = @"
                IF NOT EXISTS (
                    SELECT * FROM sys.columns 
                    WHERE object_id = OBJECT_ID(N'[MissionQueues]') 
                    AND name = N'SavedMissionId'
                )
                BEGIN
                    ALTER TABLE [MissionQueues] ADD [SavedMissionId] int NULL
                END";
            migrationBuilder.Sql(checkSavedMissionIdSql);

            // Using raw SQL to check if column exists before adding it to avoid duplicate column error
            var checkTriggerSourceSql2 = @"
                IF NOT EXISTS (
                    SELECT * FROM sys.columns 
                    WHERE object_id = OBJECT_ID(N'[MissionQueues]') 
                    AND name = N'TriggerSource'
                )
                BEGIN
                    ALTER TABLE [MissionQueues] ADD [TriggerSource] int NOT NULL DEFAULT 0
                END";
            migrationBuilder.Sql(checkTriggerSourceSql2);

            // Using raw SQL to check if column exists before adding it to avoid duplicate column error
            var checkUnlockMissionCodeSql = @"
                IF NOT EXISTS (
                    SELECT * FROM sys.columns 
                    WHERE object_id = OBJECT_ID(N'[MissionQueues]') 
                    AND name = N'UnlockMissionCode'
                )
                BEGIN
                    ALTER TABLE [MissionQueues] ADD [UnlockMissionCode] nvarchar(100) NULL
                END";
            migrationBuilder.Sql(checkUnlockMissionCodeSql);

            // Using raw SQL to check if column exists before adding it to avoid duplicate column error
            var checkUnlockRobotIdSql = @"
                IF NOT EXISTS (
                    SELECT * FROM sys.columns 
                    WHERE object_id = OBJECT_ID(N'[MissionQueues]') 
                    AND name = N'UnlockRobotId'
                )
                BEGIN
                    ALTER TABLE [MissionQueues] ADD [UnlockRobotId] nvarchar(50) NULL
                END";
            migrationBuilder.Sql(checkUnlockRobotIdSql);

            // Using raw SQL to check if column exists before adding it to avoid duplicate column error
            var checkViewBoardTypeSql = @"
                IF NOT EXISTS (
                    SELECT * FROM sys.columns 
                    WHERE object_id = OBJECT_ID(N'[MissionQueues]') 
                    AND name = N'ViewBoardType'
                )
                BEGIN
                    ALTER TABLE [MissionQueues] ADD [ViewBoardType] nvarchar(50) NULL
                END";
            migrationBuilder.Sql(checkViewBoardTypeSql);

            // Using raw SQL to check if column exists before adding it to avoid duplicate column error
            var checkVisitedManualWaypointsJsonSql = @"
                IF NOT EXISTS (
                    SELECT * FROM sys.columns 
                    WHERE object_id = OBJECT_ID(N'[MissionQueues]') 
                    AND name = N'VisitedManualWaypointsJson'
                )
                BEGIN
                    ALTER TABLE [MissionQueues] ADD [VisitedManualWaypointsJson] nvarchar(max) NULL
                END";
            migrationBuilder.Sql(checkVisitedManualWaypointsJsonSql);

            migrationBuilder.AlterColumn<string>(
                name: "WorkflowName",
                table: "MissionHistories",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            // Check if column exists before adding it to avoid duplicate column error
            // Using raw SQL to check if column exists
            var checkColumnExistsSql2 = @"
                IF NOT EXISTS (
                    SELECT * FROM sys.columns 
                    WHERE object_id = OBJECT_ID(N'[MissionHistories]') 
                    AND name = N'AssignedRobotId'
                )
                BEGIN
                    ALTER TABLE [MissionHistories] ADD [AssignedRobotId] nvarchar(50) NULL
                END";
            migrationBuilder.Sql(checkColumnExistsSql2);

            // Using raw SQL to check if column exists before adding it to avoid duplicate column error
            var checkCompletedDateSql = @"
                IF NOT EXISTS (
                    SELECT * FROM sys.columns 
                    WHERE object_id = OBJECT_ID(N'[MissionHistories]') 
                    AND name = N'CompletedDate'
                )
                BEGIN
                    ALTER TABLE [MissionHistories] ADD [CompletedDate] datetime2 NULL
                END";
            migrationBuilder.Sql(checkCompletedDateSql);

            // Using raw SQL to check if column exists before adding it to avoid duplicate column error
            var checkCreatedBySql = @"
                IF NOT EXISTS (
                    SELECT * FROM sys.columns 
                    WHERE object_id = OBJECT_ID(N'[MissionHistories]') 
                    AND name = N'CreatedBy'
                )
                BEGIN
                    ALTER TABLE [MissionHistories] ADD [CreatedBy] nvarchar(100) NULL
                END";
            migrationBuilder.Sql(checkCreatedBySql);

            // Using raw SQL to check if column exists before adding it to avoid duplicate column error
            var checkErrorMessageSql = @"
                IF NOT EXISTS (
                    SELECT * FROM sys.columns 
                    WHERE object_id = OBJECT_ID(N'[MissionHistories]') 
                    AND name = N'ErrorMessage'
                )
                BEGIN
                    ALTER TABLE [MissionHistories] ADD [ErrorMessage] nvarchar(500) NULL
                END";
            migrationBuilder.Sql(checkErrorMessageSql);

            // Using raw SQL to check if column exists before adding it to avoid duplicate column error
            var checkMissionTypeSqlMH = @"
                IF NOT EXISTS (
                    SELECT * FROM sys.columns 
                    WHERE object_id = OBJECT_ID(N'[MissionHistories]') 
                    AND name = N'MissionType'
                )
                BEGIN
                    ALTER TABLE [MissionHistories] ADD [MissionType] nvarchar(50) NULL
                END";
            migrationBuilder.Sql(checkMissionTypeSqlMH);

            // Using raw SQL to check if column exists before adding it to avoid duplicate column error
            var checkProcessedDateSql = @"
                IF NOT EXISTS (
                    SELECT * FROM sys.columns 
                    WHERE object_id = OBJECT_ID(N'[MissionHistories]') 
                    AND name = N'ProcessedDate'
                )
                BEGIN
                    ALTER TABLE [MissionHistories] ADD [ProcessedDate] datetime2 NULL
                END";
            migrationBuilder.Sql(checkProcessedDateSql);

            // Using raw SQL to check if column exists before adding it to avoid duplicate column error
            var checkSavedMissionIdMHSql = @"
                IF NOT EXISTS (
                    SELECT * FROM sys.columns 
                    WHERE object_id = OBJECT_ID(N'[MissionHistories]') 
                    AND name = N'SavedMissionId'
                )
                BEGIN
                    ALTER TABLE [MissionHistories] ADD [SavedMissionId] int NULL
                END";
            migrationBuilder.Sql(checkSavedMissionIdMHSql);

            // Using raw SQL to check if column exists before adding it to avoid duplicate column error
            var checkSubmittedToAmrDateSql = @"
                IF NOT EXISTS (
                    SELECT * FROM sys.columns 
                    WHERE object_id = OBJECT_ID(N'[MissionHistories]') 
                    AND name = N'SubmittedToAmrDate'
                )
                BEGIN
                    ALTER TABLE [MissionHistories] ADD [SubmittedToAmrDate] datetime2 NULL
                END";
            migrationBuilder.Sql(checkSubmittedToAmrDateSql);

            // Using raw SQL to check if column exists before adding it to avoid duplicate column error
            var checkTriggerSourceSql = @"
                IF NOT EXISTS (
                    SELECT * FROM sys.columns 
                    WHERE object_id = OBJECT_ID(N'[MissionHistories]') 
                    AND name = N'TriggerSource'
                )
                BEGIN
                    ALTER TABLE [MissionHistories] ADD [TriggerSource] int NOT NULL DEFAULT 0
                END";
            migrationBuilder.Sql(checkTriggerSourceSql);

            // Using raw SQL to check if column exists before adding it to avoid duplicate column error
            var checkWorkflowIdSql = @"
                IF NOT EXISTS (
                    SELECT * FROM sys.columns 
                    WHERE object_id = OBJECT_ID(N'[MissionHistories]') 
                    AND name = N'WorkflowId'
                )
                BEGIN
                    ALTER TABLE [MissionHistories] ADD [WorkflowId] int NULL
                END";
            migrationBuilder.Sql(checkWorkflowIdSql);

            // Using raw SQL to create table only if it doesn't exist
            var createAreasTableSql = @"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Areas')
                BEGIN
                    CREATE TABLE [Areas] (
                        [Id] int NOT NULL IDENTITY,
                        [DisplayName] nvarchar(128) NOT NULL,
                        [ActualValue] nvarchar(128) NOT NULL,
                        [Description] nvarchar(512) NULL,
                        [IsActive] bit NOT NULL,
                        [CreatedUtc] datetime2 NOT NULL,
                        [UpdatedUtc] datetime2 NOT NULL,
                        CONSTRAINT [PK_Areas] PRIMARY KEY ([Id])
                    )
                END";
            migrationBuilder.Sql(createAreasTableSql);

            // Using raw SQL to create table only if it doesn't exist
            var createMissionTypesTableSql = @"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'MissionTypes')
                BEGIN
                    CREATE TABLE [MissionTypes] (
                        [Id] int NOT NULL IDENTITY,
                        [DisplayName] nvarchar(128) NOT NULL,
                        [ActualValue] nvarchar(128) NOT NULL,
                        [Description] nvarchar(512) NULL,
                        [IsActive] bit NOT NULL,
                        [CreatedUtc] datetime2 NOT NULL,
                        [UpdatedUtc] datetime2 NOT NULL,
                        CONSTRAINT [PK_MissionTypes] PRIMARY KEY ([Id])
                    )
                END";
            migrationBuilder.Sql(createMissionTypesTableSql);

            // Using raw SQL to create table only if it doesn't exist
            var createMobileRobotsTableSql = @"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'MobileRobots')
                BEGIN
                    CREATE TABLE [MobileRobots] (
                        [Id] int NOT NULL IDENTITY,
                        [CreateTime] datetime2 NOT NULL,
                        [CreateBy] nvarchar(100) NOT NULL,
                        [CreateApp] nvarchar(100) NOT NULL,
                        [LastUpdateTime] datetime2 NOT NULL,
                        [LastUpdateBy] nvarchar(100) NOT NULL,
                        [LastUpdateApp] nvarchar(100) NOT NULL,
                        [RobotId] nvarchar(100) NOT NULL,
                        [RobotTypeCode] nvarchar(100) NOT NULL,
                        [BuildingCode] nvarchar(100) NOT NULL,
                        [MapCode] nvarchar(100) NOT NULL,
                        [FloorNumber] nvarchar(50) NOT NULL,
                        [LastNodeNumber] int NOT NULL,
                        [LastNodeDeleteFlag] bit NOT NULL,
                        [ContainerCode] nvarchar(100) NOT NULL,
                        [ActuatorType] int NOT NULL,
                        [ActuatorStatusInfo] nvarchar(255) NOT NULL,
                        [IpAddress] nvarchar(50) NOT NULL,
                        [WarningInfo] nvarchar(255) NOT NULL,
                        [ConfigVersion] nvarchar(50) NOT NULL,
                        [SendConfigVersion] nvarchar(50) NOT NULL,
                        [SendConfigTime] datetime2 NOT NULL,
                        [FirmwareVersion] nvarchar(100) NOT NULL,
                        [SendFirmwareVersion] nvarchar(100) NOT NULL,
                        [SendFirmwareTime] datetime2 NOT NULL,
                        [Status] int NOT NULL,
                        [OccupyStatus] int NOT NULL,
                        [BatteryLevel] int NOT NULL,
                        [Mileage] int NOT NULL,
                        [MissionCode] nvarchar(100) NOT NULL,
                        [MeetObstacleStatus] int NOT NULL,
                        [RobotOrientation] int NULL,
                        [Reliability] int NOT NULL,
                        [RunTime] int NULL,
                        [RobotTypeClass] int NULL,
                        [TrailerNum] nvarchar(100) NOT NULL,
                        [TractionStatus] nvarchar(100) NOT NULL,
                        [XCoordinate] float NULL,
                        [YCoordinate] float NULL,
                        CONSTRAINT [PK_MobileRobots] PRIMARY KEY ([Id])
                    )
                END";
            migrationBuilder.Sql(createMobileRobotsTableSql);

            // Using raw SQL to create table only if it doesn't exist
            var createResumeStrategiesTableSql = @"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ResumeStrategies')
                BEGIN
                    CREATE TABLE [ResumeStrategies] (
                        [Id] int NOT NULL IDENTITY,
                        [DisplayName] nvarchar(128) NOT NULL,
                        [ActualValue] nvarchar(128) NOT NULL,
                        [Description] nvarchar(512) NULL,
                        [IsActive] bit NOT NULL,
                        [CreatedUtc] datetime2 NOT NULL,
                        [UpdatedUtc] datetime2 NOT NULL,
                        CONSTRAINT [PK_ResumeStrategies] PRIMARY KEY ([Id])
                    )
                END";
            migrationBuilder.Sql(createResumeStrategiesTableSql);

            // Using raw SQL to create table only if it doesn't exist
            var createRobotManualPausesTableSql = @"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'RobotManualPauses')
                BEGIN
                    CREATE TABLE [RobotManualPauses] (
                        [Id] int NOT NULL IDENTITY,
                        [RobotId] nvarchar(50) NOT NULL,
                        [MissionCode] nvarchar(100) NOT NULL,
                        [WaypointCode] nvarchar(100) NULL,
                        [PauseStartUtc] datetime2 NOT NULL,
                        [PauseEndUtc] datetime2 NULL,
                        [Reason] nvarchar(200) NULL,
                        [CreatedUtc] datetime2 NOT NULL,
                        [UpdatedUtc] datetime2 NOT NULL,
                        CONSTRAINT [PK_RobotManualPauses] PRIMARY KEY ([Id])
                    )
                END";
            migrationBuilder.Sql(createRobotManualPausesTableSql);

            // Using raw SQL to create table only if it doesn't exist
            var createRobotTypesTableSql = @"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'RobotTypes')
                BEGIN
                    CREATE TABLE [RobotTypes] (
                        [Id] int NOT NULL IDENTITY,
                        [DisplayName] nvarchar(128) NOT NULL,
                        [ActualValue] nvarchar(128) NOT NULL,
                        [Description] nvarchar(512) NULL,
                        [IsActive] bit NOT NULL,
                        [CreatedUtc] datetime2 NOT NULL,
                        [UpdatedUtc] datetime2 NOT NULL,
                        CONSTRAINT [PK_RobotTypes] PRIMARY KEY ([Id])
                    )
                END";
            migrationBuilder.Sql(createRobotTypesTableSql);

            // Using raw SQL to create table only if it doesn't exist
            var createSavedCustomMissionsTableSql = @"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SavedCustomMissions')
                BEGIN
                    CREATE TABLE [SavedCustomMissions] (
                        [Id] int NOT NULL IDENTITY,
                        [MissionName] nvarchar(200) NOT NULL,
                        [Description] nvarchar(500) NULL,
                        [MissionType] nvarchar(50) NOT NULL,
                        [RobotType] nvarchar(50) NOT NULL,
                        [Priority] int NOT NULL,
                        [RobotModels] nvarchar(500) NULL,
                        [RobotIds] nvarchar(500) NULL,
                        [ContainerModelCode] nvarchar(100) NULL,
                        [ContainerCode] nvarchar(100) NULL,
                        [IdleNode] nvarchar(100) NULL,
                        [MissionStepsJson] nvarchar(max) NOT NULL,
                        [CreatedBy] nvarchar(100) NOT NULL,
                        [CreatedUtc] datetime2 NOT NULL,
                        [UpdatedUtc] datetime2 NOT NULL,
                        [IsDeleted] bit NOT NULL,
                        CONSTRAINT [PK_SavedCustomMissions] PRIMARY KEY ([Id])
                    )
                END";
            migrationBuilder.Sql(createSavedCustomMissionsTableSql);

            // Using raw SQL to create table only if it doesn't exist
            var createShelfDecisionRulesTableSql = @"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ShelfDecisionRules')
                BEGIN
                    CREATE TABLE [ShelfDecisionRules] (
                        [Id] int NOT NULL IDENTITY,
                        [DisplayName] nvarchar(128) NOT NULL,
                        [ActualValue] nvarchar(128) NOT NULL,
                        [Description] nvarchar(512) NULL,
                        [IsActive] bit NOT NULL,
                        [CreatedUtc] datetime2 NOT NULL,
                        [UpdatedUtc] datetime2 NOT NULL,
                        CONSTRAINT [PK_ShelfDecisionRules] PRIMARY KEY ([Id])
                    )
                END";
            migrationBuilder.Sql(createShelfDecisionRulesTableSql);

            // Using raw SQL to create table only if it doesn't exist
            var createSavedMissionSchedulesTableSql = @"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SavedMissionSchedules')
                BEGIN
                    CREATE TABLE [SavedMissionSchedules] (
                        [Id] int NOT NULL IDENTITY,
                        [SavedMissionId] int NOT NULL,
                        [TriggerType] int NOT NULL,
                        [CronExpression] nvarchar(120) NULL,
                        [OneTimeRunUtc] datetime2 NULL,
                        [TimezoneId] nvarchar(100) NOT NULL,
                        [IsEnabled] bit NOT NULL,
                        [CreatedUtc] datetime2 NOT NULL,
                        [UpdatedUtc] datetime2 NOT NULL,
                        [LastRunUtc] datetime2 NULL,
                        [LastStatus] nvarchar(30) NULL,
                        [LastError] nvarchar(500) NULL,
                        [NextRunUtc] datetime2 NULL,
                        [QueueLockToken] nvarchar(80) NULL,
                        CONSTRAINT [PK_SavedMissionSchedules] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_SavedMissionSchedules_SavedCustomMissions_SavedMissionId] FOREIGN KEY ([SavedMissionId]) REFERENCES [SavedCustomMissions] ([Id]) ON DELETE CASCADE
                    )
                END";
            migrationBuilder.Sql(createSavedMissionSchedulesTableSql);

            // Using raw SQL to create table only if it doesn't exist
            var createSavedMissionScheduleLogsTableSql = @"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SavedMissionScheduleLogs')
                BEGIN
                    CREATE TABLE [SavedMissionScheduleLogs] (
                        [Id] int NOT NULL IDENTITY,
                        [ScheduleId] int NOT NULL,
                        [ScheduledForUtc] datetime2 NOT NULL,
                        [EnqueuedUtc] datetime2 NULL,
                        [QueueId] int NULL,
                        [ResultStatus] nvarchar(30) NOT NULL,
                        [Error] nvarchar(500) NULL,
                        [CreatedUtc] datetime2 NOT NULL,
                        CONSTRAINT [PK_SavedMissionScheduleLogs] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_SavedMissionScheduleLogs_SavedMissionSchedules_ScheduleId] FOREIGN KEY ([ScheduleId]) REFERENCES [SavedMissionSchedules] ([Id]) ON DELETE CASCADE
                    )
                END";
            migrationBuilder.Sql(createSavedMissionScheduleLogsTableSql);

            // Using raw SQL to create the index only if it doesn't exist
            var createIndexSql1 = @"
                IF NOT EXISTS (
                    SELECT name FROM sys.indexes 
                    WHERE object_id = OBJECT_ID(N'[MissionQueues]') 
                    AND name = N'IX_MissionQueues_AssignedRobotId_CompletedDate'
                )
                BEGIN
                    CREATE INDEX [IX_MissionQueues_AssignedRobotId_CompletedDate] ON [MissionQueues] ([AssignedRobotId], [CompletedDate])
                END";
            migrationBuilder.Sql(createIndexSql1);

            // Using raw SQL to create the index only if it doesn't exist
            var createIXMissionQueuesSavedMissionIdSql = @"
                IF NOT EXISTS (
                    SELECT name FROM sys.indexes 
                    WHERE object_id = OBJECT_ID(N'[MissionQueues]') 
                    AND name = N'IX_MissionQueues_SavedMissionId'
                )
                BEGIN
                    CREATE INDEX [IX_MissionQueues_SavedMissionId] ON [MissionQueues] ([SavedMissionId])
                END";
            migrationBuilder.Sql(createIXMissionQueuesSavedMissionIdSql);

            // Using raw SQL to create the index only if it doesn't exist
            var createIXMissionQueuesTriggerSourceCompletedDateSql = @"
                IF NOT EXISTS (
                    SELECT name FROM sys.indexes 
                    WHERE object_id = OBJECT_ID(N'[MissionQueues]') 
                    AND name = N'IX_MissionQueues_TriggerSource_CompletedDate'
                )
                BEGIN
                    CREATE INDEX [IX_MissionQueues_TriggerSource_CompletedDate] ON [MissionQueues] ([TriggerSource], [CompletedDate])
                END";
            migrationBuilder.Sql(createIXMissionQueuesTriggerSourceCompletedDateSql);

            // Using raw SQL to create the index only if it doesn't exist
            var createIndexSql2 = @"
                IF NOT EXISTS (
                    SELECT name FROM sys.indexes 
                    WHERE object_id = OBJECT_ID(N'[MissionHistories]') 
                    AND name = N'IX_MissionHistories_AssignedRobotId_CompletedDate'
                )
                BEGIN
                    CREATE INDEX [IX_MissionHistories_AssignedRobotId_CompletedDate] ON [MissionHistories] ([AssignedRobotId], [CompletedDate])
                END";
            migrationBuilder.Sql(createIndexSql2);

            // Using raw SQL to create the index only if it doesn't exist
            var createIXMissionHistoriesSavedMissionIdSql = @"
                IF NOT EXISTS (
                    SELECT name FROM sys.indexes 
                    WHERE object_id = OBJECT_ID(N'[MissionHistories]') 
                    AND name = N'IX_MissionHistories_SavedMissionId'
                )
                BEGIN
                    CREATE INDEX [IX_MissionHistories_SavedMissionId] ON [MissionHistories] ([SavedMissionId])
                END";
            migrationBuilder.Sql(createIXMissionHistoriesSavedMissionIdSql);

            // Using raw SQL to create the index only if it doesn't exist
            var createIXMissionHistoriesTriggerSourceCompletedDateSql = @"
                IF NOT EXISTS (
                    SELECT name FROM sys.indexes 
                    WHERE object_id = OBJECT_ID(N'[MissionHistories]') 
                    AND name = N'IX_MissionHistories_TriggerSource_CompletedDate'
                )
                BEGIN
                    CREATE INDEX [IX_MissionHistories_TriggerSource_CompletedDate] ON [MissionHistories] ([TriggerSource], [CompletedDate])
                END";
            migrationBuilder.Sql(createIXMissionHistoriesTriggerSourceCompletedDateSql);

            // Using raw SQL to create the index only if it doesn't exist
            var createIXAreasActualValueSql = @"
                IF NOT EXISTS (
                    SELECT name FROM sys.indexes 
                    WHERE object_id = OBJECT_ID(N'[Areas]') 
                    AND name = N'IX_Areas_ActualValue'
                )
                BEGIN
                    CREATE UNIQUE INDEX [IX_Areas_ActualValue] ON [Areas] ([ActualValue])
                END";
            migrationBuilder.Sql(createIXAreasActualValueSql);

            // Using raw SQL to create the index only if it doesn't exist
            var createIXMissionTypesActualValueSql = @"
                IF NOT EXISTS (
                    SELECT name FROM sys.indexes 
                    WHERE object_id = OBJECT_ID(N'[MissionTypes]') 
                    AND name = N'IX_MissionTypes_ActualValue'
                )
                BEGIN
                    CREATE UNIQUE INDEX [IX_MissionTypes_ActualValue] ON [MissionTypes] ([ActualValue])
                END";
            migrationBuilder.Sql(createIXMissionTypesActualValueSql);

            // Using raw SQL to create the index only if it doesn't exist
            var createIXResumeStrategiesActualValueSql = @"
                IF NOT EXISTS (
                    SELECT name FROM sys.indexes 
                    WHERE object_id = OBJECT_ID(N'[ResumeStrategies]') 
                    AND name = N'IX_ResumeStrategies_ActualValue'
                )
                BEGIN
                    CREATE UNIQUE INDEX [IX_ResumeStrategies_ActualValue] ON [ResumeStrategies] ([ActualValue])
                END";
            migrationBuilder.Sql(createIXResumeStrategiesActualValueSql);

            // Using raw SQL to create the index only if it doesn't exist
            var createIXRobotManualPausesMissionCodePauseStartUtcSql = @"
                IF NOT EXISTS (
                    SELECT name FROM sys.indexes 
                    WHERE object_id = OBJECT_ID(N'[RobotManualPauses]') 
                    AND name = N'IX_RobotManualPauses_MissionCode_PauseStartUtc'
                )
                BEGIN
                    CREATE INDEX [IX_RobotManualPauses_MissionCode_PauseStartUtc] ON [RobotManualPauses] ([MissionCode], [PauseStartUtc])
                END";
            migrationBuilder.Sql(createIXRobotManualPausesMissionCodePauseStartUtcSql);

            // Using raw SQL to create the index only if it doesn't exist
            var createIXRobotManualPausesRobotIdPauseStartUtcSql = @"
                IF NOT EXISTS (
                    SELECT name FROM sys.indexes 
                    WHERE object_id = OBJECT_ID(N'[RobotManualPauses]') 
                    AND name = N'IX_RobotManualPauses_RobotId_PauseStartUtc'
                )
                BEGIN
                    CREATE INDEX [IX_RobotManualPauses_RobotId_PauseStartUtc] ON [RobotManualPauses] ([RobotId], [PauseStartUtc])
                END";
            migrationBuilder.Sql(createIXRobotManualPausesRobotIdPauseStartUtcSql);

            // Using raw SQL to create the index only if it doesn't exist
            var createIXRobotTypesActualValueSql = @"
                IF NOT EXISTS (
                    SELECT name FROM sys.indexes 
                    WHERE object_id = OBJECT_ID(N'[RobotTypes]') 
                    AND name = N'IX_RobotTypes_ActualValue'
                )
                BEGIN
                    CREATE UNIQUE INDEX [IX_RobotTypes_ActualValue] ON [RobotTypes] ([ActualValue])
                END";
            migrationBuilder.Sql(createIXRobotTypesActualValueSql);

            // Using raw SQL to create the index only if it doesn't exist
            var createIXSavedCustomMissionsCreatedByIsDeletedSql = @"
                IF NOT EXISTS (
                    SELECT name FROM sys.indexes 
                    WHERE object_id = OBJECT_ID(N'[SavedCustomMissions]') 
                    AND name = N'IX_SavedCustomMissions_CreatedBy_IsDeleted'
                )
                BEGIN
                    CREATE INDEX [IX_SavedCustomMissions_CreatedBy_IsDeleted] ON [SavedCustomMissions] ([CreatedBy], [IsDeleted])
                END";
            migrationBuilder.Sql(createIXSavedCustomMissionsCreatedByIsDeletedSql);

            // Using raw SQL to create the index only if it doesn't exist
            var createIXSavedCustomMissionsMissionNameSql = @"
                IF NOT EXISTS (
                    SELECT name FROM sys.indexes 
                    WHERE object_id = OBJECT_ID(N'[SavedCustomMissions]') 
                    AND name = N'IX_SavedCustomMissions_MissionName'
                )
                BEGIN
                    CREATE INDEX [IX_SavedCustomMissions_MissionName] ON [SavedCustomMissions] ([MissionName])
                END";
            migrationBuilder.Sql(createIXSavedCustomMissionsMissionNameSql);

            // Using raw SQL to create the index only if it doesn't exist
            var createIXSavedMissionScheduleLogsScheduleIdSql = @"
                IF NOT EXISTS (
                    SELECT name FROM sys.indexes 
                    WHERE object_id = OBJECT_ID(N'[SavedMissionScheduleLogs]') 
                    AND name = N'IX_SavedMissionScheduleLogs_ScheduleId'
                )
                BEGIN
                    CREATE INDEX [IX_SavedMissionScheduleLogs_ScheduleId] ON [SavedMissionScheduleLogs] ([ScheduleId])
                END";
            migrationBuilder.Sql(createIXSavedMissionScheduleLogsScheduleIdSql);

            // Using raw SQL to create the index only if it doesn't exist
            var createIXSavedMissionSchedulesNextRunUtcSql = @"
                IF NOT EXISTS (
                    SELECT name FROM sys.indexes 
                    WHERE object_id = OBJECT_ID(N'[SavedMissionSchedules]') 
                    AND name = N'IX_SavedMissionSchedules_NextRunUtc'
                )
                BEGIN
                    CREATE INDEX [IX_SavedMissionSchedules_NextRunUtc] ON [SavedMissionSchedules] ([NextRunUtc])
                END";
            migrationBuilder.Sql(createIXSavedMissionSchedulesNextRunUtcSql);

            // Using raw SQL to create the index only if it doesn't exist
            var createIXSavedMissionSchedulesSavedMissionIdIsEnabledSql = @"
                IF NOT EXISTS (
                    SELECT name FROM sys.indexes 
                    WHERE object_id = OBJECT_ID(N'[SavedMissionSchedules]') 
                    AND name = N'IX_SavedMissionSchedules_SavedMissionId_IsEnabled'
                )
                BEGIN
                    CREATE INDEX [IX_SavedMissionSchedules_SavedMissionId_IsEnabled] ON [SavedMissionSchedules] ([SavedMissionId], [IsEnabled])
                END";
            migrationBuilder.Sql(createIXSavedMissionSchedulesSavedMissionIdIsEnabledSql);

            // Using raw SQL to create the index only if it doesn't exist
            var createIXShelfDecisionRulesActualValueSql = @"
                IF NOT EXISTS (
                    SELECT name FROM sys.indexes 
                    WHERE object_id = OBJECT_ID(N'[ShelfDecisionRules]') 
                    AND name = N'IX_ShelfDecisionRules_ActualValue'
                )
                BEGIN
                    CREATE UNIQUE INDEX [IX_ShelfDecisionRules_ActualValue] ON [ShelfDecisionRules] ([ActualValue])
                END";
            migrationBuilder.Sql(createIXShelfDecisionRulesActualValueSql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Areas");

            migrationBuilder.DropTable(
                name: "MissionTypes");

            migrationBuilder.DropTable(
                name: "MobileRobots");

            migrationBuilder.DropTable(
                name: "ResumeStrategies");

            migrationBuilder.DropTable(
                name: "RobotManualPauses");

            migrationBuilder.DropTable(
                name: "RobotTypes");

            migrationBuilder.DropTable(
                name: "SavedMissionScheduleLogs");

            migrationBuilder.DropTable(
                name: "ShelfDecisionRules");

            migrationBuilder.DropTable(
                name: "SavedMissionSchedules");

            migrationBuilder.DropTable(
                name: "SavedCustomMissions");

            // Using raw SQL to safely drop the index only if it exists
            var dropIndexSql1 = @"
                IF EXISTS (
                    SELECT name FROM sys.indexes 
                    WHERE object_id = OBJECT_ID(N'[MissionQueues]') 
                    AND name = N'IX_MissionQueues_AssignedRobotId_CompletedDate'
                )
                BEGIN
                    DROP INDEX [IX_MissionQueues_AssignedRobotId_CompletedDate] ON [MissionQueues]
                END";
            migrationBuilder.Sql(dropIndexSql1);

            migrationBuilder.DropIndex(
                name: "IX_MissionQueues_SavedMissionId",
                table: "MissionQueues");

            migrationBuilder.DropIndex(
                name: "IX_MissionQueues_TriggerSource_CompletedDate",
                table: "MissionQueues");

            // Using raw SQL to safely drop the index only if it exists
            var dropIndexSql2 = @"
                IF EXISTS (
                    SELECT name FROM sys.indexes 
                    WHERE object_id = OBJECT_ID(N'[MissionHistories]') 
                    AND name = N'IX_MissionHistories_AssignedRobotId_CompletedDate'
                )
                BEGIN
                    DROP INDEX [IX_MissionHistories_AssignedRobotId_CompletedDate] ON [MissionHistories]
                END";
            migrationBuilder.Sql(dropIndexSql2);

            migrationBuilder.DropIndex(
                name: "IX_MissionHistories_SavedMissionId",
                table: "MissionHistories");

            migrationBuilder.DropIndex(
                name: "IX_MissionHistories_TriggerSource_CompletedDate",
                table: "MissionHistories");

            // Using raw SQL to safely drop the column only if it exists
            var dropColumnSql1 = @"
                IF EXISTS (
                    SELECT * FROM sys.columns 
                    WHERE object_id = OBJECT_ID(N'[MissionQueues]') 
                    AND name = N'AssignedRobotId'
                )
                BEGIN
                    ALTER TABLE [MissionQueues] DROP COLUMN [AssignedRobotId]
                END";
            migrationBuilder.Sql(dropColumnSql1);

            migrationBuilder.DropColumn(
                name: "ContainerCode",
                table: "MissionQueues");

            migrationBuilder.DropColumn(
                name: "ContainerModelCode",
                table: "MissionQueues");

            migrationBuilder.DropColumn(
                name: "CurrentManualWaypointPosition",
                table: "MissionQueues");

            migrationBuilder.DropColumn(
                name: "IdleNode",
                table: "MissionQueues");

            migrationBuilder.DropColumn(
                name: "IsWaitingForManualResume",
                table: "MissionQueues");

            migrationBuilder.DropColumn(
                name: "LastRobotQueryTime",
                table: "MissionQueues");

            migrationBuilder.DropColumn(
                name: "LockRobotAfterFinish",
                table: "MissionQueues");

            migrationBuilder.DropColumn(
                name: "ManualWaypointsJson",
                table: "MissionQueues");

            migrationBuilder.DropColumn(
                name: "MissionDataJson",
                table: "MissionQueues");

            migrationBuilder.DropColumn(
                name: "MissionType",
                table: "MissionQueues");

            migrationBuilder.DropColumn(
                name: "RobotBatteryLevel",
                table: "MissionQueues");

            migrationBuilder.DropColumn(
                name: "RobotIds",
                table: "MissionQueues");

            migrationBuilder.DropColumn(
                name: "RobotModels",
                table: "MissionQueues");

            migrationBuilder.DropColumn(
                name: "RobotNodeCode",
                table: "MissionQueues");

            migrationBuilder.DropColumn(
                name: "RobotStatusCode",
                table: "MissionQueues");

            migrationBuilder.DropColumn(
                name: "RobotType",
                table: "MissionQueues");

            migrationBuilder.DropColumn(
                name: "SavedMissionId",
                table: "MissionQueues");

            migrationBuilder.DropColumn(
                name: "TriggerSource",
                table: "MissionQueues");

            migrationBuilder.DropColumn(
                name: "UnlockMissionCode",
                table: "MissionQueues");

            migrationBuilder.DropColumn(
                name: "UnlockRobotId",
                table: "MissionQueues");

            migrationBuilder.DropColumn(
                name: "ViewBoardType",
                table: "MissionQueues");

            migrationBuilder.DropColumn(
                name: "VisitedManualWaypointsJson",
                table: "MissionQueues");

            // Using raw SQL to safely drop the column only if it exists
            var dropColumnSql2 = @"
                IF EXISTS (
                    SELECT * FROM sys.columns 
                    WHERE object_id = OBJECT_ID(N'[MissionHistories]') 
                    AND name = N'AssignedRobotId'
                )
                BEGIN
                    ALTER TABLE [MissionHistories] DROP COLUMN [AssignedRobotId]
                END";
            migrationBuilder.Sql(dropColumnSql2);

            migrationBuilder.DropColumn(
                name: "CompletedDate",
                table: "MissionHistories");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "MissionHistories");

            migrationBuilder.DropColumn(
                name: "ErrorMessage",
                table: "MissionHistories");

            migrationBuilder.DropColumn(
                name: "MissionType",
                table: "MissionHistories");

            migrationBuilder.DropColumn(
                name: "ProcessedDate",
                table: "MissionHistories");

            migrationBuilder.DropColumn(
                name: "SavedMissionId",
                table: "MissionHistories");

            migrationBuilder.DropColumn(
                name: "SubmittedToAmrDate",
                table: "MissionHistories");

            migrationBuilder.DropColumn(
                name: "TriggerSource",
                table: "MissionHistories");

            migrationBuilder.DropColumn(
                name: "WorkflowId",
                table: "MissionHistories");

            migrationBuilder.AlterColumn<string>(
                name: "WorkflowName",
                table: "MissionQueues",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "WorkflowId",
                table: "MissionQueues",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "WorkflowCode",
                table: "MissionQueues",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TemplateCode",
                table: "MissionQueues",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "WorkflowName",
                table: "MissionHistories",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);
        }
    }
}
