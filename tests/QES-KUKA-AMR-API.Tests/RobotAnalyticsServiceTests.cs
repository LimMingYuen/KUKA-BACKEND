using System.Linq;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using QES_KUKA_AMR_API.Data;
using QES_KUKA_AMR_API.Data.Entities;
using QES_KUKA_AMR_API.Models.Analytics;
using QES_KUKA_AMR_API.Services.Analytics;

namespace QES_KUKA_AMR_API.Tests;

public class RobotAnalyticsServiceTests
{
    [Fact]
    public async Task GetUtilizationAsync_NoMissions_ReturnsZeroMetrics()
    {
        using var context = CreateContext(nameof(GetUtilizationAsync_NoMissions_ReturnsZeroMetrics));
        var service = new RobotAnalyticsService(context, NullLogger<RobotAnalyticsService>.Instance);

        var start = DateTime.UtcNow.Date.AddDays(-1);
        var end = start.AddDays(1);

        var result = await service.GetUtilizationAsync("R-01", start, end, UtilizationGroupingInterval.Day);

        result.RobotId.Should().Be("R-01");
        result.UtilizedMinutes.Should().Be(0);
        result.ManualPauseMinutes.Should().Be(0);
        result.UtilizationPercent.Should().Be(0);
        result.Missions.Should().BeEmpty();
        result.Breakdown.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUtilizationAsync_SubtractsManualPauseFromMissionDuration()
    {
        using var context = CreateContext(nameof(GetUtilizationAsync_SubtractsManualPauseFromMissionDuration));

        var missionStart = DateTime.UtcNow.Date.AddHours(8);
        var missionEnd = missionStart.AddHours(4);

        var mission = new MissionQueue
        {
            MissionCode = "M-1001",
            WorkflowId = 1,
            WorkflowCode = "WF-001",
            WorkflowName = "Transfer",
            CreatedDate = missionStart.AddMinutes(-5),
            ProcessedDate = missionStart,
            CompletedDate = missionEnd,
            RequestId = "REQ-1",
            CreatedBy = "tester",
            Status = QueueStatus.Completed,
            Priority = 5,
            MissionDataJson = "{}",
            AssignedRobotId = "R-99"
        };

        var manualPauseStart = missionStart.AddHours(1);
        var manualPauseEnd = manualPauseStart.AddMinutes(30);

        var manualPause = new RobotManualPause
        {
            RobotId = "R-99",
            MissionCode = mission.MissionCode,
            PauseStartUtc = manualPauseStart,
            PauseEndUtc = manualPauseEnd,
            CreatedUtc = manualPauseStart,
            UpdatedUtc = manualPauseEnd,
            WaypointCode = "WP-01"
        };

        context.MissionQueues.Add(mission);
        context.RobotManualPauses.Add(manualPause);
        context.SaveChanges();

        var service = new RobotAnalyticsService(context, NullLogger<RobotAnalyticsService>.Instance);

        var start = missionStart.AddHours(-1);
        var end = missionEnd.AddHours(1);

        var result = await service.GetUtilizationAsync("R-99", start, end, UtilizationGroupingInterval.Hour);

        var expectedMissionMinutes = (missionEnd - missionStart).TotalMinutes;
        var expectedManualMinutes = (manualPauseEnd - manualPauseStart).TotalMinutes;

        result.UtilizedMinutes.Should().BeApproximately(expectedMissionMinutes - expectedManualMinutes, precision: 0.01);
        result.ManualPauseMinutes.Should().BeApproximately(expectedManualMinutes, precision: 0.01);
        result.Missions.Should().ContainSingle(m => m.MissionCode == mission.MissionCode);
        result.Missions[0].ManualPauseMinutes.Should().BeApproximately(expectedManualMinutes, 0.01);
        result.Breakdown.Should().NotBeEmpty();

        result.Breakdown.Sum(b => b.UtilizedMinutes).Should().BeApproximately(result.UtilizedMinutes, 0.01);
        result.Breakdown.Sum(b => b.ManualPauseMinutes).Should().BeApproximately(result.ManualPauseMinutes, 0.01);
    }

    private static ApplicationDbContext CreateContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        return new ApplicationDbContext(options);
    }
}
