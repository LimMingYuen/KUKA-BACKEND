IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

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
GO

CREATE INDEX [IX_RobotManualPauses_MissionCode_PauseStartUtc] ON [RobotManualPauses] ([MissionCode], [PauseStartUtc]);
GO

CREATE INDEX [IX_RobotManualPauses_RobotId_PauseStartUtc] ON [RobotManualPauses] ([RobotId], [PauseStartUtc]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250205090000_AddRobotManualPauseTable', N'8.0.5');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [SavedMissionSchedules] (
    [Id] int NOT NULL IDENTITY,
    [SavedMissionId] int NOT NULL,
    [TriggerType] int NOT NULL,
    [CronExpression] nvarchar(120) NULL,
    [OneTimeRunUtc] datetime2 NULL,
    [TimezoneId] nvarchar(100) NOT NULL DEFAULT N'UTC',
    [IsEnabled] bit NOT NULL DEFAULT CAST(1 AS bit),
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
GO

CREATE TABLE [SavedMissionScheduleLogs] (
    [Id] int NOT NULL IDENTITY,
    [ScheduleId] int NOT NULL,
    [ScheduledForUtc] datetime2 NOT NULL,
    [EnqueuedUtc] datetime2 NULL,
    [QueueId] int NULL,
    [ResultStatus] nvarchar(30) NOT NULL DEFAULT N'Pending',
    [Error] nvarchar(500) NULL,
    [CreatedUtc] datetime2 NOT NULL,
    CONSTRAINT [PK_SavedMissionScheduleLogs] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SavedMissionScheduleLogs_SavedMissionSchedules_ScheduleId] FOREIGN KEY ([ScheduleId]) REFERENCES [SavedMissionSchedules] ([Id]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_SavedMissionScheduleLogs_ScheduleId] ON [SavedMissionScheduleLogs] ([ScheduleId]);
GO

CREATE INDEX [IX_SavedMissionSchedules_NextRunUtc] ON [SavedMissionSchedules] ([NextRunUtc]);
GO

CREATE INDEX [IX_SavedMissionSchedules_SavedMissionId_IsEnabled] ON [SavedMissionSchedules] ([SavedMissionId], [IsEnabled]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250206090000_AddSavedMissionSchedules', N'8.0.5');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [WorkflowDiagrams] (
    [Id] int NOT NULL IDENTITY,
    [WorkflowCode] nvarchar(64) NOT NULL,
    [WorkflowOuterCode] nvarchar(64) NOT NULL,
    [WorkflowName] nvarchar(256) NOT NULL,
    [WorkflowModel] int NOT NULL,
    [RobotTypeClass] int NOT NULL,
    [MapCode] nvarchar(128) NOT NULL,
    [ButtonName] nvarchar(128) NULL,
    [CreateUsername] nvarchar(128) NOT NULL,
    [CreateTime] datetime2 NOT NULL,
    [UpdateUsername] nvarchar(128) NOT NULL,
    [UpdateTime] datetime2 NOT NULL,
    [Status] int NOT NULL,
    [NeedConfirm] int NOT NULL,
    [LockRobotAfterFinish] int NOT NULL,
    [WorkflowPriority] int NOT NULL,
    [TargetAreaCode] nvarchar(64) NULL,
    [PreSelectedRobotCellCode] nvarchar(64) NULL,
    [PreSelectedRobotId] int NULL,
    CONSTRAINT [PK_WorkflowDiagrams] PRIMARY KEY ([Id])
);
GO

CREATE UNIQUE INDEX [IX_WorkflowDiagrams_WorkflowCode] ON [WorkflowDiagrams] ([WorkflowCode]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251011142411_CreateWorkflowTable', N'8.0.5');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [MissionHistories] (
    [Id] int NOT NULL IDENTITY,
    [MissionCode] nvarchar(100) NOT NULL,
    [RequestId] nvarchar(100) NOT NULL,
    [WorkflowName] nvarchar(200) NOT NULL,
    [Status] nvarchar(50) NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_MissionHistories] PRIMARY KEY ([Id])
);
GO

