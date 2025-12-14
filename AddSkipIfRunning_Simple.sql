-- =====================================================
-- Migration: AddSkipIfRunningToWorkflowSchedule
-- Description: Adds SkipIfRunning column to WorkflowSchedules table
-- Date: 2024-12-14
-- =====================================================

-- Check if migration has already been applied
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251214031341_AddSkipIfRunningToWorkflowSchedule'
)
BEGIN
    PRINT 'Applying migration: AddSkipIfRunningToWorkflowSchedule';

    -- Add SkipIfRunning column with default value false
    ALTER TABLE [WorkflowSchedules] ADD [SkipIfRunning] bit NOT NULL DEFAULT CAST(0 AS bit);

    -- Record migration in history
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251214031341_AddSkipIfRunningToWorkflowSchedule', N'8.0.5');

    PRINT 'Migration applied successfully.';
END
ELSE
BEGIN
    PRINT 'Migration already applied. Skipping.';
END
GO
