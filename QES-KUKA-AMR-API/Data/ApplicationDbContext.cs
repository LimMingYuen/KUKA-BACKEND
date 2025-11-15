using Microsoft.EntityFrameworkCore;
using QES_KUKA_AMR_API.Data.Entities;

namespace QES_KUKA_AMR_API.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<WorkflowDiagram> WorkflowDiagrams => Set<WorkflowDiagram>();

    public DbSet<MissionHistory> MissionHistories => Set<MissionHistory>();



    public DbSet<MobileRobot> MobileRobots => Set<MobileRobot>();

    public DbSet<QrCode> QrCodes => Set<QrCode>();

    public DbSet<MapZone> MapZones => Set<MapZone>();

    public DbSet<MissionType> MissionTypes => Set<MissionType>();

    public DbSet<RobotType> RobotTypes => Set<RobotType>();

    public DbSet<ShelfDecisionRule> ShelfDecisionRules => Set<ShelfDecisionRule>();

    public DbSet<ResumeStrategy> ResumeStrategies => Set<ResumeStrategy>();

    public DbSet<Area> Areas => Set<Area>();

    public DbSet<SavedCustomMission> SavedCustomMissions => Set<SavedCustomMission>();

    public DbSet<RobotManualPause> RobotManualPauses => Set<RobotManualPause>();



    public DbSet<WorkflowSchedule> WorkflowSchedules => Set<WorkflowSchedule>();

    public DbSet<WorkflowScheduleLog> WorkflowScheduleLogs => Set<WorkflowScheduleLog>();

    public DbSet<User> Users => Set<User>();

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    public DbSet<Page> Pages => Set<Page>();

    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<WorkflowDiagram>(entity =>
        {
            entity.HasIndex(e => e.WorkflowCode).IsUnique();
            entity.Property(e => e.CreateTime).HasColumnType("datetime2");
            entity.Property(e => e.UpdateTime).HasColumnType("datetime2");
        });

        modelBuilder.Entity<MissionHistory>(entity =>
        {
            entity.Property(e => e.CreatedDate).HasColumnType("datetime2");
            entity.Property(e => e.ProcessedDate).HasColumnType("datetime2");
            entity.Property(e => e.SubmittedToAmrDate).HasColumnType("datetime2");
            entity.Property(e => e.CompletedDate).HasColumnType("datetime2");
            entity.HasIndex(e => e.MissionCode);
            entity.HasIndex(e => e.SavedMissionId);
            entity.HasIndex(e => new { e.AssignedRobotId, e.CompletedDate });
            entity.HasIndex(e => new { e.TriggerSource, e.CompletedDate });
        });



        modelBuilder.Entity<MobileRobot>(entity =>
        {
            entity.Property(e => e.CreateTime).HasColumnType("datetime2");
            entity.Property(e => e.LastUpdateTime).HasColumnType("datetime2");
            entity.Property(e => e.SendConfigTime).HasColumnType("datetime2");
            entity.Property(e => e.SendFirmwareTime).HasColumnType("datetime2");
            entity.Property(e => e.RobotId).HasMaxLength(100);
        });

        modelBuilder.Entity<QrCode>(entity =>
        {
            entity.Property(e => e.CreateTime).HasColumnType("datetime2");
            entity.Property(e => e.LastUpdateTime).HasColumnType("datetime2");
            entity.HasIndex(e => new { e.NodeLabel, e.MapCode }).IsUnique();
            entity.HasIndex(e => e.MapCode);
        });

        modelBuilder.Entity<MapZone>(entity =>
        {
            entity.Property(e => e.CreateTime).HasColumnType("datetime2");
            entity.Property(e => e.LastUpdateTime).HasColumnType("datetime2");
            entity.Property(e => e.BeginTime).HasColumnType("datetime2");
            entity.Property(e => e.EndTime).HasColumnType("datetime2");
            entity.HasIndex(e => e.ZoneCode).IsUnique();
            entity.HasIndex(e => e.MapCode);
        });

        modelBuilder.Entity<MissionType>(entity =>
        {
            entity.HasIndex(e => e.ActualValue).IsUnique();
            entity.Property(e => e.DisplayName).HasMaxLength(128);
            entity.Property(e => e.ActualValue).HasMaxLength(128);
            entity.Property(e => e.Description).HasMaxLength(512);
            entity.Property(e => e.CreatedUtc).HasColumnType("datetime2");
            entity.Property(e => e.UpdatedUtc).HasColumnType("datetime2");
        });

        modelBuilder.Entity<RobotType>(entity =>
        {
            entity.HasIndex(e => e.ActualValue).IsUnique();
            entity.Property(e => e.DisplayName).HasMaxLength(128);
            entity.Property(e => e.ActualValue).HasMaxLength(128);
            entity.Property(e => e.Description).HasMaxLength(512);
            entity.Property(e => e.CreatedUtc).HasColumnType("datetime2");
            entity.Property(e => e.UpdatedUtc).HasColumnType("datetime2");
        });

        modelBuilder.Entity<ShelfDecisionRule>(entity =>
        {
            entity.HasIndex(e => e.ActualValue).IsUnique();
            entity.Property(e => e.DisplayName).HasMaxLength(128);
            entity.Property(e => e.ActualValue).HasMaxLength(128);
            entity.Property(e => e.Description).HasMaxLength(512);
            entity.Property(e => e.CreatedUtc).HasColumnType("datetime2");
            entity.Property(e => e.UpdatedUtc).HasColumnType("datetime2");
        });

        modelBuilder.Entity<ResumeStrategy>(entity =>
        {
            entity.HasIndex(e => e.ActualValue).IsUnique();
            entity.Property(e => e.DisplayName).HasMaxLength(128);
            entity.Property(e => e.ActualValue).HasMaxLength(128);
            entity.Property(e => e.Description).HasMaxLength(512);
            entity.Property(e => e.CreatedUtc).HasColumnType("datetime2");
            entity.Property(e => e.UpdatedUtc).HasColumnType("datetime2");
        });

        modelBuilder.Entity<Area>(entity =>
        {
            entity.HasIndex(e => e.ActualValue).IsUnique();
            entity.Property(e => e.DisplayName).HasMaxLength(128);
            entity.Property(e => e.ActualValue).HasMaxLength(128);
            entity.Property(e => e.Description).HasMaxLength(512);
            entity.Property(e => e.CreatedUtc).HasColumnType("datetime2");
            entity.Property(e => e.UpdatedUtc).HasColumnType("datetime2");
        });

        modelBuilder.Entity<SavedCustomMission>(entity =>
        {
            entity.HasIndex(e => e.MissionName);
            entity.HasIndex(e => new { e.CreatedBy, e.IsDeleted });
            entity.Property(e => e.MissionStepsJson).HasColumnType("nvarchar(max)");
            entity.Property(e => e.CreatedUtc).HasColumnType("datetime2");
            entity.Property(e => e.UpdatedUtc).HasColumnType("datetime2");
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        modelBuilder.Entity<RobotManualPause>(entity =>
        {
            entity.Property(e => e.PauseStartUtc).HasColumnType("datetime2");
            entity.Property(e => e.PauseEndUtc).HasColumnType("datetime2");
            entity.Property(e => e.CreatedUtc).HasColumnType("datetime2");
            entity.Property(e => e.UpdatedUtc).HasColumnType("datetime2");
            entity.HasIndex(e => new { e.RobotId, e.PauseStartUtc });
            entity.HasIndex(e => new { e.MissionCode, e.PauseStartUtc });
        });



        modelBuilder.Entity<WorkflowSchedule>(entity =>
        {
            entity.Property(e => e.CreatedUtc).HasColumnType("datetime2");
            entity.Property(e => e.UpdatedUtc).HasColumnType("datetime2");
            entity.Property(e => e.LastRunUtc).HasColumnType("datetime2");
            entity.Property(e => e.NextRunUtc).HasColumnType("datetime2");
            entity.Property(e => e.OneTimeRunUtc).HasColumnType("datetime2");
            entity.HasIndex(e => new { e.WorkflowId, e.IsEnabled });
            entity.HasIndex(e => e.NextRunUtc);
        });

        modelBuilder.Entity<WorkflowScheduleLog>(entity =>
        {
            entity.Property(e => e.ScheduledForUtc).HasColumnType("datetime2");
            entity.Property(e => e.EnqueuedUtc).HasColumnType("datetime2");
            entity.Property(e => e.CreatedUtc).HasColumnType("datetime2");
            entity.HasIndex(e => e.ScheduleId);
            entity.HasIndex(e => new { e.ScheduleId, e.CreatedUtc });  // Composite index for filtering by schedule
            entity.HasIndex(e => new { e.WorkflowId, e.CreatedUtc });  // Composite index for GetLogsAsync query without JOIN
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CreateTime).HasColumnType("datetime2");
            entity.Property(e => e.LastUpdateTime).HasColumnType("datetime2");
            entity.HasIndex(e => e.Username).IsUnique();
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.RoleCode).IsUnique();
        });

        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.RoleId, e.PageId }).IsUnique();
        });

        modelBuilder.Entity<SystemSetting>(entity =>
        {
            entity.ToTable("SystemSetting");
            entity.HasIndex(e => e.Key).IsUnique();
            entity.Property(e => e.Key).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Value).HasMaxLength(255).IsRequired();
            entity.Property(e => e.LastUpdated).HasColumnType("datetime2");
        });

        modelBuilder.Entity<Page>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CreatedAt).HasColumnType("datetime2");
            entity.HasIndex(e => e.PagePath).IsUnique();
        });
    }
}
