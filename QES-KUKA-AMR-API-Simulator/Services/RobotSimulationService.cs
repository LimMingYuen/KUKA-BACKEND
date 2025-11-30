using QES_KUKA_AMR_API_Simulator.Models.MobileRobot;
using QES_KUKA_AMR_API_Simulator.Models.Missions;

namespace QES_KUKA_AMR_API_Simulator.Services;

/// <summary>
/// Service that manages simulated robot positions based on active jobs
/// </summary>
public class RobotSimulationService
{
    private readonly ILogger<RobotSimulationService> _logger;
    private readonly object _lock = new();

    // Robot positions keyed by RobotId
    private readonly Dictionary<string, SimulatedRobotState> _robotStates = new();

    // Node coordinates lookup (nodeUuid -> coordinates)
    private readonly Dictionary<string, (double X, double Y)> _nodeCoordinates = new();

    // Default robot positions (when idle)
    private readonly Dictionary<string, (double X, double Y)> _defaultPositions = new()
    {
        { "1001", (51.135, 32.231) },
        { "1003", (19.003, 32.231) },
        { "1005", (22.251, 63.943) }
    };

    public RobotSimulationService(ILogger<RobotSimulationService> logger)
    {
        _logger = logger;
        InitializeRobots();
        InitializeNodeCoordinates();
    }

    private void InitializeRobots()
    {
        foreach (var (robotId, pos) in _defaultPositions)
        {
            _robotStates[robotId] = new SimulatedRobotState
            {
                RobotId = robotId,
                XCoordinate = pos.X,
                YCoordinate = pos.Y,
                Status = 3, // Idle
                MapCode = "Sim1",
                FloorNumber = "1"
            };
        }
    }

    private void InitializeNodeCoordinates()
    {
        // Sample node coordinates from the map - add more as needed
        // These should match the imported map nodes
        _nodeCoordinates["Sim1-1-1"] = (19.003, 14.914);
        _nodeCoordinates["Sim1-1-2"] = (19.003, 23.572);
        _nodeCoordinates["Sim1-1-3"] = (19.003, 32.231);
        _nodeCoordinates["Sim1-1-4"] = (19.003, 40.89);
        _nodeCoordinates["Sim1-1-5"] = (19.003, 49.548);
        _nodeCoordinates["Sim1-1-6"] = (19.003, 58.206);
        _nodeCoordinates["Sim1-1-7"] = (27.036, 14.914);
        _nodeCoordinates["Sim1-1-8"] = (27.036, 23.572);
        _nodeCoordinates["Sim1-1-9"] = (27.036, 32.231);
        _nodeCoordinates["Sim1-1-10"] = (27.036, 40.89);
        _nodeCoordinates["Sim1-1-11"] = (27.036, 49.548);
        _nodeCoordinates["Sim1-1-12"] = (27.036, 58.206);
        _nodeCoordinates["Sim1-1-13"] = (35.166, 14.914);
        _nodeCoordinates["Sim1-1-14"] = (35.166, 23.572);
        _nodeCoordinates["Sim1-1-15"] = (35.166, 32.231);
        _nodeCoordinates["Sim1-1-16"] = (35.166, 40.89);
        _nodeCoordinates["Sim1-1-17"] = (35.166, 49.548);
        _nodeCoordinates["Sim1-1-18"] = (35.166, 58.206);
        _nodeCoordinates["Sim1-1-19"] = (43.2, 14.914);
        _nodeCoordinates["Sim1-1-20"] = (43.2, 23.572);
        _nodeCoordinates["Sim1-1-21"] = (43.2, 32.231);
        _nodeCoordinates["Sim1-1-22"] = (43.2, 40.89);
        _nodeCoordinates["Sim1-1-23"] = (43.2, 49.548);
        _nodeCoordinates["Sim1-1-24"] = (43.2, 58.206);
        _nodeCoordinates["Sim1-1-25"] = (51.135, 14.914);
        _nodeCoordinates["Sim1-1-26"] = (51.135, 23.572);
        _nodeCoordinates["Sim1-1-27"] = (51.135, 32.231);
        _nodeCoordinates["Sim1-1-28"] = (51.135, 40.89);
        _nodeCoordinates["Sim1-1-29"] = (51.135, 49.548);
        _nodeCoordinates["Sim1-1-30"] = (51.135, 58.206);
    }

