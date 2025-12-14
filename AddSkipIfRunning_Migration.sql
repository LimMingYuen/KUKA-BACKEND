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

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251115075216_InitialCreate'
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
    WHERE [MigrationId] = N'20251115075216_InitialCreate'
)
BEGIN
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
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251115075216_InitialCreate'
)
BEGIN
    CREATE TABLE [MissionHistories] (
        [Id] int NOT NULL IDENTITY,
        [MissionCode] nvarchar(100) NOT NULL,
        [RequestId] nvarchar(100) NOT NULL,
        [WorkflowId] int NULL,
        [WorkflowName] nvarchar(200) NULL,
        [SavedMissionId] int NULL,
        [TriggerSource] int NOT NULL,
        [Status] nvarchar(50) NOT NULL,
        [MissionType] nvarchar(50) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [ProcessedDate] datetime2 NULL,
        [SubmittedToAmrDate] datetime2 NULL,
        [CompletedDate] datetime2 NULL,
        [AssignedRobotId] nvarchar(50) NULL,
        [ErrorMessage] nvarchar(500) NULL,
        [CreatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_MissionHistories] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251115075216_InitialCreate'
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
    WHERE [MigrationId] = N'20251115075216_InitialCreate'
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
        [BatteryLevel] float NOT NULL,
        [Mileage] float NOT NULL,
        [MissionCode] nvarchar(100) NOT NULL,
        [MeetObstacleStatus] int NOT NULL,
        [RobotOrientation] float NULL,
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
    WHERE [MigrationId] = N'20251115075216_InitialCreate'
)
BEGIN
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
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251115075216_InitialCreate'
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
    WHERE [MigrationId] = N'20251115075216_InitialCreate'
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
    WHERE [MigrationId] = N'20251115075216_InitialCreate'
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
    WHERE [MigrationId] = N'20251115075216_InitialCreate'
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
    WHERE [MigrationId] = N'20251115075216_InitialCreate'
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
    WHERE [MigrationId] = N'20251115075216_InitialCreate'
)
BEGIN
    CREATE TABLE [SystemSetting] (
        [Id] int NOT NULL IDENTITY,
        [Key] nvarchar(100) NOT NULL,
        [Value] nvarchar(255) NOT NULL,
        [Description] nvarchar(max) NULL,
        [LastUpdated] datetime2 NOT NULL,
        CONSTRAINT [PK_SystemSetting] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251115075216_InitialCreate'
)
BEGIN
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
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251115075216_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Areas_ActualValue] ON [Areas] ([ActualValue]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251115075216_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_MapZones_MapCode] ON [MapZones] ([MapCode]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251115075216_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_MapZones_ZoneCode] ON [MapZones] ([ZoneCode]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251115075216_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_MissionHistories_AssignedRobotId_CompletedDate] ON [MissionHistories] ([AssignedRobotId], [CompletedDate]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251115075216_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_MissionHistories_MissionCode] ON [MissionHistories] ([MissionCode]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251115075216_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_MissionHistories_SavedMissionId] ON [MissionHistories] ([SavedMissionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251115075216_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_MissionHistories_TriggerSource_CompletedDate] ON [MissionHistories] ([TriggerSource], [CompletedDate]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251115075216_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_MissionTypes_ActualValue] ON [MissionTypes] ([ActualValue]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251115075216_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_QrCodes_MapCode] ON [QrCodes] ([MapCode]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251115075216_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_QrCodes_NodeLabel_MapCode] ON [QrCodes] ([NodeLabel], [MapCode]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251115075216_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ResumeStrategies_ActualValue] ON [ResumeStrategies] ([ActualValue]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251115075216_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_RobotManualPauses_MissionCode_PauseStartUtc] ON [RobotManualPauses] ([MissionCode], [PauseStartUtc]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251115075216_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_RobotManualPauses_RobotId_PauseStartUtc] ON [RobotManualPauses] ([RobotId], [PauseStartUtc]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251115075216_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_RobotTypes_ActualValue] ON [RobotTypes] ([ActualValue]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251115075216_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_SavedCustomMissions_CreatedBy_IsDeleted] ON [SavedCustomMissions] ([CreatedBy], [IsDeleted]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251115075216_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_SavedCustomMissions_MissionName] ON [SavedCustomMissions] ([MissionName]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251115075216_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ShelfDecisionRules_ActualValue] ON [ShelfDecisionRules] ([ActualValue]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251115075216_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_SystemSetting_Key] ON [SystemSetting] ([Key]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251115075216_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_WorkflowDiagrams_WorkflowCode] ON [WorkflowDiagrams] ([WorkflowCode]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251115075216_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251115075216_InitialCreate', N'8.0.5');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251115080715_AddUserRole'
)
BEGIN
    CREATE TABLE [Users] (
        [Id] int NOT NULL IDENTITY,
        [Username] nvarchar(100) NOT NULL,
        [Nickname] nvarchar(100) NULL,
        [IsSuperAdmin] bit NOT NULL,
        [RolesJson] nvarchar(max) NULL,
        [CreateTime] datetime2 NOT NULL,
        [CreateBy] nvarchar(100) NULL,
        [CreateApp] nvarchar(100) NULL,
        [LastUpdateTime] datetime2 NOT NULL,
        [LastUpdateBy] nvarchar(100) NULL,
        [LastUpdateApp] nvarchar(100) NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251115080715_AddUserRole'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Users_Username] ON [Users] ([Username]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251115080715_AddUserRole'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251115080715_AddUserRole', N'8.0.5');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251115082427_AddUserAndRoleEntities'
)
BEGIN
    CREATE TABLE [Roles] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        [RoleCode] nvarchar(100) NOT NULL,
        [IsProtected] bit NOT NULL,
        [CreatedUtc] datetime2 NOT NULL,
        [UpdatedUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_Roles] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251115082427_AddUserAndRoleEntities'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Roles_RoleCode] ON [Roles] ([RoleCode]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251115082427_AddUserAndRoleEntities'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251115082427_AddUserAndRoleEntities', N'8.0.5');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251115121515_AddPasswordHashToUser'
)
BEGIN
    ALTER TABLE [Users] ADD [PasswordHash] nvarchar(255) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251115121515_AddPasswordHashToUser'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251115121515_AddPasswordHashToUser', N'8.0.5');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251115143457_AddExternalId'
)
BEGIN
    ALTER TABLE [WorkflowDiagrams] ADD [ExternalWorkflowId] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251115143457_AddExternalId'
)
BEGIN
    ALTER TABLE [QrCodes] ADD [ExternalQrCodeId] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251115143457_AddExternalId'
)
BEGIN
    ALTER TABLE [MobileRobots] ADD [ExternalMobileRobotId] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251115143457_AddExternalId'
)
BEGIN
    ALTER TABLE [MapZones] ADD [ExternalMapZoneId] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251115143457_AddExternalId'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251115143457_AddExternalId', N'8.0.5');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251115151343_AddWorkflowNodeCodeTable'
)
BEGIN
    CREATE TABLE [WorkflowNodeCodes] (
        [Id] int NOT NULL IDENTITY,
        [ExternalWorkflowId] int NOT NULL,
        [NodeCode] nvarchar(128) NOT NULL,
        [CreatedUtc] datetime2 NOT NULL,
        [UpdatedUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_WorkflowNodeCodes] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251115151343_AddWorkflowNodeCodeTable'
)
BEGIN
    CREATE INDEX [IX_WorkflowNodeCodes_ExternalWorkflowId] ON [WorkflowNodeCodes] ([ExternalWorkflowId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251115151343_AddWorkflowNodeCodeTable'
)
BEGIN
    CREATE UNIQUE INDEX [IX_WorkflowNodeCodes_ExternalWorkflowId_NodeCode] ON [WorkflowNodeCodes] ([ExternalWorkflowId], [NodeCode]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251115151343_AddWorkflowNodeCodeTable'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251115151343_AddWorkflowNodeCodeTable', N'8.0.5');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251116015144_AddWorkflowZoneMappingTable'
)
BEGIN
    CREATE TABLE [WorkflowZoneMappings] (
        [Id] int NOT NULL IDENTITY,
        [ExternalWorkflowId] int NOT NULL,
        [WorkflowCode] nvarchar(64) NOT NULL,
        [WorkflowName] nvarchar(256) NOT NULL,
        [ZoneName] nvarchar(256) NOT NULL,
        [ZoneCode] nvarchar(128) NOT NULL,
        [MapCode] nvarchar(128) NOT NULL,
        [MatchedNodesCount] int NOT NULL,
        [CreatedUtc] datetime2 NOT NULL,
        [UpdatedUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_WorkflowZoneMappings] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251116015144_AddWorkflowZoneMappingTable'
)
BEGIN
    CREATE UNIQUE INDEX [IX_WorkflowZoneMappings_ExternalWorkflowId] ON [WorkflowZoneMappings] ([ExternalWorkflowId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251116015144_AddWorkflowZoneMappingTable'
)
BEGIN
    CREATE INDEX [IX_WorkflowZoneMappings_ZoneCode] ON [WorkflowZoneMappings] ([ZoneCode]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251116015144_AddWorkflowZoneMappingTable'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251116015144_AddWorkflowZoneMappingTable', N'8.0.5');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251116025410_AddSaveCustomMissionTable'
)
BEGIN
    ALTER TABLE [SavedCustomMissions] ADD [LockRobotAfterFinish] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251116025410_AddSaveCustomMissionTable'
)
BEGIN
    ALTER TABLE [SavedCustomMissions] ADD [OrgId] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251116025410_AddSaveCustomMissionTable'
)
BEGIN
    ALTER TABLE [SavedCustomMissions] ADD [TemplateCode] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251116025410_AddSaveCustomMissionTable'
)
BEGIN
    ALTER TABLE [SavedCustomMissions] ADD [UnlockMissionCode] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251116025410_AddSaveCustomMissionTable'
)
BEGIN
    ALTER TABLE [SavedCustomMissions] ADD [UnlockRobotId] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251116025410_AddSaveCustomMissionTable'
)
BEGIN
    ALTER TABLE [SavedCustomMissions] ADD [ViewBoardType] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251116025410_AddSaveCustomMissionTable'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251116025410_AddSaveCustomMissionTable', N'8.0.5');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251116045835_AddQrCodeCoordinatesAndNavigation'
)
BEGIN
    ALTER TABLE [QrCodes] ADD [AngularAccuracy] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251116045835_AddQrCodeCoordinatesAndNavigation'
)
BEGIN
    ALTER TABLE [QrCodes] ADD [DistanceAccuracy] float NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251116045835_AddQrCodeCoordinatesAndNavigation'
)
BEGIN
    ALTER TABLE [QrCodes] ADD [FunctionListJson] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251116045835_AddQrCodeCoordinatesAndNavigation'
)
BEGIN
    ALTER TABLE [QrCodes] ADD [GoalAngularAccuracy] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251116045835_AddQrCodeCoordinatesAndNavigation'
)
BEGIN
    ALTER TABLE [QrCodes] ADD [GoalDistanceAccuracy] float NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251116045835_AddQrCodeCoordinatesAndNavigation'
)
BEGIN
    ALTER TABLE [QrCodes] ADD [NodeType] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251116045835_AddQrCodeCoordinatesAndNavigation'
)
BEGIN
    ALTER TABLE [QrCodes] ADD [NodeUuid] nvarchar(128) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251116045835_AddQrCodeCoordinatesAndNavigation'
)
BEGIN
    ALTER TABLE [QrCodes] ADD [SpecialConfig] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251116045835_AddQrCodeCoordinatesAndNavigation'
)
BEGIN
    ALTER TABLE [QrCodes] ADD [TransitOrientations] nvarchar(64) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251116045835_AddQrCodeCoordinatesAndNavigation'
)
BEGIN
    ALTER TABLE [QrCodes] ADD [XCoordinate] float NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251116045835_AddQrCodeCoordinatesAndNavigation'
)
BEGIN
    ALTER TABLE [QrCodes] ADD [YCoordinate] float NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251116045835_AddQrCodeCoordinatesAndNavigation'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251116045835_AddQrCodeCoordinatesAndNavigation', N'8.0.5');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251118151505_AddQueueSystem'
)
BEGIN
    DROP TABLE [MissionQueueItems];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251118151505_AddQueueSystem'
)
BEGIN
    DROP TABLE [RobotJobOpportunities];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251118151505_AddQueueSystem'
)
BEGIN
    DECLARE @var0 sysname;
    SELECT @var0 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[MapCodeQueueConfigurations]') AND [c].[name] = N'AverageJobWaitTimeSeconds');
    IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [MapCodeQueueConfigurations] DROP CONSTRAINT [' + @var0 + '];');
    ALTER TABLE [MapCodeQueueConfigurations] DROP COLUMN [AverageJobWaitTimeSeconds];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251118151505_AddQueueSystem'
)
BEGIN
    DECLARE @var1 sysname;
    SELECT @var1 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[MapCodeQueueConfigurations]') AND [c].[name] = N'AverageOpportunisticJobDistanceMeters');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [MapCodeQueueConfigurations] DROP CONSTRAINT [' + @var1 + '];');
    ALTER TABLE [MapCodeQueueConfigurations] DROP COLUMN [AverageOpportunisticJobDistanceMeters];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251118151505_AddQueueSystem'
)
BEGIN
    DECLARE @var2 sysname;
    SELECT @var2 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[MapCodeQueueConfigurations]') AND [c].[name] = N'EnableCrossMapOptimization');
    IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [MapCodeQueueConfigurations] DROP CONSTRAINT [' + @var2 + '];');
    ALTER TABLE [MapCodeQueueConfigurations] DROP COLUMN [EnableCrossMapOptimization];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251118151505_AddQueueSystem'
)
BEGIN
    DECLARE @var3 sysname;
    SELECT @var3 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[MapCodeQueueConfigurations]') AND [c].[name] = N'MaxConcurrentRobotsOnMap');
    IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [MapCodeQueueConfigurations] DROP CONSTRAINT [' + @var3 + '];');
    ALTER TABLE [MapCodeQueueConfigurations] DROP COLUMN [MaxConcurrentRobotsOnMap];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251118151505_AddQueueSystem'
)
BEGIN
    DECLARE @var4 sysname;
    SELECT @var4 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[MapCodeQueueConfigurations]') AND [c].[name] = N'MaxConsecutiveOpportunisticJobs');
    IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [MapCodeQueueConfigurations] DROP CONSTRAINT [' + @var4 + '];');
    ALTER TABLE [MapCodeQueueConfigurations] DROP COLUMN [MaxConsecutiveOpportunisticJobs];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251118151505_AddQueueSystem'
)
BEGIN
    DECLARE @var5 sysname;
    SELECT @var5 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[MapCodeQueueConfigurations]') AND [c].[name] = N'OpportunisticJobsChained');
    IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [MapCodeQueueConfigurations] DROP CONSTRAINT [' + @var5 + '];');
    ALTER TABLE [MapCodeQueueConfigurations] DROP COLUMN [OpportunisticJobsChained];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251118151505_AddQueueSystem'
)
BEGIN
    EXEC sp_rename N'[MapCodeQueueConfigurations].[TotalJobsProcessed]', N'MaxQueueDepth', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251118151505_AddQueueSystem'
)
BEGIN
    EXEC sp_rename N'[MapCodeQueueConfigurations].[QueueProcessingIntervalSeconds]', N'MaxConcurrentTasks', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251118151505_AddQueueSystem'
)
BEGIN
    EXEC sp_rename N'[MapCodeQueueConfigurations].[EnableQueue]', N'IsEnabled', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251118151505_AddQueueSystem'
)
BEGIN
    EXEC sp_rename N'[MapCodeQueueConfigurations].[IX_MapCodeQueueConfigurations_MapCode]', N'IX_MapCodeQueueConfig_MapCode', N'INDEX';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251118151505_AddQueueSystem'
)
BEGIN
    ALTER TABLE [MapCodeQueueConfigurations] ADD [DisplayName] nvarchar(200) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251118151505_AddQueueSystem'
)
BEGIN
    CREATE TABLE [QueueItems] (
        [Id] int NOT NULL IDENTITY,
        [MapCode] nvarchar(128) NOT NULL,
        [Priority] int NOT NULL,
        [QueuePosition] int NOT NULL,
        [SavedMissionId] int NULL,
        [MissionCode] nvarchar(100) NOT NULL,
        [RequestId] nvarchar(100) NOT NULL,
        [MissionDataJson] nvarchar(max) NOT NULL,
        [MissionType] nvarchar(50) NULL,
        [RobotIds] nvarchar(500) NULL,
        [AssignedRobotId] nvarchar(100) NULL,
        [RobotType] nvarchar(50) NULL,
        [TargetNodeCode] nvarchar(100) NULL,
        [TargetX] float NULL,
        [TargetY] float NULL,
        [RequiresOptimization] bit NOT NULL,
        [OrgId] nvarchar(100) NULL,
        [Status] int NOT NULL,
        [CreatedUtc] datetime2 NOT NULL,
        [ProcessingStartedUtc] datetime2 NULL,
        [SubmittedUtc] datetime2 NULL,
        [CompletedUtc] datetime2 NULL,
        [ErrorMessage] nvarchar(2000) NULL,
        [RetryCount] int NOT NULL,
        [CreatedBy] nvarchar(100) NOT NULL,
        CONSTRAINT [PK_QueueItems] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251118151505_AddQueueSystem'
)
BEGIN
    CREATE INDEX [IX_QueueItem_MapProcessing] ON [QueueItems] ([MapCode], [Status], [Priority], [QueuePosition]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251118151505_AddQueueSystem'
)
BEGIN
    CREATE INDEX [IX_QueueItem_MapStatus] ON [QueueItems] ([MapCode], [Status]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251118151505_AddQueueSystem'
)
BEGIN
    CREATE INDEX [IX_QueueItem_MissionCode] ON [QueueItems] ([MissionCode]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251118151505_AddQueueSystem'
)
BEGIN
    CREATE INDEX [IX_QueueItem_RobotStatus] ON [QueueItems] ([AssignedRobotId], [Status]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251118151505_AddQueueSystem'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251118151505_AddQueueSystem', N'8.0.5');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251122054620_AddPagePermissionSystem'
)
BEGIN
    DROP TABLE [MapCodeQueueConfigurations];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251122054620_AddPagePermissionSystem'
)
BEGIN
    DROP TABLE [QueueItems];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251122054620_AddPagePermissionSystem'
)
BEGIN
    CREATE TABLE [Pages] (
        [Id] int NOT NULL IDENTITY,
        [PagePath] nvarchar(255) NOT NULL,
        [PageName] nvarchar(100) NOT NULL,
        [PageIcon] nvarchar(50) NULL,
        [CreatedUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_Pages] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251122054620_AddPagePermissionSystem'
)
BEGIN
    CREATE TABLE [RolePermissions] (
        [Id] int NOT NULL IDENTITY,
        [RoleId] int NOT NULL,
        [PageId] int NOT NULL,
        [CanAccess] bit NOT NULL,
        [CreatedUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_RolePermissions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RolePermissions_Pages_PageId] FOREIGN KEY ([PageId]) REFERENCES [Pages] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_RolePermissions_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251122054620_AddPagePermissionSystem'
)
BEGIN
    CREATE TABLE [UserPermissions] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [PageId] int NOT NULL,
        [CanAccess] bit NOT NULL,
        [CreatedUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_UserPermissions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UserPermissions_Pages_PageId] FOREIGN KEY ([PageId]) REFERENCES [Pages] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_UserPermissions_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251122054620_AddPagePermissionSystem'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Pages_PagePath] ON [Pages] ([PagePath]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251122054620_AddPagePermissionSystem'
)
BEGIN
    CREATE INDEX [IX_RolePermissions_PageId] ON [RolePermissions] ([PageId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251122054620_AddPagePermissionSystem'
)
BEGIN
    CREATE INDEX [IX_RolePermissions_RoleId] ON [RolePermissions] ([RoleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251122054620_AddPagePermissionSystem'
)
BEGIN
    CREATE UNIQUE INDEX [IX_RolePermissions_RoleId_PageId] ON [RolePermissions] ([RoleId], [PageId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251122054620_AddPagePermissionSystem'
)
BEGIN
    CREATE INDEX [IX_UserPermissions_PageId] ON [UserPermissions] ([PageId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251122054620_AddPagePermissionSystem'
)
BEGIN
    CREATE INDEX [IX_UserPermissions_UserId] ON [UserPermissions] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251122054620_AddPagePermissionSystem'
)
BEGIN
    CREATE UNIQUE INDEX [IX_UserPermissions_UserId_PageId] ON [UserPermissions] ([UserId], [PageId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251122054620_AddPagePermissionSystem'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251122054620_AddPagePermissionSystem', N'8.0.5');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251123101344_AddMissionQueue'
)
BEGIN
    CREATE TABLE [MissionQueues] (
        [Id] int NOT NULL IDENTITY,
        [MissionCode] nvarchar(100) NOT NULL,
        [RequestId] nvarchar(100) NOT NULL,
        [SavedMissionId] int NULL,
        [MissionName] nvarchar(200) NOT NULL,
        [MissionRequestJson] nvarchar(max) NOT NULL,
        [Status] int NOT NULL,
        [Priority] int NOT NULL,
        [QueuePosition] int NOT NULL,
        [AssignedRobotId] nvarchar(50) NULL,
        [CreatedUtc] datetime2 NOT NULL,
        [ProcessingStartedUtc] datetime2 NULL,
        [AssignedUtc] datetime2 NULL,
        [CompletedUtc] datetime2 NULL,
        [CreatedBy] nvarchar(100) NULL,
        [RetryCount] int NOT NULL,
        [MaxRetries] int NOT NULL,
        [ErrorMessage] nvarchar(500) NULL,
        [RobotTypeFilter] nvarchar(50) NULL,
        [PreferredRobotIds] nvarchar(500) NULL,
        CONSTRAINT [PK_MissionQueues] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251123101344_AddMissionQueue'
)
BEGIN
    CREATE INDEX [IX_MissionQueues_AssignedRobotId] ON [MissionQueues] ([AssignedRobotId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251123101344_AddMissionQueue'
)
BEGIN
    CREATE UNIQUE INDEX [IX_MissionQueues_MissionCode] ON [MissionQueues] ([MissionCode]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251123101344_AddMissionQueue'
)
BEGIN
    CREATE INDEX [IX_MissionQueues_SavedMissionId] ON [MissionQueues] ([SavedMissionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251123101344_AddMissionQueue'
)
BEGIN
    CREATE INDEX [IX_MissionQueues_Status_Priority_CreatedUtc] ON [MissionQueues] ([Status], [Priority], [CreatedUtc]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251123101344_AddMissionQueue'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251123101344_AddMissionQueue', N'8.0.5');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251125122006_AddTemplatePermissionSystem'
)
BEGIN
    CREATE TABLE [RoleTemplatePermissions] (
        [Id] int NOT NULL IDENTITY,
        [RoleId] int NOT NULL,
        [SavedCustomMissionId] int NOT NULL,
        [CanAccess] bit NOT NULL,
        [CreatedUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_RoleTemplatePermissions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RoleTemplatePermissions_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_RoleTemplatePermissions_SavedCustomMissions_SavedCustomMissionId] FOREIGN KEY ([SavedCustomMissionId]) REFERENCES [SavedCustomMissions] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251125122006_AddTemplatePermissionSystem'
)
BEGIN
    CREATE TABLE [UserTemplatePermissions] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [SavedCustomMissionId] int NOT NULL,
        [CanAccess] bit NOT NULL,
        [CreatedUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_UserTemplatePermissions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UserTemplatePermissions_SavedCustomMissions_SavedCustomMissionId] FOREIGN KEY ([SavedCustomMissionId]) REFERENCES [SavedCustomMissions] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_UserTemplatePermissions_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251125122006_AddTemplatePermissionSystem'
)
BEGIN
    CREATE INDEX [IX_RoleTemplatePermissions_RoleId] ON [RoleTemplatePermissions] ([RoleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251125122006_AddTemplatePermissionSystem'
)
BEGIN
    CREATE UNIQUE INDEX [IX_RoleTemplatePermissions_RoleId_SavedCustomMissionId] ON [RoleTemplatePermissions] ([RoleId], [SavedCustomMissionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251125122006_AddTemplatePermissionSystem'
)
BEGIN
    CREATE INDEX [IX_RoleTemplatePermissions_SavedCustomMissionId] ON [RoleTemplatePermissions] ([SavedCustomMissionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251125122006_AddTemplatePermissionSystem'
)
BEGIN
    CREATE INDEX [IX_UserTemplatePermissions_SavedCustomMissionId] ON [UserTemplatePermissions] ([SavedCustomMissionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251125122006_AddTemplatePermissionSystem'
)
BEGIN
    CREATE INDEX [IX_UserTemplatePermissions_UserId] ON [UserTemplatePermissions] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251125122006_AddTemplatePermissionSystem'
)
BEGIN
    CREATE UNIQUE INDEX [IX_UserTemplatePermissions_UserId_SavedCustomMissionId] ON [UserTemplatePermissions] ([UserId], [SavedCustomMissionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251125122006_AddTemplatePermissionSystem'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251125122006_AddTemplatePermissionSystem', N'8.0.5');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251126003426_AddWorkflowSchedule'
)
BEGIN
    CREATE TABLE [WorkflowSchedules] (
        [Id] int NOT NULL IDENTITY,
        [SavedMissionId] int NOT NULL,
        [ScheduleName] nvarchar(200) NOT NULL,
        [Description] nvarchar(500) NULL,
        [ScheduleType] nvarchar(20) NOT NULL,
        [OneTimeUtc] datetime2 NULL,
        [IntervalMinutes] int NULL,
        [CronExpression] nvarchar(100) NULL,
        [IsEnabled] bit NOT NULL,
        [NextRunUtc] datetime2 NULL,
        [LastRunUtc] datetime2 NULL,
        [LastRunStatus] nvarchar(20) NULL,
        [LastErrorMessage] nvarchar(500) NULL,
        [ExecutionCount] int NOT NULL,
        [MaxExecutions] int NULL,
        [CreatedBy] nvarchar(100) NOT NULL,
        [CreatedUtc] datetime2 NOT NULL,
        [UpdatedUtc] datetime2 NULL,
        CONSTRAINT [PK_WorkflowSchedules] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_WorkflowSchedules_SavedCustomMissions_SavedMissionId] FOREIGN KEY ([SavedMissionId]) REFERENCES [SavedCustomMissions] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251126003426_AddWorkflowSchedule'
)
BEGIN
    CREATE INDEX [IX_WorkflowSchedules_IsEnabled_NextRunUtc] ON [WorkflowSchedules] ([IsEnabled], [NextRunUtc]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251126003426_AddWorkflowSchedule'
)
BEGIN
    CREATE INDEX [IX_WorkflowSchedules_SavedMissionId] ON [WorkflowSchedules] ([SavedMissionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251126003426_AddWorkflowSchedule'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251126003426_AddWorkflowSchedule', N'8.0.5');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251126050204_AddMissionQueueJobOptimization'
)
BEGIN
    ALTER TABLE [MissionQueues] ADD [ReservedByMissionCode] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251126050204_AddMissionQueueJobOptimization'
)
BEGIN
    ALTER TABLE [MissionQueues] ADD [ReservedForRobotId] nvarchar(50) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251126050204_AddMissionQueueJobOptimization'
)
BEGIN
    ALTER TABLE [MissionQueues] ADD [ReservedUtc] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251126050204_AddMissionQueueJobOptimization'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251126050204_AddMissionQueueJobOptimization', N'8.0.5');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251126074811_AddMapEdgeAndFloorMap'
)
BEGIN
    CREATE TABLE [FloorMaps] (
        [Id] int NOT NULL IDENTITY,
        [MapCode] nvarchar(128) NOT NULL,
        [FloorNumber] nvarchar(16) NOT NULL,
        [FloorName] nvarchar(64) NOT NULL,
        [FloorLevel] int NOT NULL,
        [FloorLength] float NOT NULL,
        [FloorWidth] float NOT NULL,
        [FloorMapVersion] nvarchar(32) NULL,
        [LaserMapId] int NULL,
        [NodeCount] int NOT NULL,
        [EdgeCount] int NOT NULL,
        [CreateTime] datetime2 NOT NULL,
        [LastUpdateTime] datetime2 NOT NULL,
        CONSTRAINT [PK_FloorMaps] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251126074811_AddMapEdgeAndFloorMap'
)
BEGIN
    CREATE TABLE [MapEdges] (
        [Id] int NOT NULL IDENTITY,
        [BeginNodeLabel] nvarchar(64) NOT NULL,
        [EndNodeLabel] nvarchar(64) NOT NULL,
        [MapCode] nvarchar(128) NOT NULL,
        [FloorNumber] nvarchar(16) NOT NULL,
        [EdgeLength] float NOT NULL,
        [EdgeType] int NOT NULL,
        [EdgeWeight] float NOT NULL,
        [EdgeWidth] float NOT NULL,
        [MaxVelocity] float NOT NULL,
        [MaxAccelerationVelocity] float NOT NULL,
        [MaxDecelerationVelocity] float NOT NULL,
        [Orientation] int NOT NULL,
        [Radius] float NOT NULL,
        [RoadType] nvarchar(32) NULL,
        [Status] int NOT NULL,
        [CreateTime] datetime2 NOT NULL,
        [LastUpdateTime] datetime2 NOT NULL,
        CONSTRAINT [PK_MapEdges] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251126074811_AddMapEdgeAndFloorMap'
)
BEGIN
    CREATE UNIQUE INDEX [IX_FloorMaps_MapCode_FloorNumber] ON [FloorMaps] ([MapCode], [FloorNumber]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251126074811_AddMapEdgeAndFloorMap'
)
BEGIN
    CREATE UNIQUE INDEX [IX_MapEdges_BeginNodeLabel_EndNodeLabel_MapCode] ON [MapEdges] ([BeginNodeLabel], [EndNodeLabel], [MapCode]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251126074811_AddMapEdgeAndFloorMap'
)
BEGIN
    CREATE INDEX [IX_MapEdges_MapCode_FloorNumber] ON [MapEdges] ([MapCode], [FloorNumber]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251126074811_AddMapEdgeAndFloorMap'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251126074811_AddMapEdgeAndFloorMap', N'8.0.5');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251129084950_RemoveWarehouseLiveMap'
)
BEGIN
    DROP TABLE [FloorMaps];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251129084950_RemoveWarehouseLiveMap'
)
BEGIN
    DROP TABLE [MapEdges];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251129084950_RemoveWarehouseLiveMap'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251129084950_RemoveWarehouseLiveMap', N'8.0.5');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202015615_AddRobotMonitoringMaps'
)
BEGIN
    CREATE TABLE [RobotMonitoringMaps] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(256) NOT NULL,
        [Description] nvarchar(1000) NULL,
        [MapCode] nvarchar(128) NULL,
        [FloorNumber] nvarchar(16) NULL,
        [BackgroundImagePath] nvarchar(512) NULL,
        [BackgroundImageOriginalName] nvarchar(256) NULL,
        [ImageWidth] int NULL,
        [ImageHeight] int NULL,
        [CoordinateSettingsJson] nvarchar(max) NULL,
        [DisplaySettingsJson] nvarchar(max) NULL,
        [RefreshIntervalMs] int NOT NULL,
        [IsDefault] bit NOT NULL,
        [CreatedBy] nvarchar(100) NOT NULL,
        [CreatedUtc] datetime2 NOT NULL,
        [LastUpdatedBy] nvarchar(100) NULL,
        [LastUpdatedUtc] datetime2 NULL,
        CONSTRAINT [PK_RobotMonitoringMaps] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202015615_AddRobotMonitoringMaps'
)
BEGIN
    CREATE INDEX [IX_RobotMonitoringMaps_CreatedBy] ON [RobotMonitoringMaps] ([CreatedBy]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202015615_AddRobotMonitoringMaps'
)
BEGIN
    CREATE INDEX [IX_RobotMonitoringMaps_IsDefault] ON [RobotMonitoringMaps] ([IsDefault]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202015615_AddRobotMonitoringMaps'
)
BEGIN
    CREATE INDEX [IX_RobotMonitoringMaps_MapCode] ON [RobotMonitoringMaps] ([MapCode]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202015615_AddRobotMonitoringMaps'
)
BEGIN
    CREATE INDEX [IX_RobotMonitoringMaps_Name] ON [RobotMonitoringMaps] ([Name]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202015615_AddRobotMonitoringMaps'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251202015615_AddRobotMonitoringMaps', N'8.0.5');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202040058_AddCustomNodesZonesToRobotMonitoringMap'
)
BEGIN
    ALTER TABLE [RobotMonitoringMaps] ADD [CustomNodesJson] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202040058_AddCustomNodesZonesToRobotMonitoringMap'
)
BEGIN
    ALTER TABLE [RobotMonitoringMaps] ADD [CustomZonesJson] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202040058_AddCustomNodesZonesToRobotMonitoringMap'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251202040058_AddCustomNodesZonesToRobotMonitoringMap', N'8.0.5');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202142444_AddCustomLinesJsonToRobotMonitoringMap'
)
BEGIN
    ALTER TABLE [RobotMonitoringMaps] ADD [CustomLinesJson] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202142444_AddCustomLinesJsonToRobotMonitoringMap'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251202142444_AddCustomLinesJsonToRobotMonitoringMap', N'8.0.5');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251203060744_AddRobotLicenseFields'
)
BEGIN
    ALTER TABLE [MobileRobots] ADD [IsLicensed] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251203060744_AddRobotLicenseFields'
)
BEGIN
    ALTER TABLE [MobileRobots] ADD [LicenseError] nvarchar(500) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251203060744_AddRobotLicenseFields'
)
BEGIN
    CREATE UNIQUE INDEX [IX_MobileRobots_RobotId] ON [MobileRobots] ([RobotId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251203060744_AddRobotLicenseFields'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251203060744_AddRobotLicenseFields', N'8.0.5');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251204143252_AddIsActiveToSavedCustomMission'
)
BEGIN
    ALTER TABLE [SavedCustomMissions] ADD [IsActive] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251204143252_AddIsActiveToSavedCustomMission'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251204143252_AddIsActiveToSavedCustomMission', N'8.0.5');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251206040546_AddRefreshTokens'
)
BEGIN
    CREATE TABLE [RefreshTokens] (
        [Id] int NOT NULL IDENTITY,
        [Token] nvarchar(256) NOT NULL,
        [UserId] int NOT NULL,
        [ExpiresUtc] datetime2 NOT NULL,
        [CreatedUtc] datetime2 NOT NULL,
        [RevokedUtc] datetime2 NULL,
        [ReplacedByToken] nvarchar(256) NULL,
        [RevokedReason] nvarchar(100) NULL,
        CONSTRAINT [PK_RefreshTokens] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RefreshTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251206040546_AddRefreshTokens'
)
BEGIN
    CREATE INDEX [IX_RefreshTokens_Token] ON [RefreshTokens] ([Token]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251206040546_AddRefreshTokens'
)
BEGIN
    CREATE INDEX [IX_RefreshTokens_UserId] ON [RefreshTokens] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251206040546_AddRefreshTokens'
)
BEGIN
    CREATE INDEX [IX_RefreshTokens_UserId_ExpiresUtc] ON [RefreshTokens] ([UserId], [ExpiresUtc]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251206040546_AddRefreshTokens'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251206040546_AddRefreshTokens', N'8.0.5');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251208003714_AddConcurrencyModeToSavedCustomMission'
)
BEGIN
    ALTER TABLE [SavedCustomMissions] ADD [ConcurrencyMode] nvarchar(20) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251208003714_AddConcurrencyModeToSavedCustomMission'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251208003714_AddConcurrencyModeToSavedCustomMission', N'8.0.5');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251208020044_AddOrganizationIds'
)
BEGIN
    CREATE TABLE [OrganizationIds] (
        [Id] int NOT NULL IDENTITY,
        [DisplayName] nvarchar(128) NOT NULL,
        [ActualValue] nvarchar(128) NOT NULL,
        [Description] nvarchar(512) NULL,
        [IsActive] bit NOT NULL,
        [CreatedUtc] datetime2 NOT NULL,
        [UpdatedUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_OrganizationIds] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251208020044_AddOrganizationIds'
)
BEGIN
    CREATE UNIQUE INDEX [IX_OrganizationIds_ActualValue] ON [OrganizationIds] ([ActualValue]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251208020044_AddOrganizationIds'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251208020044_AddOrganizationIds', N'8.0.5');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251213052722_AddTemplateCategories'
)
BEGIN
    ALTER TABLE [SavedCustomMissions] ADD [CategoryId] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251213052722_AddTemplateCategories'
)
BEGIN
    CREATE TABLE [TemplateCategories] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(128) NOT NULL,
        [Description] nvarchar(512) NULL,
        [DisplayOrder] int NOT NULL,
        [CreatedUtc] datetime2 NOT NULL,
        [UpdatedUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_TemplateCategories] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251213052722_AddTemplateCategories'
)
BEGIN
    CREATE INDEX [IX_SavedCustomMissions_CategoryId] ON [SavedCustomMissions] ([CategoryId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251213052722_AddTemplateCategories'
)
BEGIN
    CREATE UNIQUE INDEX [IX_TemplateCategories_Name] ON [TemplateCategories] ([Name]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251213052722_AddTemplateCategories'
)
BEGIN
    ALTER TABLE [SavedCustomMissions] ADD CONSTRAINT [FK_SavedCustomMissions_TemplateCategories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [TemplateCategories] ([Id]) ON DELETE SET NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251213052722_AddTemplateCategories'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251213052722_AddTemplateCategories', N'8.0.5');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251214031341_AddSkipIfRunningToWorkflowSchedule'
)
BEGIN
    ALTER TABLE [WorkflowSchedules] ADD [SkipIfRunning] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251214031341_AddSkipIfRunningToWorkflowSchedule'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251214031341_AddSkipIfRunningToWorkflowSchedule', N'8.0.5');
END;
GO

COMMIT;
GO

