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

    public DbSet<WorkflowNodeCode> WorkflowNodeCodes => Set<WorkflowNodeCode>();

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

    public DbSet<User> Users => Set<User>();

    public DbSet<Role> Roles => Set<Role>();



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

        modelBuilder.Entity<WorkflowNodeCode>(entity =>
        {
            // Composite index for efficient querying by workflow and node code
            entity.HasIndex(e => new { e.ExternalWorkflowId, e.NodeCode }).IsUnique();
            // Index for querying all node codes for a specific workflow
            entity.HasIndex(e => e.ExternalWorkflowId);
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

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasIndex(e => e.Username).IsUnique();
            entity.Property(e => e.Username).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Nickname).HasMaxLength(100);
            entity.Property(e => e.RolesJson).HasColumnType("nvarchar(max)");
            entity.Property(e => e.CreateTime).HasColumnType("datetime2");
            entity.Property(e => e.LastUpdateTime).HasColumnType("datetime2");
            entity.Property(e => e.CreateBy).HasMaxLength(100);
            entity.Property(e => e.CreateApp).HasMaxLength(100);
            entity.Property(e => e.LastUpdateBy).HasMaxLength(100);
            entity.Property(e => e.LastUpdateApp).HasMaxLength(100);
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("Roles");
            entity.HasIndex(e => e.RoleCode).IsUnique();
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.RoleCode).HasMaxLength(100).IsRequired();
            entity.Property(e => e.CreatedUtc).HasColumnType("datetime2");
            entity.Property(e => e.UpdatedUtc).HasColumnType("datetime2");
        });

        modelBuilder.Entity<SystemSetting>(entity =>
        {
            entity.ToTable("SystemSetting");
            entity.HasIndex(e => e.Key).IsUnique();
            entity.Property(e => e.Key).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Value).HasMaxLength(255).IsRequired();
            entity.Property(e => e.LastUpdated).HasColumnType("datetime2");
        });
    }
}