    /// <summary>
    /// Start a job for a robot - moves robot along waypoints
    /// </summary>
    public void StartJob(string robotId, string missionCode, List<MissionDataItem>? missionSteps)
    {
        lock (_lock)
        {
            if (!_robotStates.TryGetValue(robotId, out var state))
            {
                state = new SimulatedRobotState
                {
                    RobotId = robotId,
                    MapCode = "Sim1",
                    FloorNumber = "1"
                };
                _robotStates[robotId] = state;
            }

            state.MissionCode = missionCode;
            state.Status = 20; // Executing
            state.MissionSteps = missionSteps ?? new List<MissionDataItem>();
            state.CurrentStepIndex = 0;
            state.StepStartTime = DateTime.UtcNow;
            state.IsActive = true;

            _logger.LogInformation(
                "Started job {MissionCode} for robot {RobotId} with {StepCount} steps",
                missionCode, robotId, state.MissionSteps.Count);

            // Move to first waypoint if available
            if (state.MissionSteps.Count > 0)
            {
                var firstStep = state.MissionSteps[0];
                UpdateRobotPosition(state, firstStep.Position);
            }
        }
    }

    /// <summary>
    /// Complete or cancel a job
    /// </summary>
    public void EndJob(string robotId, bool cancelled = false)
    {
        lock (_lock)
        {
            if (_robotStates.TryGetValue(robotId, out var state))
            {
                state.IsActive = false;
                state.MissionCode = "";
                state.Status = cancelled ? 31 : 3; // Cancelled or Idle
                state.MissionSteps = null;
                state.CurrentStepIndex = 0;

                // Return to default position
                if (_defaultPositions.TryGetValue(robotId, out var defaultPos))
                {
                    state.XCoordinate = defaultPos.X;
                    state.YCoordinate = defaultPos.Y;
                }

                _logger.LogInformation(
                    "Ended job for robot {RobotId}, cancelled={Cancelled}",
                    robotId, cancelled);
            }
        }
    }

    /// <summary>
    /// Update robot positions based on elapsed time (call periodically)
    /// </summary>
    public void UpdateSimulation()
    {
        lock (_lock)
        {
            foreach (var state in _robotStates.Values.Where(s => s.IsActive))
            {
                if (state.MissionSteps == null || state.MissionSteps.Count == 0)
                    continue;

                var elapsed = DateTime.UtcNow - state.StepStartTime;
                var travelTimePerStep = TimeSpan.FromSeconds(4); // 4 seconds per node

                // Check if we should move to next step
                if (elapsed >= travelTimePerStep)
                {
                    state.CurrentStepIndex++;
                    state.StepStartTime = DateTime.UtcNow;

                    if (state.CurrentStepIndex < state.MissionSteps.Count)
                    {
                        var nextStep = state.MissionSteps[state.CurrentStepIndex];
                        UpdateRobotPosition(state, nextStep.Position);

                        _logger.LogDebug(
                            "Robot {RobotId} moved to step {Step}: {Position}",
                            state.RobotId, state.CurrentStepIndex, nextStep.Position);
                    }
                    else
                    {
                        // Mission complete
                        state.IsActive = false;
                        state.Status = 3; // Idle
                        state.MissionCode = "";

                        _logger.LogInformation(
                            "Robot {RobotId} completed mission",
                            state.RobotId);
                    }
                }
                else
                {
                    // Interpolate position between steps
                    InterpolatePosition(state, elapsed, travelTimePerStep);
                }
            }
        }
    }

