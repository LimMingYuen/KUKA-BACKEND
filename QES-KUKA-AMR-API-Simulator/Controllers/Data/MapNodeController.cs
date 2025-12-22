using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QES_KUKA_AMR_API_Simulator.Models.MapNode;

namespace QES_KUKA_AMR_API_Simulator.Controllers.Data;

/// <summary>
/// Simulator controller for map node data (mimics external WCS API)
/// This API does not require authentication
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("wcs/api/map")]
public class MapNodeController : ControllerBase
{
    // Static map node data with coordinates matching existing QR codes
    private static readonly IReadOnlyList<MapNodeDto> MapNodes = new[]
    {
        // Numbered nodes
        new MapNodeDto { CellCode = "Sim1-1-2", MapCode = "Sim1", FloorNumber = "1", NodeLabel = "2", X = "16478.0", Y = "54598.0", NodeId = 33, NodeFunctionType = 4, MapNodeNumber = 2 },
        new MapNodeDto { CellCode = "Sim1-1-3", MapCode = "Sim1", FloorNumber = "1", NodeLabel = "3", X = "16478.0", Y = "57123.0", NodeId = 34, NodeFunctionType = 4, MapNodeNumber = 3 },
        new MapNodeDto { CellCode = "Sim1-1-4", MapCode = "Sim1", FloorNumber = "1", NodeLabel = "4", X = "16478.0", Y = "59648.0", NodeId = 35, NodeFunctionType = 4, MapNodeNumber = 4 },
        new MapNodeDto { CellCode = "Sim1-1-5", MapCode = "Sim1", FloorNumber = "1", NodeLabel = "5", X = "16478.0", Y = "62173.0", NodeId = 36, NodeFunctionType = 4, MapNodeNumber = 5 },
        new MapNodeDto { CellCode = "Sim1-1-6", MapCode = "Sim1", FloorNumber = "1", NodeLabel = "6", X = "16478.0", Y = "64698.0", NodeId = 37, NodeFunctionType = 4, MapNodeNumber = 6 },
        new MapNodeDto { CellCode = "Sim1-1-8", MapCode = "Sim1", FloorNumber = "1", NodeLabel = "8", X = "16478.0", Y = "69748.0", NodeId = 38, NodeFunctionType = 4, MapNodeNumber = 8 },
        new MapNodeDto { CellCode = "Sim1-1-9", MapCode = "Sim1", FloorNumber = "1", NodeLabel = "9", X = "16478.0", Y = "72273.0", NodeId = 39, NodeFunctionType = 4, MapNodeNumber = 9 },
        new MapNodeDto { CellCode = "Sim1-1-10", MapCode = "Sim1", FloorNumber = "1", NodeLabel = "10", X = "16478.0", Y = "74798.0", NodeId = 40, NodeFunctionType = 4, MapNodeNumber = 10 },
        new MapNodeDto { CellCode = "Sim1-1-11", MapCode = "Sim1", FloorNumber = "1", NodeLabel = "11", X = "16478.0", Y = "77323.0", NodeId = 41, NodeFunctionType = 4, MapNodeNumber = 11 },
        new MapNodeDto { CellCode = "Sim1-1-15", MapCode = "Sim1", FloorNumber = "1", NodeLabel = "15", X = "13953.0", Y = "52073.0", NodeId = 42, NodeFunctionType = 4, MapNodeNumber = 15 },
        new MapNodeDto { CellCode = "Sim1-1-16", MapCode = "Sim1", FloorNumber = "1", NodeLabel = "16", X = "13953.0", Y = "54598.0", NodeId = 43, NodeFunctionType = 4, MapNodeNumber = 16 },
        new MapNodeDto { CellCode = "Sim1-1-17", MapCode = "Sim1", FloorNumber = "1", NodeLabel = "17", X = "13953.0", Y = "57123.0", NodeId = 44, NodeFunctionType = 4, MapNodeNumber = 17 },
        new MapNodeDto { CellCode = "Sim1-1-18", MapCode = "Sim1", FloorNumber = "1", NodeLabel = "18", X = "13953.0", Y = "59648.0", NodeId = 45, NodeFunctionType = 4, MapNodeNumber = 18 },
        new MapNodeDto { CellCode = "Sim1-1-21", MapCode = "Sim1", FloorNumber = "1", NodeLabel = "21", X = "13953.0", Y = "67223.0", NodeId = 46, NodeFunctionType = 4, MapNodeNumber = 21 },
        new MapNodeDto { CellCode = "Sim1-1-26", MapCode = "Sim1", FloorNumber = "1", NodeLabel = "26", X = "13953.0", Y = "79848.0", NodeId = 47, NodeFunctionType = 4, MapNodeNumber = 26 },
        new MapNodeDto { CellCode = "Sim1-1-29", MapCode = "Sim1", FloorNumber = "1", NodeLabel = "29", X = "13953.0", Y = "87423.0", NodeId = 48, NodeFunctionType = 4, MapNodeNumber = 29 },
        new MapNodeDto { CellCode = "Sim1-1-30", MapCode = "Sim1", FloorNumber = "1", NodeLabel = "30", X = "13953.0", Y = "89948.0", NodeId = 49, NodeFunctionType = 4, MapNodeNumber = 30 },
        new MapNodeDto { CellCode = "Sim1-1-31", MapCode = "Sim1", FloorNumber = "1", NodeLabel = "31", X = "13953.0", Y = "92473.0", NodeId = 50, NodeFunctionType = 4, MapNodeNumber = 31 },
        new MapNodeDto { CellCode = "Sim1-1-32", MapCode = "Sim1", FloorNumber = "1", NodeLabel = "32", X = "13953.0", Y = "94998.0", NodeId = 51, NodeFunctionType = 4, MapNodeNumber = 32 },
        new MapNodeDto { CellCode = "Sim1-1-33", MapCode = "Sim1", FloorNumber = "1", NodeLabel = "33", X = "13953.0", Y = "97523.0", NodeId = 52, NodeFunctionType = 4, MapNodeNumber = 33 },
        new MapNodeDto { CellCode = "Sim1-1-34", MapCode = "Sim1", FloorNumber = "1", NodeLabel = "34", X = "11428.0", Y = "49548.0", NodeId = 53, NodeFunctionType = 4, MapNodeNumber = 34 },
        new MapNodeDto { CellCode = "Sim1-1-35", MapCode = "Sim1", FloorNumber = "1", NodeLabel = "35", X = "11428.0", Y = "52073.0", NodeId = 54, NodeFunctionType = 4, MapNodeNumber = 35 },
        new MapNodeDto { CellCode = "Sim1-1-36", MapCode = "Sim1", FloorNumber = "1", NodeLabel = "36", X = "11428.0", Y = "54598.0", NodeId = 55, NodeFunctionType = 4, MapNodeNumber = 36 },
        new MapNodeDto { CellCode = "Sim1-1-37", MapCode = "Sim1", FloorNumber = "1", NodeLabel = "37", X = "11428.0", Y = "57123.0", NodeId = 56, NodeFunctionType = 4, MapNodeNumber = 37 },
        new MapNodeDto { CellCode = "Sim1-1-38", MapCode = "Sim1", FloorNumber = "1", NodeLabel = "38", X = "11428.0", Y = "59648.0", NodeId = 57, NodeFunctionType = 4, MapNodeNumber = 38 },
    };

    /// <summary>
    /// Get all map nodes with coordinates
    /// This endpoint mimics the external WCS API: GET /wcs/api/map/queryAllMapNode
    /// </summary>
    [HttpGet("queryAllMapNode")]
    public ActionResult<MapNodeApiResponse> GetAllMapNodes()
    {
        return Ok(new MapNodeApiResponse
        {
            Code = 0,
            Msg = "SUCCESS",
            Data = MapNodes.ToList(),
            Succ = true
        });
    }
}