CREATE INDEX [IX_MissionHistories_MissionCode] ON [MissionHistories] ([MissionCode]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251020115331_AddMissionHistoryTable', N'8.0.5');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [MissionQueues] (
    [Id] int NOT NULL IDENTITY,
    [WorkflowId] int NOT NULL,
    [WorkflowCode] nvarchar(50) NOT NULL,
    [WorkflowName] nvarchar(200) NOT NULL,
    [MissionCode] nvarchar(100) NOT NULL,
    [TemplateCode] nvarchar(100) NOT NULL,
    [Priority] int NOT NULL,
    [RequestId] nvarchar(100) NOT NULL,
    [Status] int NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [ProcessedDate] datetime2 NULL,
    [CompletedDate] datetime2 NULL,
    [ErrorMessage] nvarchar(500) NULL,
    [CreatedBy] nvarchar(100) NOT NULL,
    CONSTRAINT [PK_MissionQueues] PRIMARY KEY ([Id])
);
GO

CREATE UNIQUE INDEX [IX_MissionQueues_MissionCode] ON [MissionQueues] ([MissionCode]);
GO

CREATE INDEX [IX_MissionQueues_Status_Priority_CreatedDate] ON [MissionQueues] ([Status], [Priority], [CreatedDate]);
GO

CREATE INDEX [IX_MissionQueues_WorkflowId] ON [MissionQueues] ([WorkflowId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251021021028_AddMissionQueueTable', N'8.0.5');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [MissionQueues] ADD [AmrSubmissionError] nvarchar(500) NULL;
GO

ALTER TABLE [MissionQueues] ADD [SubmittedToAmr] bit NOT NULL DEFAULT CAST(0 AS bit);
GO

ALTER TABLE [MissionQueues] ADD [SubmittedToAmrDate] datetime2 NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251021065459_AddAmrSubmissionTracking', N'8.0.5');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [QrCodes] (
    [Id] int NOT NULL IDENTITY,
    [CreateTime] datetime2 NOT NULL,
    [CreateBy] nvarchar(128) NOT NULL,
    [CreateApp] nvarchar(128) NOT NULL,
    [LastUpdateTime] datetime2 NOT NULL,
    [LastUpdateBy] nvarchar(128) NOT NULL,
    [LastUpdateApp] nvarchar(128) NOT NULL,
    [NodeLabel] nvarchar(64) NOT NULL,
    [Reliability] int NOT NULL,
    [MapCode] nvarchar(128) NOT NULL,
    [FloorNumber] nvarchar(16) NOT NULL,
    [NodeNumber] int NOT NULL,
    [ReportTimes] int NOT NULL,
    CONSTRAINT [PK_QrCodes] PRIMARY KEY ([Id])
);
GO

CREATE INDEX [IX_QrCodes_MapCode] ON [QrCodes] ([MapCode]);
GO

CREATE UNIQUE INDEX [IX_QrCodes_NodeLabel_MapCode] ON [QrCodes] ([NodeLabel], [MapCode]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251022144934_AddQrCodeTable', N'8.0.5');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [MapZones] (
    [Id] int NOT NULL IDENTITY,
    [CreateTime] datetime2 NOT NULL,
    [CreateBy] nvarchar(128) NOT NULL,
    [CreateApp] nvarchar(128) NOT NULL,
    [LastUpdateTime] datetime2 NOT NULL,
    [LastUpdateBy] nvarchar(128) NOT NULL,
    [LastUpdateApp] nvarchar(128) NOT NULL,
    [ZoneName] nvarchar(256) NOT NULL,
    [ZoneCode] nvarchar(256) NOT NULL,
    [ZoneDescription] nvarchar(1000) NOT NULL,
    [ZoneColor] nvarchar(64) NOT NULL,
    [MapCode] nvarchar(128) NOT NULL,
    [FloorNumber] nvarchar(16) NOT NULL,
    [Points] nvarchar(max) NOT NULL,
    [Nodes] nvarchar(max) NOT NULL,
    [Edges] nvarchar(max) NOT NULL,
    [CustomerUi] nvarchar(max) NOT NULL,
    [ZoneType] nvarchar(16) NOT NULL,
    [Status] int NOT NULL,
    [BeginTime] datetime2 NULL,
    [EndTime] datetime2 NULL,
    [Configs] nvarchar(max) NULL,
    CONSTRAINT [PK_MapZones] PRIMARY KEY ([Id])
);
GO

CREATE INDEX [IX_MapZones_MapCode] ON [MapZones] ([MapCode]);
GO

CREATE UNIQUE INDEX [IX_MapZones_ZoneCode] ON [MapZones] ([ZoneCode]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251022153839_AddMapZoneTable', N'8.0.5');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

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
GO

CREATE UNIQUE INDEX [IX_MissionTypes_ActualValue] ON [MissionTypes] ([ActualValue]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251024163710_AddMissionTypeTable', N'8.0.5');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

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
GO

CREATE UNIQUE INDEX [IX_RobotTypes_ActualValue] ON [RobotTypes] ([ActualValue]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251025045107_AddRobotTypesTable', N'8.0.5');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

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
GO

CREATE UNIQUE INDEX [IX_ShelfDecisionRules_ActualValue] ON [ShelfDecisionRules] ([ActualValue]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251025090127_AddShelfDecisionRulesTable', N'8.0.5');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

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
GO

CREATE UNIQUE INDEX [IX_ResumeStrategies_ActualValue] ON [ResumeStrategies] ([ActualValue]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251025120241_AddResumeStrategiesTable', N'8.0.5');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

DECLARE @var0 sysname;
SELECT @var0 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[MissionQueues]') AND [c].[name] = N'WorkflowName');
IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [MissionQueues] DROP CONSTRAINT [' + @var0 + '];');
ALTER TABLE [MissionQueues] ALTER COLUMN [WorkflowName] nvarchar(200) NULL;
GO

DECLARE @var1 sysname;
SELECT @var1 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[MissionQueues]') AND [c].[name] = N'WorkflowId');
IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [MissionQueues] DROP CONSTRAINT [' + @var1 + '];');
ALTER TABLE [MissionQueues] ALTER COLUMN [WorkflowId] int NULL;
GO

DECLARE @var2 sysname;
SELECT @var2 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[MissionQueues]') AND [c].[name] = N'WorkflowCode');
IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [MissionQueues] DROP CONSTRAINT [' + @var2 + '];');
ALTER TABLE [MissionQueues] ALTER COLUMN [WorkflowCode] nvarchar(50) NULL;
GO

DECLARE @var3 sysname;
SELECT @var3 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[MissionQueues]') AND [c].[name] = N'TemplateCode');
IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [MissionQueues] DROP CONSTRAINT [' + @var3 + '];');
ALTER TABLE [MissionQueues] ALTER COLUMN [TemplateCode] nvarchar(100) NULL;
GO

ALTER TABLE [MissionQueues] ADD [ContainerCode] nvarchar(100) NULL;
GO

ALTER TABLE [MissionQueues] ADD [ContainerModelCode] nvarchar(100) NULL;
GO

ALTER TABLE [MissionQueues] ADD [IdleNode] nvarchar(100) NULL;
GO

ALTER TABLE [MissionQueues] ADD [LockRobotAfterFinish] bit NOT NULL DEFAULT CAST(0 AS bit);
GO

ALTER TABLE [MissionQueues] ADD [MissionDataJson] nvarchar(max) NULL;
GO

ALTER TABLE [MissionQueues] ADD [MissionType] nvarchar(50) NULL;
GO

ALTER TABLE [MissionQueues] ADD [RobotIds] nvarchar(500) NULL;
GO

ALTER TABLE [MissionQueues] ADD [RobotModels] nvarchar(500) NULL;
GO

ALTER TABLE [MissionQueues] ADD [RobotType] nvarchar(50) NULL;
GO

ALTER TABLE [MissionQueues] ADD [UnlockMissionCode] nvarchar(100) NULL;
GO

ALTER TABLE [MissionQueues] ADD [UnlockRobotId] nvarchar(50) NULL;
GO

ALTER TABLE [MissionQueues] ADD [ViewBoardType] nvarchar(50) NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251025152143_AddCustomMissionSupport', N'8.0.5');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [PositionTypes] (
    [Id] int NOT NULL IDENTITY,
    [DisplayName] nvarchar(128) NOT NULL,
    [ActualValue] nvarchar(128) NOT NULL,
    [Description] nvarchar(512) NULL,
    [IsActive] bit NOT NULL,
    [CreatedUtc] datetime2 NOT NULL,
    [UpdatedUtc] datetime2 NOT NULL,
    CONSTRAINT [PK_PositionTypes] PRIMARY KEY ([Id])
);
GO

CREATE UNIQUE INDEX [IX_PositionTypes_ActualValue] ON [PositionTypes] ([ActualValue]);
GO

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'DisplayName', N'ActualValue', N'Description', N'IsActive', N'CreatedUtc', N'UpdatedUtc') AND [object_id] = OBJECT_ID(N'[PositionTypes]'))
    SET IDENTITY_INSERT [PositionTypes] ON;
INSERT INTO [PositionTypes] ([DisplayName], [ActualValue], [Description], [IsActive], [CreatedUtc], [UpdatedUtc])
VALUES (N'Node Point', N'NODE_POINT', N'Standard navigation node point', CAST(1 AS bit), '2025-11-04T05:43:10.1387344Z', '2025-11-04T05:43:10.1387350Z'),
(N'Area', N'AREA', N'Designated area or zone', CAST(1 AS bit), '2025-11-04T05:43:10.1387352Z', '2025-11-04T05:43:10.1387352Z'),
(N'Charging Point', N'CHARGING_POINT', N'Robot charging station', CAST(1 AS bit), '2025-11-04T05:43:10.1387355Z', '2025-11-04T05:43:10.1387356Z'),
(N'Parking Point', N'PARKING_POINT', N'Robot parking location', CAST(1 AS bit), '2025-11-04T05:43:10.1387357Z', '2025-11-04T05:43:10.1387357Z');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'DisplayName', N'ActualValue', N'Description', N'IsActive', N'CreatedUtc', N'UpdatedUtc') AND [object_id] = OBJECT_ID(N'[PositionTypes]'))
    SET IDENTITY_INSERT [PositionTypes] OFF;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251025160421_AddPositionTypeConfiguration', N'8.0.5');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

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
GO

CREATE INDEX [IX_SavedCustomMissions_CreatedBy_IsDeleted] ON [SavedCustomMissions] ([CreatedBy], [IsDeleted]);
GO

CREATE INDEX [IX_SavedCustomMissions_MissionName] ON [SavedCustomMissions] ([MissionName]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251025162237_AddSavedCustomMissionsTable', N'8.0.5');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

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
GO

CREATE UNIQUE INDEX [IX_Areas_ActualValue] ON [Areas] ([ActualValue]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251025164750_AddAreasTable', N'8.0.5');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

DROP TABLE [PositionTypes];
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251025174239_RemovePositionTypesTable', N'8.0.5');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

DECLARE @var4 sysname;
SELECT @var4 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[MissionHistories]') AND [c].[name] = N'WorkflowName');
IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [MissionHistories] DROP CONSTRAINT [' + @var4 + '];');
ALTER TABLE [MissionHistories] ALTER COLUMN [WorkflowName] nvarchar(200) NULL;
GO

ALTER TABLE [MissionHistories] ADD [CompletedDate] datetime2 NULL;
GO

ALTER TABLE [MissionHistories] ADD [CreatedBy] nvarchar(100) NULL;
GO

ALTER TABLE [MissionHistories] ADD [ErrorMessage] nvarchar(500) NULL;
GO

ALTER TABLE [MissionHistories] ADD [MissionType] nvarchar(50) NULL;
GO

ALTER TABLE [MissionHistories] ADD [WorkflowId] int NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251025192004_EnhanceMissionHistoryTracking', N'8.0.5');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [MissionQueues] ADD [AssignedRobotId] nvarchar(50) NULL;
GO

ALTER TABLE [MissionQueues] ADD [LastRobotQueryTime] datetime2 NULL;
GO

ALTER TABLE [MissionQueues] ADD [RobotBatteryLevel] int NULL;
GO

ALTER TABLE [MissionQueues] ADD [RobotNodeCode] nvarchar(100) NULL;
GO

ALTER TABLE [MissionQueues] ADD [RobotStatusCode] int NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251027160245_AddRobotTrackingFields', N'8.0.5');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [MissionQueues] ADD [CurrentManualWaypointPosition] nvarchar(100) NULL;
GO

ALTER TABLE [MissionQueues] ADD [IsWaitingForManualResume] bit NOT NULL DEFAULT CAST(0 AS bit);
GO

ALTER TABLE [MissionQueues] ADD [ManualWaypointsJson] nvarchar(max) NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251028011042_AddManualWaypointHandling', N'8.0.5');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [MissionQueues] ADD [VisitedManualWaypointsJson] nvarchar(max) NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251028063302_AddVisitedManualWaypointsToMissionQueue', N'8.0.5');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

DECLARE @var5 sysname;
SELECT @var5 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[SavedMissionSchedules]') AND [c].[name] = N'TimezoneId');
IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [SavedMissionSchedules] DROP CONSTRAINT [' + @var5 + '];');
UPDATE [SavedMissionSchedules] SET [TimezoneId] = N'' WHERE [TimezoneId] IS NULL;
ALTER TABLE [SavedMissionSchedules] ALTER COLUMN [TimezoneId] nvarchar(100) NOT NULL;
ALTER TABLE [SavedMissionSchedules] ADD DEFAULT N'' FOR [TimezoneId];
GO

DECLARE @var6 sysname;
SELECT @var6 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[SavedMissionSchedules]') AND [c].[name] = N'IsEnabled');
IF @var6 IS NOT NULL EXEC(N'ALTER TABLE [SavedMissionSchedules] DROP CONSTRAINT [' + @var6 + '];');
GO

DECLARE @var7 sysname;
SELECT @var7 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[SavedMissionScheduleLogs]') AND [c].[name] = N'ResultStatus');
IF @var7 IS NOT NULL EXEC(N'ALTER TABLE [SavedMissionScheduleLogs] DROP CONSTRAINT [' + @var7 + '];');
UPDATE [SavedMissionScheduleLogs] SET [ResultStatus] = N'' WHERE [ResultStatus] IS NULL;
ALTER TABLE [SavedMissionScheduleLogs] ALTER COLUMN [ResultStatus] nvarchar(30) NOT NULL;
ALTER TABLE [SavedMissionScheduleLogs] ADD DEFAULT N'' FOR [ResultStatus];
GO

ALTER TABLE [MissionQueues] ADD [SavedMissionId] int NULL;
GO

ALTER TABLE [MissionQueues] ADD [TriggerSource] int NOT NULL DEFAULT 0;
GO

CREATE INDEX [IX_MissionQueues_AssignedRobotId_CompletedDate] ON [MissionQueues] ([AssignedRobotId], [CompletedDate]);
GO

CREATE INDEX [IX_MissionQueues_SavedMissionId] ON [MissionQueues] ([SavedMissionId]);
GO

CREATE INDEX [IX_MissionQueues_TriggerSource_CompletedDate] ON [MissionQueues] ([TriggerSource], [CompletedDate]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251030152301_AddMissionSourceTracking', N'8.0.5');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [MissionHistories] ADD [AssignedRobotId] nvarchar(50) NULL;
GO

ALTER TABLE [MissionHistories] ADD [ProcessedDate] datetime2 NULL;
GO

ALTER TABLE [MissionHistories] ADD [SavedMissionId] int NULL;
GO

ALTER TABLE [MissionHistories] ADD [SubmittedToAmrDate] datetime2 NULL;
GO

ALTER TABLE [MissionHistories] ADD [TriggerSource] int NOT NULL DEFAULT 0;
GO

CREATE INDEX [IX_MissionHistories_AssignedRobotId_CompletedDate] ON [MissionHistories] ([AssignedRobotId], [CompletedDate]);
GO

CREATE INDEX [IX_MissionHistories_SavedMissionId] ON [MissionHistories] ([SavedMissionId]);
GO

CREATE INDEX [IX_MissionHistories_TriggerSource_CompletedDate] ON [MissionHistories] ([TriggerSource], [CompletedDate]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251030160204_AddTrackingFieldsToMissionHistory', N'8.0.5');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

DECLARE @var8 sysname;
SELECT @var8 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[MissionQueues]') AND [c].[name] = N'WorkflowName');
IF @var8 IS NOT NULL EXEC(N'ALTER TABLE [MissionQueues] DROP CONSTRAINT [' + @var8 + '];');
ALTER TABLE [MissionQueues] ALTER COLUMN [WorkflowName] nvarchar(200) NULL;
GO

DECLARE @var9 sysname;
SELECT @var9 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[MissionQueues]') AND [c].[name] = N'WorkflowId');
IF @var9 IS NOT NULL EXEC(N'ALTER TABLE [MissionQueues] DROP CONSTRAINT [' + @var9 + '];');
ALTER TABLE [MissionQueues] ALTER COLUMN [WorkflowId] int NULL;
GO

DECLARE @var10 sysname;
SELECT @var10 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[MissionQueues]') AND [c].[name] = N'WorkflowCode');
IF @var10 IS NOT NULL EXEC(N'ALTER TABLE [MissionQueues] DROP CONSTRAINT [' + @var10 + '];');
ALTER TABLE [MissionQueues] ALTER COLUMN [WorkflowCode] nvarchar(50) NULL;
GO

DECLARE @var11 sysname;
SELECT @var11 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[MissionQueues]') AND [c].[name] = N'TemplateCode');
IF @var11 IS NOT NULL EXEC(N'ALTER TABLE [MissionQueues] DROP CONSTRAINT [' + @var11 + '];');
ALTER TABLE [MissionQueues] ALTER COLUMN [TemplateCode] nvarchar(100) NULL;
GO

ALTER TABLE [MissionQueues] ADD [AssignedRobotId] nvarchar(50) NULL;
GO

ALTER TABLE [MissionQueues] ADD [ContainerCode] nvarchar(100) NULL;
GO

ALTER TABLE [MissionQueues] ADD [ContainerModelCode] nvarchar(100) NULL;
GO

ALTER TABLE [MissionQueues] ADD [CurrentManualWaypointPosition] nvarchar(100) NULL;
GO

ALTER TABLE [MissionQueues] ADD [IdleNode] nvarchar(100) NULL;
GO

ALTER TABLE [MissionQueues] ADD [IsWaitingForManualResume] bit NOT NULL DEFAULT CAST(0 AS bit);
GO

ALTER TABLE [MissionQueues] ADD [LastRobotQueryTime] datetime2 NULL;
GO

ALTER TABLE [MissionQueues] ADD [LockRobotAfterFinish] bit NOT NULL DEFAULT CAST(0 AS bit);
GO

ALTER TABLE [MissionQueues] ADD [ManualWaypointsJson] nvarchar(max) NULL;
GO

ALTER TABLE [MissionQueues] ADD [MissionDataJson] nvarchar(max) NULL;
GO

ALTER TABLE [MissionQueues] ADD [MissionType] nvarchar(50) NULL;
GO

ALTER TABLE [MissionQueues] ADD [RobotBatteryLevel] int NULL;
GO

ALTER TABLE [MissionQueues] ADD [RobotIds] nvarchar(500) NULL;
GO

ALTER TABLE [MissionQueues] ADD [RobotModels] nvarchar(500) NULL;
GO

ALTER TABLE [MissionQueues] ADD [RobotNodeCode] nvarchar(100) NULL;
GO

ALTER TABLE [MissionQueues] ADD [RobotStatusCode] int NULL;
GO

ALTER TABLE [MissionQueues] ADD [RobotType] nvarchar(50) NULL;
GO

ALTER TABLE [MissionQueues] ADD [SavedMissionId] int NULL;
GO

ALTER TABLE [MissionQueues] ADD [TriggerSource] int NOT NULL DEFAULT 0;
GO

ALTER TABLE [MissionQueues] ADD [UnlockMissionCode] nvarchar(100) NULL;
GO

ALTER TABLE [MissionQueues] ADD [UnlockRobotId] nvarchar(50) NULL;
GO

ALTER TABLE [MissionQueues] ADD [ViewBoardType] nvarchar(50) NULL;
GO

ALTER TABLE [MissionQueues] ADD [VisitedManualWaypointsJson] nvarchar(max) NULL;
GO

DECLARE @var12 sysname;
SELECT @var12 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[MissionHistories]') AND [c].[name] = N'WorkflowName');
IF @var12 IS NOT NULL EXEC(N'ALTER TABLE [MissionHistories] DROP CONSTRAINT [' + @var12 + '];');
ALTER TABLE [MissionHistories] ALTER COLUMN [WorkflowName] nvarchar(200) NULL;
GO

ALTER TABLE [MissionHistories] ADD [AssignedRobotId] nvarchar(50) NULL;
GO

ALTER TABLE [MissionHistories] ADD [CompletedDate] datetime2 NULL;
GO

ALTER TABLE [MissionHistories] ADD [CreatedBy] nvarchar(100) NULL;
GO

ALTER TABLE [MissionHistories] ADD [ErrorMessage] nvarchar(500) NULL;
GO

ALTER TABLE [MissionHistories] ADD [MissionType] nvarchar(50) NULL;
GO

ALTER TABLE [MissionHistories] ADD [ProcessedDate] datetime2 NULL;
GO

ALTER TABLE [MissionHistories] ADD [SavedMissionId] int NULL;
GO

ALTER TABLE [MissionHistories] ADD [SubmittedToAmrDate] datetime2 NULL;
GO

ALTER TABLE [MissionHistories] ADD [TriggerSource] int NOT NULL DEFAULT 0;
GO

ALTER TABLE [MissionHistories] ADD [WorkflowId] int NULL;
GO

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
GO

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
GO

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
GO

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
GO

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
GO

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
GO

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
GO

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
GO

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
GO

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
GO

CREATE INDEX [IX_MissionQueues_AssignedRobotId_CompletedDate] ON [MissionQueues] ([AssignedRobotId], [CompletedDate]);
GO

CREATE INDEX [IX_MissionQueues_SavedMissionId] ON [MissionQueues] ([SavedMissionId]);
GO

CREATE INDEX [IX_MissionQueues_TriggerSource_CompletedDate] ON [MissionQueues] ([TriggerSource], [CompletedDate]);
GO

CREATE INDEX [IX_MissionHistories_AssignedRobotId_CompletedDate] ON [MissionHistories] ([AssignedRobotId], [CompletedDate]);
GO

CREATE INDEX [IX_MissionHistories_SavedMissionId] ON [MissionHistories] ([SavedMissionId]);
GO

CREATE INDEX [IX_MissionHistories_TriggerSource_CompletedDate] ON [MissionHistories] ([TriggerSource], [CompletedDate]);
GO

CREATE UNIQUE INDEX [IX_Areas_ActualValue] ON [Areas] ([ActualValue]);
GO

CREATE UNIQUE INDEX [IX_MissionTypes_ActualValue] ON [MissionTypes] ([ActualValue]);
GO

CREATE UNIQUE INDEX [IX_ResumeStrategies_ActualValue] ON [ResumeStrategies] ([ActualValue]);
GO

CREATE INDEX [IX_RobotManualPauses_MissionCode_PauseStartUtc] ON [RobotManualPauses] ([MissionCode], [PauseStartUtc]);
GO

CREATE INDEX [IX_RobotManualPauses_RobotId_PauseStartUtc] ON [RobotManualPauses] ([RobotId], [PauseStartUtc]);
GO

CREATE UNIQUE INDEX [IX_RobotTypes_ActualValue] ON [RobotTypes] ([ActualValue]);
GO

CREATE INDEX [IX_SavedCustomMissions_CreatedBy_IsDeleted] ON [SavedCustomMissions] ([CreatedBy], [IsDeleted]);
GO

CREATE INDEX [IX_SavedCustomMissions_MissionName] ON [SavedCustomMissions] ([MissionName]);
GO

CREATE INDEX [IX_SavedMissionScheduleLogs_ScheduleId] ON [SavedMissionScheduleLogs] ([ScheduleId]);
GO

CREATE INDEX [IX_SavedMissionSchedules_NextRunUtc] ON [SavedMissionSchedules] ([NextRunUtc]);
GO

CREATE INDEX [IX_SavedMissionSchedules_SavedMissionId_IsEnabled] ON [SavedMissionSchedules] ([SavedMissionId], [IsEnabled]);
GO

CREATE UNIQUE INDEX [IX_ShelfDecisionRules_ActualValue] ON [ShelfDecisionRules] ([ActualValue]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251101164122_AddMobileRobotTable', N'8.0.5');
GO

COMMIT;
GO

