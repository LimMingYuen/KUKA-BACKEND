using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QES_KUKA_AMR_API.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeedMapCodeQueueConfigurations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Seed MapCodeQueueConfigurations for all unique MapCodes from WorkflowDiagrams
            // This ensures the QueueScheduler can process missions
            migrationBuilder.Sql(@"
                -- Insert configurations for all unique MapCodes from WorkflowDiagrams
                INSERT INTO MapCodeQueueConfigurations
                (
                    MapCode,
                    EnableQueue,
                    DefaultPriority,
                    MaxConsecutiveOpportunisticJobs,
                    EnableCrossMapOptimization,
                    MaxConcurrentRobotsOnMap,
                    QueueProcessingIntervalSeconds,
                    TotalJobsProcessed,
                    OpportunisticJobsChained,
                    AverageJobWaitTimeSeconds,
                    AverageOpportunisticJobDistanceMeters,
                    CreatedUtc,
                    UpdatedUtc
                )
                SELECT DISTINCT
                    w.MapCode,
                    1,  -- EnableQueue = true
                    5,  -- DefaultPriority
                    1,  -- MaxConsecutiveOpportunisticJobs
                    1,  -- EnableCrossMapOptimization
                    10, -- MaxConcurrentRobotsOnMap
                    5,  -- QueueProcessingIntervalSeconds
                    0,  -- TotalJobsProcessed (initial stats)
                    0,  -- OpportunisticJobsChained (initial stats)
                    0,  -- AverageJobWaitTimeSeconds (initial stats)
                    0,  -- AverageOpportunisticJobDistanceMeters (initial stats)
                    GETUTCDATE(),
                    GETUTCDATE()
                FROM WorkflowDiagrams w
                WHERE w.MapCode IS NOT NULL
                  AND w.MapCode <> ''
                  AND NOT EXISTS (
                      SELECT 1
                      FROM MapCodeQueueConfigurations c
                      WHERE c.MapCode = w.MapCode
                  );

                -- Also ensure 'Sim1' exists (common simulator MapCode)
                IF NOT EXISTS (SELECT 1 FROM MapCodeQueueConfigurations WHERE MapCode = 'Sim1')
                BEGIN
                    INSERT INTO MapCodeQueueConfigurations
                    (
                        MapCode,
                        EnableQueue,
                        DefaultPriority,
                        MaxConsecutiveOpportunisticJobs,
                        EnableCrossMapOptimization,
                        MaxConcurrentRobotsOnMap,
                        QueueProcessingIntervalSeconds,
                        TotalJobsProcessed,
                        OpportunisticJobsChained,
                        AverageJobWaitTimeSeconds,
                        AverageOpportunisticJobDistanceMeters,
                        CreatedUtc,
                        UpdatedUtc
                    )
                    VALUES
                    (
                        'Sim1',
                        1,  -- EnableQueue
                        5,  -- DefaultPriority
                        1,  -- MaxConsecutiveOpportunisticJobs
                        1,  -- EnableCrossMapOptimization
                        10, -- MaxConcurrentRobotsOnMap
                        5,  -- QueueProcessingIntervalSeconds
                        0, 0, 0, 0,  -- Initial statistics
                        GETUTCDATE(),
                        GETUTCDATE()
                    );
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove all seeded configurations
            migrationBuilder.Sql(@"
                DELETE FROM MapCodeQueueConfigurations
                WHERE MapCode IN (
                    SELECT DISTINCT MapCode
                    FROM WorkflowDiagrams
                    WHERE MapCode IS NOT NULL AND MapCode <> ''
                )
                OR MapCode = 'Sim1';
            ");
        }
    }
}
