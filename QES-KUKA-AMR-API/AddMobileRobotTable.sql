BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251101164122_AddMobileRobotTable'
)
BEGIN
    DECLARE @var0 sysname;
    SELECT @var0 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[MissionQueues]') AND [c].[name] = N'WorkflowName');
    IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [MissionQueues] DROP CONSTRAINT [' + @var0 + '];');
    ALTER TABLE [MissionQueues] ALTER COLUMN [WorkflowName] nvarchar(200) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251101164122_AddMobileRobotTable'
)
BEGIN
    DECLARE @var1 sysname;
    SELECT @var1 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[MissionQueues]') AND [c].[name] = N'WorkflowId');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [MissionQueues] DROP CONSTRAINT [' + @var1 + '];');
    ALTER TABLE [MissionQueues] ALTER COLUMN [WorkflowId] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251101164122_AddMobileRobotTable'
)
BEGIN
    DECLARE @var2 sysname;
    SELECT @var2 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[MissionQueues]') AND [c].[name] = N'WorkflowCode');
    IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [MissionQueues] DROP CONSTRAINT [' + @var2 + '];');
    ALTER TABLE [MissionQueues] ALTER COLUMN [WorkflowCode] nvarchar(50) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251101164122_AddMobileRobotTable'
)
BEGIN
    DECLARE @var3 sysname;
    SELECT @var3 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[MissionQueues]') AND [c].[name] = N'TemplateCode');
    IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [MissionQueues] DROP CONSTRAINT [' + @var3 + '];');
    ALTER TABLE [MissionQueues] ALTER COLUMN [TemplateCode] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251101164122_AddMobileRobotTable'
)
BEGIN
    ALTER TABLE [MissionQueues] ADD [AssignedRobotId] nvarchar(50) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251101164122_AddMobileRobotTable'
)
BEGIN
    ALTER TABLE [MissionQueues] ADD [ContainerCode] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251101164122_AddMobileRobotTable'
)
BEGIN
    ALTER TABLE [MissionQueues] ADD [ContainerModelCode] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251101164122_AddMobileRobotTable'
)
BEGIN
    ALTER TABLE [MissionQueues] ADD [CurrentManualWaypointPosition] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251101164122_AddMobileRobotTable'
)
BEGIN
    ALTER TABLE [MissionQueues] ADD [IdleNode] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251101164122_AddMobileRobotTable'
)
BEGIN
    ALTER TABLE [MissionQueues] ADD [IsWaitingForManualResume] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251101164122_AddMobileRobotTable'
)
BEGIN
    ALTER TABLE [MissionQueues] ADD [LastRobotQueryTime] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251101164122_AddMobileRobotTable'
)
BEGIN
    ALTER TABLE [MissionQueues] ADD [LockRobotAfterFinish] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251101164122_AddMobileRobotTable'
)
BEGIN
    ALTER TABLE [MissionQueues] ADD [ManualWaypointsJson] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251101164122_AddMobileRobotTable'
)
BEGIN
    ALTER TABLE [MissionQueues] ADD [MissionDataJson] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251101164122_AddMobileRobotTable'
)
BEGIN
    ALTER TABLE [MissionQueues] ADD [MissionType] nvarchar(50) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251101164122_AddMobileRobotTable'
)
BEGIN
    ALTER TABLE [MissionQueues] ADD [RobotBatteryLevel] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251101164122_AddMobileRobotTable'
)
BEGIN
    ALTER TABLE [MissionQueues] ADD [RobotIds] nvarchar(500) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251101164122_AddMobileRobotTable'
)
BEGIN
    ALTER TABLE [MissionQueues] ADD [RobotModels] nvarchar(500) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251101164122_AddMobileRobotTable'
)
BEGIN
    ALTER TABLE [MissionQueues] ADD [RobotNodeCode] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251101164122_AddMobileRobotTable'
)
BEGIN
    ALTER TABLE [MissionQueues] ADD [RobotStatusCode] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251101164122_AddMobileRobotTable'
)
BEGIN
    ALTER TABLE [MissionQueues] ADD [RobotType] nvarchar(50) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251101164122_AddMobileRobotTable'
)
BEGIN
    ALTER TABLE [MissionQueues] ADD [SavedMissionId] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251101164122_AddMobileRobotTable'
)
BEGIN
    ALTER TABLE [MissionQueues] ADD [TriggerSource] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251101164122_AddMobileRobotTable'
)
BEGIN
    ALTER TABLE [MissionQueues] ADD [UnlockMissionCode] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251101164122_AddMobileRobotTable'
)
BEGIN
    ALTER TABLE [MissionQueues] ADD [UnlockRobotId] nvarchar(50) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251101164122_AddMobileRobotTable'
)
BEGIN
    ALTER TABLE [MissionQueues] ADD [ViewBoardType] nvarchar(50) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251101164122_AddMobileRobotTable'
)
BEGIN
    ALTER TABLE [MissionQueues] ADD [VisitedManualWaypointsJson] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251101164122_AddMobileRobotTable'
)
BEGIN
    DECLARE @var4 sysname;
    SELECT @var4 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[MissionHistories]') AND [c].[name] = N'WorkflowName');
    IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [MissionHistories] DROP CONSTRAINT [' + @var4 + '];');
    ALTER TABLE [MissionHistories] ALTER COLUMN [WorkflowName] nvarchar(200) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251101164122_AddMobileRobotTable'
)
BEGIN
    ALTER TABLE [MissionHistories] ADD [AssignedRobotId] nvarchar(50) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251101164122_AddMobileRobotTable'
)
BEGIN
    ALTER TABLE [MissionHistories] ADD [CompletedDate] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251101164122_AddMobileRobotTable'
)
BEGIN
    ALTER TABLE [MissionHistories] ADD [CreatedBy] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251101164122_AddMobileRobotTable'
)
BEGIN
    ALTER TABLE [MissionHistories] ADD [ErrorMessage] nvarchar(500) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251101164122_AddMobileRobotTable'
)
BEGIN
    ALTER TABLE [MissionHistories] ADD [MissionType] nvarchar(50) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251101164122_AddMobileRobotTable'
)
BEGIN
    ALTER TABLE [MissionHistories] ADD [ProcessedDate] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251101164122_AddMobileRobotTable'
)
BEGIN
    ALTER TABLE [MissionHistories] ADD [SavedMissionId] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251101164122_AddMobileRobotTable'
)
BEGIN
    ALTER TABLE [MissionHistories] ADD [SubmittedToAmrDate] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251101164122_AddMobileRobotTable'
)
BEGIN
    ALTER TABLE [MissionHistories] ADD [TriggerSource] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251101164122_AddMobileRobotTable'
)
BEGIN
    ALTER TABLE [MissionHistories] ADD [WorkflowId] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251101164122_AddMobileRobotTable'
)
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
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251101164122_AddMobileRobotTable'
)
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
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251101164122_AddMobileRobotTable'
)
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
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251101164122_AddMobileRobotTable'
)
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
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251101164122_AddMobileRobotTable'
)
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
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251101164122_AddMobileRobotTable'
)
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
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251101164122_AddMobileRobotTable'
)
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
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251101164122_AddMobileRobotTable'
)
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
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251101164122_AddMobileRobotTable'
)
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
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251101164122_AddMobileRobotTable'
)
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
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251101164122_AddMobileRobotTable'
)
BEGIN
    CREATE INDEX [IX_MissionQueues_AssignedRobotId_CompletedDate] ON [MissionQueues] ([AssignedRobotId], [CompletedDate]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251101164122_AddMobileRobotTable'
)
BEGIN
    CREATE INDEX [IX_MissionQueues_SavedMissionId] ON [MissionQueues] ([SavedMissionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251101164122_AddMobileRobotTable'
)
BEGIN
    CREATE INDEX [IX_MissionQueues_TriggerSource_CompletedDate] ON [MissionQueues] ([TriggerSource], [CompletedDate]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251101164122_AddMobileRobotTable'
)
BEGIN
    CREATE INDEX [IX_MissionHistories_AssignedRobotId_CompletedDate] ON [MissionHistories] ([AssignedRobotId], [CompletedDate]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251101164122_AddMobileRobotTable'
)
BEGIN
    CREATE INDEX [IX_MissionHistories_SavedMissionId] ON [MissionHistories] ([SavedMissionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251101164122_AddMobileRobotTable'
)
BEGIN
    CREATE INDEX [IX_MissionHistories_TriggerSource_CompletedDate] ON [MissionHistories] ([TriggerSource], [CompletedDate]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251101164122_AddMobileRobotTable'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Areas_ActualValue] ON [Areas] ([ActualValue]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251101164122_AddMobileRobotTable'
)
BEGIN
    CREATE UNIQUE INDEX [IX_MissionTypes_ActualValue] ON [MissionTypes] ([ActualValue]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251101164122_AddMobileRobotTable'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ResumeStrategies_ActualValue] ON [ResumeStrategies] ([ActualValue]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251101164122_AddMobileRobotTable'
)
BEGIN
    CREATE INDEX [IX_RobotManualPauses_MissionCode_PauseStartUtc] ON [RobotManualPauses] ([MissionCode], [PauseStartUtc]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251101164122_AddMobileRobotTable'
)
BEGIN
    CREATE INDEX [IX_RobotManualPauses_RobotId_PauseStartUtc] ON [RobotManualPauses] ([RobotId], [PauseStartUtc]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251101164122_AddMobileRobotTable'
)
BEGIN
    CREATE UNIQUE INDEX [IX_RobotTypes_ActualValue] ON [RobotTypes] ([ActualValue]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251101164122_AddMobileRobotTable'
)
BEGIN
    CREATE INDEX [IX_SavedCustomMissions_CreatedBy_IsDeleted] ON [SavedCustomMissions] ([CreatedBy], [IsDeleted]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251101164122_AddMobileRobotTable'
)
BEGIN
    CREATE INDEX [IX_SavedCustomMissions_MissionName] ON [SavedCustomMissions] ([MissionName]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251101164122_AddMobileRobotTable'
)
BEGIN
    CREATE INDEX [IX_SavedMissionScheduleLogs_ScheduleId] ON [SavedMissionScheduleLogs] ([ScheduleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251101164122_AddMobileRobotTable'
)
BEGIN
    CREATE INDEX [IX_SavedMissionSchedules_NextRunUtc] ON [SavedMissionSchedules] ([NextRunUtc]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251101164122_AddMobileRobotTable'
)
BEGIN
    CREATE INDEX [IX_SavedMissionSchedules_SavedMissionId_IsEnabled] ON [SavedMissionSchedules] ([SavedMissionId], [IsEnabled]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251101164122_AddMobileRobotTable'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ShelfDecisionRules_ActualValue] ON [ShelfDecisionRules] ([ActualValue]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251101164122_AddMobileRobotTable'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251101164122_AddMobileRobotTable', N'8.0.5');
END;
GO

COMMIT;
GO

