using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using QES_KUKA_AMR_API.Data;
using QES_KUKA_AMR_API.Data.Entities;
using QES_KUKA_AMR_API.Services.MissionTypes;

namespace QES_KUKA_AMR_API.Tests;

public class MissionTypeServiceTests
{
    [Fact]
    public async Task CreateAsync_ShouldPersistMissionType_WhenActualValueIsUnique()
    {
        await using var context = CreateDbContext();
        var service = new MissionTypeService(context, NullLogger<MissionTypeService>.Instance);

        var result = await service.CreateAsync(new MissionType
        {
            DisplayName = "Rack Move",
            ActualValue = "rack_move",
            Description = "Moves racks between zones",
            IsActive = true
        });

        result.Id.Should().BeGreaterThan(0);
        result.ActualValue.Should().Be("RACK_MOVE");

        var persisted = await context.MissionTypes.SingleAsync();
        persisted.DisplayName.Should().Be("Rack Move");
        persisted.ActualValue.Should().Be("RACK_MOVE");
        persisted.Description.Should().Be("Moves racks between zones");
        persisted.IsActive.Should().BeTrue();
        persisted.CreatedUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        persisted.UpdatedUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task CreateAsync_ShouldThrowConflict_WhenActualValueAlreadyExists()
    {
        await using var context = CreateDbContext();
        context.MissionTypes.Add(new MissionType
        {
            DisplayName = "Rack Move",
            ActualValue = "RACK_MOVE",
            IsActive = true,
            CreatedUtc = DateTime.UtcNow,
            UpdatedUtc = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var service = new MissionTypeService(context, NullLogger<MissionTypeService>.Instance);

        var act = async () => await service.CreateAsync(new MissionType
        {
            DisplayName = "Rack Move Copy",
            ActualValue = "rack move"
        });

        await act.Should().ThrowAsync<MissionTypeConflictException>();
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnNull_WhenMissionTypeDoesNotExist()
    {
        await using var context = CreateDbContext();
        var service = new MissionTypeService(context, NullLogger<MissionTypeService>.Instance);

        var result = await service.UpdateAsync(42, new MissionType
        {
            DisplayName = "Updated",
            ActualValue = "UPDATED"
        });

        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrowConflict_WhenActualValueTakenByAnotherRecord()
    {
        await using var context = CreateDbContext();
        context.MissionTypes.AddRange(
            new MissionType
            {
                DisplayName = "Primary",
                ActualValue = "PRIMARY",
                IsActive = true,
                CreatedUtc = DateTime.UtcNow,
                UpdatedUtc = DateTime.UtcNow
            },
            new MissionType
            {
                DisplayName = "Secondary",
                ActualValue = "SECONDARY",
                IsActive = true,
                CreatedUtc = DateTime.UtcNow,
                UpdatedUtc = DateTime.UtcNow
            });
        await context.SaveChangesAsync();

        var service = new MissionTypeService(context, NullLogger<MissionTypeService>.Instance);

        var act = async () => await service.UpdateAsync(
            2,
            new MissionType
            {
                DisplayName = "Secondary Updated",
                ActualValue = "primary"
            });

        await act.Should().ThrowAsync<MissionTypeConflictException>();
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveMissionType_WhenRecordExists()
    {
        await using var context = CreateDbContext();
        context.MissionTypes.Add(new MissionType
        {
            DisplayName = "Rack Move",
            ActualValue = "RACK_MOVE",
            IsActive = true,
            CreatedUtc = DateTime.UtcNow,
            UpdatedUtc = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var service = new MissionTypeService(context, NullLogger<MissionTypeService>.Instance);

        var deleted = await service.DeleteAsync(1);

        deleted.Should().BeTrue();
        (await context.MissionTypes.CountAsync()).Should().Be(0);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }
}
