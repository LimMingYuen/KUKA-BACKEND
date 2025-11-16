using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QES_KUKA_AMR_API.Data;
using QES_KUKA_AMR_API.Data.Entities;
using QES_KUKA_AMR_API.Models.Config;
using QES_KUKA_AMR_API.Options;
using QES_KUKA_AMR_API.Services.Auth;

namespace QES_KUKA_AMR_API.Services.WorkflowNodeCodes;

public class WorkflowNodeCodeService : IWorkflowNodeCodeService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IExternalApiTokenService _externalApiTokenService;
    private readonly ILogger<WorkflowNodeCodeService> _logger;
    private readonly TimeProvider _timeProvider;
    private readonly MissionServiceOptions _options;

    public WorkflowNodeCodeService(
        IServiceScopeFactory serviceScopeFactory,
        IHttpClientFactory httpClientFactory,
        IExternalApiTokenService externalApiTokenService,
        ILogger<WorkflowNodeCodeService> logger,
        TimeProvider timeProvider,
        IOptions<MissionServiceOptions> options)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _httpClientFactory = httpClientFactory;
        _externalApiTokenService = externalApiTokenService;
        _logger = logger;
        _timeProvider = timeProvider;
        _options = options.Value;
    }

    public async Task<WorkflowNodeCodeSyncResult> SyncAllWorkflowNodeCodesAsync(
        int maxConcurrency = 10,
        CancellationToken cancellationToken = default)
    {
        var result = new WorkflowNodeCodeSyncResult();

        // Get all DISTINCT external workflow IDs to avoid duplicate processing
        List<int> distinctExternalIds;
        using (var scope = _serviceScopeFactory.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            distinctExternalIds = await dbContext.WorkflowDiagrams
                .Where(w => w.ExternalWorkflowId != null)
                .Select(w => w.ExternalWorkflowId!.Value)
                .Distinct()
                .ToListAsync(cancellationToken);
        }

        result.TotalWorkflows = distinctExternalIds.Count;

        if (distinctExternalIds.Count == 0)
        {
            _logger.LogInformation("No workflows with external IDs found to sync");
            return result;
        }

        _logger.LogInformation("Starting sequential sync for {Count} unique external workflow IDs",
            distinctExternalIds.Count);

        // Process workflows one by one sequentially
        var successCount = 0;
        var failureCount = 0;
        var nodeCodesInserted = 0;
        var nodeCodesDeleted = 0;
        var failedWorkflowIds = new List<int>();
        var errors = new Dictionary<int, string>();

        for (int i = 0; i < distinctExternalIds.Count; i++)
        {
            var externalId = distinctExternalIds[i];

            try
            {
                _logger.LogInformation("Syncing workflow {Current}/{Total}: External ID {ExternalId}",
                    i + 1, distinctExternalIds.Count, externalId);

                // Create a new scope for this workflow to get its own DbContext
                using var scope = _serviceScopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var syncResult = await SyncWorkflowNodeCodesInternalAsync(
                    externalId,
                    dbContext,
                    cancellationToken);

                if (syncResult.Success)
                {
                    successCount++;
                    nodeCodesInserted += syncResult.NodeCodesInserted;
                    nodeCodesDeleted += syncResult.NodeCodesDeleted;
                    _logger.LogInformation("Successfully synced workflow {ExternalId}: {Inserted} inserted, {Deleted} deleted",
                        externalId, syncResult.NodeCodesInserted, syncResult.NodeCodesDeleted);
                }
                else
                {
                    failureCount++;
                    failedWorkflowIds.Add(externalId);
                    errors[externalId] = syncResult.ErrorMessage ?? "Unknown error";
                    _logger.LogWarning("Failed to sync external workflow ID {ExternalId}: {Error}",
                        externalId, syncResult.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                failureCount++;
                failedWorkflowIds.Add(externalId);
                var errorMsg = $"Exception: {ex.Message}";
                errors[externalId] = errorMsg;
                _logger.LogError(ex, "Exception while syncing external workflow ID {ExternalId}", externalId);
            }
        }

        result.SuccessCount = successCount;
        result.FailureCount = failureCount;
        result.NodeCodesInserted = nodeCodesInserted;
        result.NodeCodesDeleted = nodeCodesDeleted;
        result.FailedWorkflowIds = failedWorkflowIds;
        result.Errors = errors;

        _logger.LogInformation("Sync completed: {Total} workflows, {Success} succeeded, {Failed} failed, " +
            "{Inserted} node codes inserted, {Deleted} node codes deleted",
            result.TotalWorkflows, result.SuccessCount, result.FailureCount,
            result.NodeCodesInserted, result.NodeCodesDeleted);

        return result;
    }

    public async Task<bool> SyncWorkflowNodeCodesAsync(
        int externalWorkflowId,
        CancellationToken cancellationToken = default)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var result = await SyncWorkflowNodeCodesInternalAsync(externalWorkflowId, dbContext, cancellationToken);
        return result.Success;
    }

    public async Task<List<string>> GetWorkflowNodeCodesAsync(
        int externalWorkflowId,
        CancellationToken cancellationToken = default)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await dbContext.WorkflowNodeCodes
            .Where(wnc => wnc.ExternalWorkflowId == externalWorkflowId)
            .Select(wnc => wnc.NodeCode)
            .OrderBy(nc => nc)
            .ToListAsync(cancellationToken);
    }

    public async Task<WorkflowZoneClassification?> ClassifyWorkflowByZoneAsync(
        int externalWorkflowId,
        CancellationToken cancellationToken = default)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await ClassifyWorkflowByZoneInternalAsync(externalWorkflowId, dbContext, cancellationToken);
    }

    public async Task<WorkflowZoneClassification?> SyncAndClassifyWorkflowAsync(
        int externalWorkflowId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Sync and classify workflow {ExternalWorkflowId}", externalWorkflowId);

        using var scope = _serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Step 1: Sync node codes from external API
        var syncResult = await SyncWorkflowNodeCodesInternalAsync(externalWorkflowId, dbContext, cancellationToken);

        if (!syncResult.Success)
        {
            _logger.LogWarning("Sync failed for workflow {ExternalWorkflowId}: {Error}",
                externalWorkflowId, syncResult.ErrorMessage);
            return null;
        }

        _logger.LogInformation("Synced workflow {ExternalWorkflowId}: {Inserted} inserted, {Deleted} deleted",
            externalWorkflowId, syncResult.NodeCodesInserted, syncResult.NodeCodesDeleted);

        // Step 2: Classify by zone using the freshly synced data
        var classification = await ClassifyWorkflowByZoneInternalAsync(externalWorkflowId, dbContext, cancellationToken);

        if (classification == null)
        {
            _logger.LogWarning("Could not classify workflow {ExternalWorkflowId} to any zone", externalWorkflowId);
            return null;
        }

        // Step 3: Get workflow details from WorkflowDiagram
        var workflow = await dbContext.WorkflowDiagrams
            .FirstOrDefaultAsync(w => w.ExternalWorkflowId == externalWorkflowId, cancellationToken);

        if (workflow == null)
        {
            _logger.LogWarning("Workflow {ExternalWorkflowId} not found in WorkflowDiagrams table", externalWorkflowId);
            return classification;
        }

        // Step 4: Save or update WorkflowZoneMapping
        var existingMapping = await dbContext.WorkflowZoneMappings
            .FirstOrDefaultAsync(m => m.ExternalWorkflowId == externalWorkflowId, cancellationToken);

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        if (existingMapping != null)
        {
            // Update existing mapping
            existingMapping.WorkflowCode = workflow.WorkflowCode;
            existingMapping.WorkflowName = workflow.WorkflowName;
            existingMapping.ZoneName = classification.ZoneName;
            existingMapping.ZoneCode = classification.ZoneCode;
            existingMapping.MapCode = classification.MapCode;
            existingMapping.MatchedNodesCount = classification.MatchedNodesCount;
            existingMapping.UpdatedUtc = now;

            _logger.LogInformation("Updated zone mapping for workflow {ExternalWorkflowId} ({WorkflowCode}): {ZoneName} ({ZoneCode})",
                externalWorkflowId, workflow.WorkflowCode, classification.ZoneName, classification.ZoneCode);
        }
        else
        {
            // Create new mapping
            var newMapping = new WorkflowZoneMapping
            {
                ExternalWorkflowId = externalWorkflowId,
                WorkflowCode = workflow.WorkflowCode,
                WorkflowName = workflow.WorkflowName,
                ZoneName = classification.ZoneName,
                ZoneCode = classification.ZoneCode,
                MapCode = classification.MapCode,
                MatchedNodesCount = classification.MatchedNodesCount,
                CreatedUtc = now,
                UpdatedUtc = now
            };

            dbContext.WorkflowZoneMappings.Add(newMapping);

            _logger.LogInformation("Created zone mapping for workflow {ExternalWorkflowId} ({WorkflowCode}): {ZoneName} ({ZoneCode})",
                externalWorkflowId, workflow.WorkflowCode, classification.ZoneName, classification.ZoneCode);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return classification;
    }

    private async Task<WorkflowZoneClassification?> ClassifyWorkflowByZoneInternalAsync(
        int externalWorkflowId,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        // Get workflow node codes in order (already sorted from external API)
        var workflowNodeCodes = await dbContext.WorkflowNodeCodes
            .Where(wnc => wnc.ExternalWorkflowId == externalWorkflowId)
            .OrderBy(wnc => wnc.Id) // Preserve insertion order (which matches API order)
            .Select(wnc => wnc.NodeCode)
            .ToListAsync(cancellationToken);

        if (workflowNodeCodes.Count == 0)
        {
            _logger.LogWarning("No node codes found for workflow {ExternalWorkflowId}", externalWorkflowId);
            return null;
        }

        // Create a HashSet for efficient lookup
        var workflowNodeSet = workflowNodeCodes.ToHashSet();

        // Get all map zones (assuming active zones only)
        var mapZones = await dbContext.MapZones
            .Where(mz => mz.Status == 1) // Assuming 1 = active
            .OrderBy(mz => mz.Id) // Process zones in order
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Classifying workflow {ExternalWorkflowId} with {NodeCount} node codes against {ZoneCount} zones",
            externalWorkflowId, workflowNodeCodes.Count, mapZones.Count);

        // Find the first zone where ALL zone nodes exist in workflow node codes
        foreach (var zone in mapZones)
        {
            if (string.IsNullOrWhiteSpace(zone.Nodes))
            {
                continue;
            }

            // Parse zone nodes (assuming JSON array format like ["Sim1-1-1", "Sim1-1-2"])
            List<string> zoneNodes;
            try
            {
                zoneNodes = System.Text.Json.JsonSerializer.Deserialize<List<string>>(zone.Nodes) ?? new List<string>();
            }
            catch (System.Text.Json.JsonException)
            {
                // If not JSON, try comma-separated
                zoneNodes = zone.Nodes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
            }

            if (zoneNodes.Count == 0)
            {
                continue;
            }

            // Check if ALL zone nodes exist in workflow node codes
            var allNodesMatch = zoneNodes.All(zoneNode => workflowNodeSet.Contains(zoneNode));

            if (allNodesMatch)
            {
                _logger.LogInformation("Workflow {ExternalWorkflowId} classified to zone '{ZoneName}' ({ZoneCode}) with {MatchCount} matching nodes",
                    externalWorkflowId, zone.ZoneName, zone.ZoneCode, zoneNodes.Count);

                return new WorkflowZoneClassification
                {
                    ZoneName = zone.ZoneName,
                    ZoneCode = zone.ZoneCode,
                    MapCode = zone.MapCode,
                    MatchedNodesCount = zoneNodes.Count,
                    MatchedNodes = zoneNodes
                };
            }
        }

        _logger.LogInformation("Workflow {ExternalWorkflowId} could not be classified to any zone", externalWorkflowId);
        return null;
    }

    private async Task<SyncInternalResult> SyncWorkflowNodeCodesInternalAsync(
        int externalWorkflowId,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        try
        {
            // Validate configuration
            if (string.IsNullOrWhiteSpace(_options.NodeCodeQueryUrl))
            {
                return new SyncInternalResult
                {
                    Success = false,
                    ErrorMessage = "NodeCodeQueryUrl is not configured"
                };
            }

            // Get JWT token
            string token;
            try
            {
                token = await _externalApiTokenService.GetTokenAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                return new SyncInternalResult
                {
                    Success = false,
                    ErrorMessage = $"Failed to obtain authentication token: {ex.Message}"
                };
            }

            // Call external API
            var httpClient = _httpClientFactory.CreateClient();
            var requestUrl = $"{_options.NodeCodeQueryUrl}?workflowConfigId={externalWorkflowId}";

            var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Add("language", "en");
            request.Headers.Add("accept", "*/*");
            request.Headers.Add("wizards", "FRONT_END");

            HttpResponseMessage response;
            try
            {
                response = await httpClient.SendAsync(request, cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                return new SyncInternalResult
                {
                    Success = false,
                    ErrorMessage = $"HTTP request failed: {ex.Message}"
                };
            }

            if (!response.IsSuccessStatusCode)
            {
                return new SyncInternalResult
                {
                    Success = false,
                    ErrorMessage = $"API returned status code {response.StatusCode}"
                };
            }

            // Parse response
            SimulatorApiResponse<List<string>>? apiResponse;
            try
            {
                apiResponse = await response.Content.ReadFromJsonAsync<SimulatorApiResponse<List<string>>>(
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                return new SyncInternalResult
                {
                    Success = false,
                    ErrorMessage = $"Failed to parse API response: {ex.Message}"
                };
            }

            if (apiResponse == null || !apiResponse.Succ)
            {
                return new SyncInternalResult
                {
                    Success = false,
                    ErrorMessage = apiResponse?.Msg ?? "API returned unsuccessful response"
                };
            }

            var nodeCodes = apiResponse.Data ?? new List<string>();

            // Update database
            var now = _timeProvider.GetUtcNow().UtcDateTime;

            // Get existing node codes for this workflow
            var existingNodeCodes = await dbContext.WorkflowNodeCodes
                .Where(wnc => wnc.ExternalWorkflowId == externalWorkflowId)
                .ToListAsync(cancellationToken);

            var existingNodeCodeSet = existingNodeCodes
                .Select(wnc => wnc.NodeCode)
                .ToHashSet();

            var newNodeCodeSet = nodeCodes.ToHashSet();

            // Calculate insertions and deletions
            var nodeCodesToInsert = nodeCodes
                .Where(nc => !existingNodeCodeSet.Contains(nc))
                .ToList();

            var nodeCodesToDelete = existingNodeCodes
                .Where(wnc => !newNodeCodeSet.Contains(wnc.NodeCode))
                .ToList();

            // Delete removed node codes
            if (nodeCodesToDelete.Any())
            {
                dbContext.WorkflowNodeCodes.RemoveRange(nodeCodesToDelete);
            }

            // Insert new node codes
            if (nodeCodesToInsert.Any())
            {
                var newEntities = nodeCodesToInsert.Select(nc => new WorkflowNodeCode
                {
                    ExternalWorkflowId = externalWorkflowId,
                    NodeCode = nc,
                    CreatedUtc = now,
                    UpdatedUtc = now
                }).ToList();

                await dbContext.WorkflowNodeCodes.AddRangeAsync(newEntities, cancellationToken);
            }

            // Update timestamp for existing node codes
            var existingToUpdate = existingNodeCodes
                .Where(wnc => newNodeCodeSet.Contains(wnc.NodeCode))
                .ToList();

            foreach (var entity in existingToUpdate)
            {
                entity.UpdatedUtc = now;
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            return new SyncInternalResult
            {
                Success = true,
                NodeCodesInserted = nodeCodesToInsert.Count,
                NodeCodesDeleted = nodeCodesToDelete.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error syncing workflow {ExternalWorkflowId}", externalWorkflowId);
            return new SyncInternalResult
            {
                Success = false,
                ErrorMessage = $"Unexpected error: {ex.Message}"
            };
        }
    }

    public async Task<IEnumerable<WorkflowZoneMapping>> GetAllWorkflowZoneMappingsAsync(
        CancellationToken cancellationToken = default)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        return await dbContext.WorkflowZoneMappings
            .OrderBy(m => m.WorkflowCode)
            .ToListAsync(cancellationToken);
    }

    public async Task<WorkflowZoneMapping?> GetWorkflowZoneMappingAsync(
        int externalWorkflowId,
        CancellationToken cancellationToken = default)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        return await dbContext.WorkflowZoneMappings
            .FirstOrDefaultAsync(m => m.ExternalWorkflowId == externalWorkflowId, cancellationToken);
    }

    private class SyncInternalResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public int NodeCodesInserted { get; set; }
        public int NodeCodesDeleted { get; set; }
    }
}
