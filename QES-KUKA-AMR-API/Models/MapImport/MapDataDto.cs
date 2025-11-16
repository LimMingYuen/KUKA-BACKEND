namespace QES_KUKA_AMR_API.Models.MapImport;

/// <summary>
/// Root structure for KUKA AMR map JSON data
/// </summary>
public class MapDataDto
{
    public string MapCode { get; set; } = string.Empty;
    public List<FloorDto> FloorList { get; set; } = new();
}

/// <summary>
/// Floor level information containing nodes and edges
/// </summary>
public class FloorDto
{
    public int FloorLevel { get; set; }
    public string FloorName { get; set; } = string.Empty;
    public string FloorNumber { get; set; } = string.Empty;
    public double FloorLength { get; set; }
    public double FloorWidth { get; set; }
    public string FloorMapVersion { get; set; } = string.Empty;
    public int LaserMapId { get; set; }
    public List<NodeDto> NodeList { get; set; } = new();
    public List<EdgeDto>? EdgeList { get; set; }
}

/// <summary>
/// Node/QR code position with navigation parameters
/// </summary>
public class NodeDto
{
    public string NodeLabel { get; set; } = string.Empty;
    public int NodeNumber { get; set; }
    public string NodeUuid { get; set; } = string.Empty;
    public int NodeType { get; set; }
    public double XCoordinate { get; set; }
    public double YCoordinate { get; set; }

    // Navigation parameters
    public double DistanceAccuracy { get; set; }
    public double GoalDistanceAccuracy { get; set; }
    public int AngularAccuracy { get; set; }
    public int GoalAngularAccuracy { get; set; }
    public string TransitOrientations { get; set; } = string.Empty;

    // Optional parameters
    public double NodeLength { get; set; }
    public double NodeWidth { get; set; }
    public int AngularOffset { get; set; }
    public int IdleParkingSupport { get; set; }
    public int IsProtected { get; set; }
    public int OpportunityCharging { get; set; }
    public int RotationType { get; set; }
    public int SingleChannelId { get; set; }

    // Velocity and acceleration limits
    public double MaxAngularVelocity { get; set; }
    public double MaxAngularAcceleration { get; set; }
    public double MaxAngularDeceleration { get; set; }
    public double MaxRecoverAngle { get; set; }
    public double MaxRecoverDistance { get; set; }

    // Additional fields
    public string ForeignCode { get; set; } = string.Empty;
    public string ContainerStopAngle { get; set; } = string.Empty;
    public string RobotStopAngle { get; set; } = string.Empty;
    public string SpecialConfig { get; set; } = string.Empty;
    public string CustomerUi { get; set; } = string.Empty;

    public List<NodeFunctionDto>? FunctionList { get; set; }
}

/// <summary>
/// Node function configuration (charging, container handling, etc.)
/// </summary>
public class NodeFunctionDto
{
    public int FunctionType { get; set; }
    public int FunctionSwitch { get; set; }
    public string ContainerModel { get; set; } = string.Empty;
    public string RobotTypeCode { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
    public string DeviceMac { get; set; } = string.Empty;
    public string EcsAddress { get; set; } = string.Empty;
    public string EcsInterface { get; set; } = string.Empty;
    public string FunctionConfig { get; set; } = string.Empty;
    public double Angle { get; set; }
    public int SerialNumber { get; set; }
    public string TemplateCode { get; set; } = string.Empty;
    public string RelativeNodeLabel { get; set; } = string.Empty;
}

/// <summary>
/// Edge/path connection between nodes
/// </summary>
public class EdgeDto
{
    public string BeginNodeLabel { get; set; } = string.Empty;
    public string EndNodeLabel { get; set; } = string.Empty;
    public double EdgeLength { get; set; }
    public int EdgeType { get; set; }
    public double EdgeWeight { get; set; }
    public double EdgeWidth { get; set; }
    public double MaxVelocity { get; set; }
    public double MaxAccelerationVelocity { get; set; }
    public double MaxDecelerationVelocity { get; set; }
    public int Orientation { get; set; }
    public double Radius { get; set; }
    public string RoadType { get; set; } = string.Empty;
    public int Status { get; set; }
}
