using System.Net;

namespace QES_KUKA_AMR_API.Services;

public readonly record struct SimulatorResponse<T>(HttpStatusCode StatusCode, T? Body);