    private void UpdateRobotPosition(SimulatedRobotState state, string? nodeCode)
    {
        if (string.IsNullOrEmpty(nodeCode)) return;

        if (_nodeCoordinates.TryGetValue(nodeCode, out var coords))
        {
            state.XCoordinate = coords.X;
            state.YCoordinate = coords.Y;
            state.CurrentNodeCode = nodeCode;
        }
        else
        {
            _logger.LogWarning("Unknown node code: {NodeCode}", nodeCode);
        }
    }

    private void InterpolatePosition(SimulatedRobotState state, TimeSpan elapsed, TimeSpan totalTime)
    {
        if (state.MissionSteps == null || state.CurrentStepIndex >= state.MissionSteps.Count)
            return;

        var progress = Math.Min(1.0, elapsed.TotalMilliseconds / totalTime.TotalMilliseconds);

        // Get current and next positions
        var currentPos = (state.XCoordinate, state.YCoordinate);

        var nextStepIndex = Math.Min(state.CurrentStepIndex + 1, state.MissionSteps.Count - 1);
        var nextNodeCode = state.MissionSteps[nextStepIndex].Position;

        if (!string.IsNullOrEmpty(nextNodeCode) && _nodeCoordinates.TryGetValue(nextNodeCode, out var nextPos))
        {
            // Linear interpolation
            state.XCoordinate = currentPos.Item1 + (nextPos.X - currentPos.Item1) * progress;
            state.YCoordinate = currentPos.Item2 + (nextPos.Y - currentPos.Item2) * progress;
        }
    }

    /// <summary>
    /// Get all robot realtime data
    /// </summary>
    public List<RobotRealtimeDto> GetRobotRealtimeList(string? mapCode = null, string? floorNumber = null)
    {
        lock (_lock)
        {
            UpdateSimulation(); // Update positions before returning

            var robots = _robotStates.Values.AsEnumerable();

            if (!string.IsNullOrEmpty(mapCode))
                robots = robots.Where(r => r.MapCode == mapCode);

            if (!string.IsNullOrEmpty(floorNumber))
                robots = robots.Where(r => r.FloorNumber == floorNumber);

            return robots.Select(r => new RobotRealtimeDto
            {
                Id = int.TryParse(r.RobotId, out var id) ? id : 0,
                RobotId = r.RobotId,
                XCoordinate = r.XCoordinate,
                YCoordinate = r.YCoordinate,
                RobotStatus = r.Status,
                MapCode = r.MapCode ?? "Sim1",
                FloorNumber = r.FloorNumber ?? "1",
                MissionCode = r.MissionCode ?? "",
                BatteryLevel = 0.85,
                ConnectionState = 1,
                RobotTypeCode = "KMP 400i diffDrive",
                WarningLevel = 0,
                WarningMessage = r.IsActive ? "Executing mission" : "Idle"
            }).ToList();
        }
    }

    /// <summary>
    /// Get robot state by ID
    /// </summary>
    public SimulatedRobotState? GetRobotState(string robotId)
    {
        lock (_lock)
        {
            return _robotStates.TryGetValue(robotId, out var state) ? state : null;
        }
    }
}

/// <summary>
/// State of a simulated robot
/// </summary>
public class SimulatedRobotState
{
    public string RobotId { get; set; } = "";
    public double XCoordinate { get; set; }
    public double YCoordinate { get; set; }
    public int Status { get; set; } = 3; // 3=Idle, 20=Executing
    public string? MapCode { get; set; } = "Sim1";
    public string? FloorNumber { get; set; } = "1";
    public string? MissionCode { get; set; }
    public string? CurrentNodeCode { get; set; }

    // Mission execution state
    public bool IsActive { get; set; }
    public List<MissionDataItem>? MissionSteps { get; set; }
    public int CurrentStepIndex { get; set; }
    public DateTime StepStartTime { get; set; }
}
