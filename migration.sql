BEGIN TRANSACTION;
GO

ALTER TABLE [SavedCustomMissions] ADD [CategoryId] int NULL;
GO

CREATE TABLE [TemplateCategories] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(128) NOT NULL,
    [Description] nvarchar(512) NULL,
    [DisplayOrder] int NOT NULL,
    [CreatedUtc] datetime2 NOT NULL,
    [UpdatedUtc] datetime2 NOT NULL,
    CONSTRAINT [PK_TemplateCategories] PRIMARY KEY ([Id])
);
GO

CREATE INDEX [IX_SavedCustomMissions_CategoryId] ON [SavedCustomMissions] ([CategoryId]);
GO

CREATE UNIQUE INDEX [IX_TemplateCategories_Name] ON [TemplateCategories] ([Name]);
GO

ALTER TABLE [SavedCustomMissions] ADD CONSTRAINT [FK_SavedCustomMissions_TemplateCategories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [TemplateCategories] ([Id]) ON DELETE SET NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251213052722_AddTemplateCategories', N'8.0.5');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [WorkflowSchedules] ADD [SkipIfRunning] bit NOT NULL DEFAULT CAST(0 AS bit);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251214031341_AddSkipIfRunningToWorkflowSchedule', N'8.0.5');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [IoControllerDevices] (
    [Id] int NOT NULL IDENTITY,
    [DeviceName] nvarchar(100) NOT NULL,
    [IpAddress] nvarchar(45) NOT NULL,
    [Port] int NOT NULL,
    [UnitId] tinyint NOT NULL,
    [Description] nvarchar(500) NULL,
    [IsActive] bit NOT NULL,
    [PollingIntervalMs] int NOT NULL,
    [ConnectionTimeoutMs] int NOT NULL,
    [LastPollUtc] datetime2 NULL,
    [LastConnectionSuccess] bit NULL,
    [LastErrorMessage] nvarchar(500) NULL,
    [CreatedUtc] datetime2 NOT NULL,
    [UpdatedUtc] datetime2 NOT NULL,
    [CreatedBy] nvarchar(100) NULL,
    [UpdatedBy] nvarchar(100) NULL,
    CONSTRAINT [PK_IoControllerDevices] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [IoChannels] (
    [Id] int NOT NULL IDENTITY,
    [DeviceId] int NOT NULL,
    [ChannelNumber] int NOT NULL,
    [ChannelType] int NOT NULL,
    [Label] nvarchar(100) NULL,
    [CurrentState] bit NOT NULL,
    [FailSafeValue] bit NULL,
    [FsvEnabled] bit NOT NULL,
    [LastStateChangeUtc] datetime2 NULL,
    CONSTRAINT [PK_IoChannels] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_IoChannels_IoControllerDevices_DeviceId] FOREIGN KEY ([DeviceId]) REFERENCES [IoControllerDevices] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [IoStateLogs] (
    [Id] int NOT NULL IDENTITY,
    [DeviceId] int NOT NULL,
    [ChannelNumber] int NOT NULL,
    [ChannelType] int NOT NULL,
    [PreviousState] bit NOT NULL,
    [NewState] bit NOT NULL,
    [ChangeSource] int NOT NULL,
    [ChangedBy] nvarchar(100) NULL,
    [ChangedUtc] datetime2 NOT NULL,
    [Reason] nvarchar(500) NULL,
    CONSTRAINT [PK_IoStateLogs] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_IoStateLogs_IoControllerDevices_DeviceId] FOREIGN KEY ([DeviceId]) REFERENCES [IoControllerDevices] ([Id]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_IoChannels_DeviceId] ON [IoChannels] ([DeviceId]);
GO

CREATE UNIQUE INDEX [IX_IoChannels_DeviceId_ChannelType_ChannelNumber] ON [IoChannels] ([DeviceId], [ChannelType], [ChannelNumber]);
GO

CREATE INDEX [IX_IoControllerDevices_DeviceName] ON [IoControllerDevices] ([DeviceName]);
GO

CREATE UNIQUE INDEX [IX_IoControllerDevices_IpAddress_Port] ON [IoControllerDevices] ([IpAddress], [Port]);
GO

CREATE INDEX [IX_IoControllerDevices_IsActive] ON [IoControllerDevices] ([IsActive]);
GO

CREATE INDEX [IX_IoStateLogs_ChangedUtc] ON [IoStateLogs] ([ChangedUtc]);
GO

CREATE INDEX [IX_IoStateLogs_DeviceId] ON [IoStateLogs] ([DeviceId]);
GO

CREATE INDEX [IX_IoStateLogs_DeviceId_ChangedUtc] ON [IoStateLogs] ([DeviceId], [ChangedUtc]);
GO

CREATE INDEX [IX_IoStateLogs_DeviceId_ChannelNumber_ChannelType] ON [IoStateLogs] ([DeviceId], [ChannelNumber], [ChannelType]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251221085011_AddIoControllerEntities', N'8.0.5');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [EmailRecipients] (
    [Id] int NOT NULL IDENTITY,
    [EmailAddress] nvarchar(255) NOT NULL,
    [DisplayName] nvarchar(128) NOT NULL,
    [Description] nvarchar(512) NULL,
    [NotificationTypes] nvarchar(512) NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedUtc] datetime2 NOT NULL,
    [UpdatedUtc] datetime2 NOT NULL,
    [CreatedBy] nvarchar(128) NULL,
    [UpdatedBy] nvarchar(128) NULL,
    CONSTRAINT [PK_EmailRecipients] PRIMARY KEY ([Id])
);
GO

CREATE UNIQUE INDEX [IX_EmailRecipients_EmailAddress] ON [EmailRecipients] ([EmailAddress]);
GO

CREATE INDEX [IX_EmailRecipients_IsActive] ON [EmailRecipients] ([IsActive]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251222141356_AddEmailRecipients', N'8.0.5');
GO

COMMIT;
GO

