namespace QES_KUKA_AMR_API.Models.MapImport;

/// <summary>
/// Request to import map data from JSON file
/// </summary>
public class MapImportRequest
{
    /// <summary>
    /// Full path to the map JSON file
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Whether to overwrite existing QR codes with the same NodeLabel and MapCode
    /// Default: false (skip existing)
    /// </summary>
    public bool OverwriteExisting { get; set; } = false;
}

/// <summary>
/// Response from map import operation
/// </summary>
public class MapImportResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public MapImportStats Stats { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}

/// <summary>
/// Statistics from import operation
/// </summary>
public class MapImportStats
{
    public string MapCode { get; set; } = string.Empty;
    public int TotalNodesInFile { get; set; }
    public int NodesImported { get; set; }
    public int NodesUpdated { get; set; }
    public int NodesSkipped { get; set; }
    public int NodesFailed { get; set; }
    public int TotalEdgesInFile { get; set; }
    public int EdgesImported { get; set; }
    public int FloorsImported { get; set; }
    public DateTime ImportedAt { get; set; }
}
