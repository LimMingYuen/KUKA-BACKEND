using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using QES_KUKA_AMR_API.Models;
using QES_KUKA_AMR_API.Models.MissionTypes;

namespace QES_KUKA_AMR_API.Tests;

public class MissionTypesControllerTests : IClassFixture<MissionTypeApiFactory>
{
    private readonly MissionTypeApiFactory _factory;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public MissionTypesControllerTests(MissionTypeApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetMissionTypes_ShouldReturnEmptyList_WhenNoMissionTypesExist()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/mission-types");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<List<MissionTypeDto>>>(_jsonOptions);
        payload.Should().NotBeNull();
        payload!.Success.Should().BeTrue();
        payload.Data.Should().NotBeNull();
        payload.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task PostMissionType_ShouldCreateMissionType_AndReturnCreatedResult()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreateClient();

        var request = new MissionTypeCreateRequest
        {
            DisplayName = "Rack Move",
            ActualValue = "rack_move",
            Description = "Moves racks between storage zones",
            IsActive = true
        };

        var response = await client.PostAsJsonAsync("/api/v1/mission-types", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<MissionTypeDto>>(_jsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeTrue();
        envelope.Data.Should().NotBeNull();
        envelope.Data!.ActualValue.Should().Be("RACK_MOVE");
        envelope.Data.DisplayName.Should().Be("Rack Move");

        var listResponse = await client.GetAsync("/api/v1/mission-types");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var listEnvelope = await listResponse.Content.ReadFromJsonAsync<ApiResponse<List<MissionTypeDto>>>(_jsonOptions);
        listEnvelope.Should().NotBeNull();
        listEnvelope!.Data.Should().ContainSingle(mt => mt.ActualValue == "RACK_MOVE");
    }

    [Fact]
    public async Task PostMissionType_ShouldReturnConflict_WhenActualValueAlreadyExists()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreateClient();

        var request = new MissionTypeCreateRequest
        {
            DisplayName = "Rack Move",
            ActualValue = "rack_move",
            Description = "Initial mission type",
            IsActive = true
        };

        var firstResponse = await client.PostAsJsonAsync("/api/v1/mission-types", request);
        firstResponse.EnsureSuccessStatusCode();

        var conflictResponse = await client.PostAsJsonAsync("/api/v1/mission-types", new MissionTypeCreateRequest
        {
            DisplayName = "Rack Move Duplicate",
            ActualValue = "RACK_MOVE",
            IsActive = true
        });

        conflictResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await conflictResponse.Content.ReadFromJsonAsync<ProblemDetails>(_jsonOptions);
        problem.Should().NotBeNull();
        problem!.Detail.Should().Contain("already exists");
    }
}
