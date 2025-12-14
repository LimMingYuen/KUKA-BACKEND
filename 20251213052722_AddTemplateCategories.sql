-- =====================================================
-- Migration: 20251213052722_AddTemplateCategories
-- Generated: 2025-12-13
-- Description: Adds TemplateCategories table and links
--              SavedCustomMissions with CategoryId FK
-- =====================================================

-- 1. Create TemplateCategories table
CREATE TABLE [TemplateCategories] (
    [Id] int NOT NULL IDENTITY(1, 1),
    [Name] nvarchar(128) NOT NULL,
    [Description] nvarchar(512) NULL,
    [DisplayOrder] int NOT NULL,
    [CreatedUtc] datetime2 NOT NULL,
    [UpdatedUtc] datetime2 NOT NULL,
    CONSTRAINT [PK_TemplateCategories] PRIMARY KEY ([Id])
);

-- 2. Add CategoryId column to SavedCustomMissions
ALTER TABLE [SavedCustomMissions] ADD [CategoryId] int NULL;

-- 3. Create unique index on TemplateCategories.Name
CREATE UNIQUE INDEX [IX_TemplateCategories_Name]
ON [TemplateCategories] ([Name]);

-- 4. Create index on SavedCustomMissions.CategoryId
CREATE INDEX [IX_SavedCustomMissions_CategoryId]
ON [SavedCustomMissions] ([CategoryId]);

-- 5. Add foreign key constraint (SET NULL on delete)
ALTER TABLE [SavedCustomMissions] ADD CONSTRAINT [FK_SavedCustomMissions_TemplateCategories_CategoryId]
FOREIGN KEY ([CategoryId]) REFERENCES [TemplateCategories] ([Id])
ON DELETE SET NULL;

-- 6. Record migration in EF migration history
INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251213052722_AddTemplateCategories', N'8.0.5');

GO
