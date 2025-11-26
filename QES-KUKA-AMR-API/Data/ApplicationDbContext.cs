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

    public DbSet<WorkflowZoneMapping> WorkflowZoneMappings => Set<WorkflowZoneMapping>();

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

    public DbSet<Page> Pages => Set<Page>();

    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    public DbSet<UserPermission> UserPermissions => Set<UserPermission>();

    public DbSet<RoleTemplatePermission> RoleTemplatePermissions => Set<RoleTemplatePermission>();

    public DbSet<UserTemplatePermission> UserTemplatePermissions => Set<UserTemplatePermission>();

    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();

    public DbSet<MissionQueue> MissionQueues => Set<MissionQueue>();

    public DbSet<WorkflowSchedule> WorkflowSchedules => Set<WorkflowSchedule>();

    public DbSet<MapEdge> MapEdges => Set<MapEdge>();

    public DbSet<FloorMap> FloorMaps => Set<FloorMap>();

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

        modelBuilder.Entity<WorkflowZoneMapping>(entity =>
        {
            // Each workflow can only be mapped to one zone
            entity.HasIndex(e => e.ExternalWorkflowId).IsUnique();
            // Index for querying by zone
            entity.HasIndex(e => e.ZoneCode);
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

        modelBuilder.Entity<Page>(entity =>
        {
            entity.ToTable("Pages");
            entity.HasIndex(e => e.PagePath).IsUnique();
            entity.Property(e => e.PagePath).HasMaxLength(255).IsRequired();
            entity.Property(e => e.PageName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.PageIcon).HasMaxLength(50);
            entity.Property(e => e.CreatedUtc).HasColumnType("datetime2");
        });

        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.ToTable("RolePermissions");
            // Composite unique index: one permission entry per role-page combination
            entity.HasIndex(e => new { e.RoleId, e.PageId }).IsUnique();
            // Index for querying all permissions for a specific role
            entity.HasIndex(e => e.RoleId);
            // Index for querying all roles that have access to a specific page
            entity.HasIndex(e => e.PageId);
            entity.Property(e => e.CreatedUtc).HasColumnType("datetime2");
        });

        modelBuilder.Entity<UserPermission>(entity =>
        {
            entity.ToTable("UserPermissions");
            // Composite unique index: one permission entry per user-page combination
            entity.HasIndex(e => new { e.UserId, e.PageId }).IsUnique();
            // Index for querying all permissions for a specific user
            entity.HasIndex(e => e.UserId);
            // Index for querying all users that have access to a specific page
            entity.HasIndex(e => e.PageId);
            entity.Property(e => e.CreatedUtc).HasColumnType("datetime2");
        });

        modelBuilder.Entity<RoleTemplatePermission>(entity =>
        {
            entity.ToTable("RoleTemplatePermissions");
            // Composite unique index: one permission entry per role-template combination
            entity.HasIndex(e => new { e.RoleId, e.SavedCustomMissionId }).IsUnique();
            // Index for querying all permissions for a specific role
            entity.HasIndex(e => e.RoleId);
            // Index for querying all roles that have access to a specific template
            entity.HasIndex(e => e.SavedCustomMissionId);
            entity.Property(e => e.CreatedUtc).HasColumnType("datetime2");
        });

        modelBuilder.Entity<UserTemplatePermission>(entity =>
        {
            entity.ToTable("UserTemplatePermissions");
            // Composite unique index: one permission entry per user-template combination
            entity.HasIndex(e => new { e.UserId, e.SavedCustomMissionId }).IsUnique();
            // Index for querying all permissions for a specific user
            entity.HasIndex(e => e.UserId);
            // Index for querying all users that have access to a specific template
            entity.HasIndex(e => e.SavedCustomMissionId);
            entity.Property(e => e.CreatedUtc).HasColumnType("datetime2");
        });

        modelBuilder.Entity<SystemSetting>(entity =>
        {
            entity.ToTable("SystemSetting");
            entity.HasIndex(e => e.Key).IsUnique();
            entity.Property(e => e.Key).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Value).HasMaxLength(255).IsRequired();
            entity.Property(e => e.LastUpdated).HasColumnType("datetime2");
        });

        modelBuilder.Entity<MissionQueue>(entity =>
        {
            entity.ToTable("MissionQueues");
            // Unique index on MissionCode
            entity.HasIndex(e => e.MissionCode).IsUnique();
            // Index for querying queued items by status and priority
            entity.HasIndex(e => new { e.Status, e.Priority, e.CreatedUtc });
            // Index for querying by SavedMissionId
            entity.HasIndex(e => e.SavedMissionId);
            // Index for querying by assigned robot
            entity.HasIndex(e => e.AssignedRobotId);
            // JSON column for mission request payload
            entity.Property(e => e.MissionRequestJson).HasColumnType("nvarchar(max)");
            // DateTime columns
            entity.Property(e => e.CreatedUtc).HasColumnType("datetime2");
            entity.Property(e => e.ProcessingStartedUtc).HasColumnType("datetime2");
            entity.Property(e => e.AssignedUtc).HasColumnType("datetime2");
            entity.Property(e => e.CompletedUtc).HasColumnType("datetime2");
        });

        modelBuilder.Entity<WorkflowSchedule>(entity =>
        {
            entity.ToTable("WorkflowSchedules");
            // Index for scheduler queries: find enabled schedules due to run
            entity.HasIndex(e => new { e.IsEnabled, e.NextRunUtc });
            // Index for querying schedules by SavedMissionId
            entity.HasIndex(e => e.SavedMissionId);
            // Foreign key relationship with cascade delete
            entity.HasOne(e => e.SavedMission)
                .WithMany()
                .HasForeignKey(e => e.SavedMissionId)
                .OnDelete(DeleteBehavior.Cascade);
            // DateTime columns
            entity.Property(e => e.OneTimeUtc).HasColumnType("datetime2");
            entity.Property(e => e.NextRunUtc).HasColumnType("datetime2");
            entity.Property(e => e.LastRunUtc).HasColumnType("datetime2");
            entity.Property(e => e.CreatedUtc).HasColumnType("datetime2");
            entity.Property(e => e.UpdatedUtc).HasColumnType("datetime2");
        });

        modelBuilder.Entity<MapEdge>(entity =>
        {
            entity.ToTable("MapEdges");
            entity.Property(e => e.CreateTime).HasColumnType("datetime2");
            entity.Property(e => e.LastUpdateTime).HasColumnType("datetime2");
            // Index for floor queries
            entity.HasIndex(e => new { e.MapCode, e.FloorNumber });
            // Unique edge: begin + end + mapCode
            entity.HasIndex(e => new { e.BeginNodeLabel, e.EndNodeLabel, e.MapCode }).IsUnique();
        });

        modelBuilder.Entity<FloorMap>(entity =>
        {
            entity.ToTable("FloorMaps");
            entity.Property(e => e.CreateTime).HasColumnType("datetime2");
            entity.Property(e => e.LastUpdateTime).HasColumnType("datetime2");
            // Unique floor per map
            entity.HasIndex(e => new { e.MapCode, e.FloorNumber }).IsUnique();
        });
    }
}
