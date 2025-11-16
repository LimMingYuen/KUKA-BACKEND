using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QES_KUKA_AMR_API.Data;
using QES_KUKA_AMR_API.Data.Entities;
using QES_KUKA_AMR_API.Models.Jobs;
using QES_KUKA_AMR_API.Models.Missions;
using QES_KUKA_AMR_API.Options;

namespace QES_KUKA_AMR_API.Services.Queue;

public class QueueSchedulerHostedService : IHostedService, IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<QueueSchedulerHostedService> _logger;
    private readonly QueueSchedulerOptions _options;
    private Timer? _processingTimer;
    private Timer? _completionCheckTimer;
    private bool _isProcessing;
    private bool _isCheckingCompletion;

    public QueueSchedulerHostedService(
        IServiceProvider serviceProvider,
        IOptions<QueueSchedulerOptions> options,
        ILogger<QueueSchedulerHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _options = options.Value;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Queue Scheduler is disabled");
            return Task.CompletedTask;
        }

        _logger.LogInformation(
            "Queue Scheduler starting (ProcessingInterval: {ProcessingInterval}s, CompletionCheck: {CompletionCheckInterval}s)",
            _options.ProcessingIntervalSeconds,
            _options.CompletionCheckIntervalSeconds
        );

        // Timer for processing pending jobs
        _processingTimer = new Timer(
            ProcessQueuesAsync,
            null,
            TimeSpan.Zero,
            TimeSpan.FromSeconds(_options.ProcessingIntervalSeconds)
        );

        // Timer for checking job completion
        _completionCheckTimer = new Timer(
            CheckJobCompletionAsync,
            null,
            TimeSpan.FromSeconds(2),
            TimeSpan.FromSeconds(_options.CompletionCheckIntervalSeconds)
        );

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Queue Scheduler stopping");

        _processingTimer?.Change(Timeout.Infinite, 0);
        _completionCheckTimer?.Change(Timeout.Infinite, 0);

        return Task.CompletedTask;
    }

    private async void ProcessQueuesAsync(object? state)
    {
        if (_isProcessing)
        {
            _logger.LogDebug("Queue processing already in progress, skipping this cycle");
            return;
        }

        _isProcessing = true;

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var queueManager = scope.ServiceProvider.GetRequiredService<IMapCodeQueueManager>();
            var robotAssignmentService = scope.ServiceProvider.GetRequiredService<IRobotAssignmentService>();

            // Get all active MapCodes from configurations
            var activeMapCodes = await dbContext.MapCodeQueueConfigurations
                .Where(c => c.EnableQueue)
                .Select(c => c.MapCode)
                .ToListAsync();

            if (!activeMapCodes.Any())
            {
                _logger.LogDebug("No active MapCode queues configured");
                return;
            }

            foreach (var mapCode in activeMapCodes)
            {
                await ProcessMapCodeQueueAsync(mapCode, queueManager, robotAssignmentService, dbContext);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing queues");
        }
        finally
        {
            _isProcessing = false;
        }
    }

    private async Task ProcessMapCodeQueueAsync(
        string mapCode,
        IMapCodeQueueManager queueManager,
        IRobotAssignmentService robotAssignmentService,
        ApplicationDbContext dbContext)
    {
        try
        {
            var pendingJobs = await queueManager.GetPendingJobsAsync(
                mapCode,
                _options.MaxJobsPerMapCodePerCycle
            );

            if (!pendingJobs.Any())
            {
                _logger.LogDebug("No pending jobs for MapCode {MapCode}", mapCode);
                return;
            }

            _logger.LogInformation(
                "Processing {Count} pending jobs for MapCode {MapCode}",
                pendingJobs.Count,
                mapCode
            );

            foreach (var job in pendingJobs)
            {
                await ProcessJobAsync(job, queueManager, robotAssignmentService, dbContext);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing MapCode queue {MapCode}", mapCode);
        }
    }

    private async Task ProcessJobAsync(
        MissionQueueItem job,
        IMapCodeQueueManager queueManager,
        IRobotAssignmentService robotAssignmentService,
        ApplicationDbContext dbContext)
    {
        try
        {
            // Skip if already assigned or being processed
            if (job.Status != MissionQueueStatus.Pending && job.Status != MissionQueueStatus.ReadyToAssign)
            {
                return;
            }

            // Try to assign robot
            var assignment = await robotAssignmentService.AssignBestRobotAsync(job);

            if (assignment == null)
            {
                _logger.LogDebug(
                    "No suitable robot available for job {QueueItemCode} on MapCode {MapCode}",
                    job.QueueItemCode,
                    job.PrimaryMapCode
                );
                return;
            }

            // Update job with robot assignment
            job.AssignedRobotId = assignment.RobotId;
            job.RobotAssignedUtc = assignment.AssignedUtc;
            job.Status = MissionQueueStatus.Assigned;
            await dbContext.SaveChangesAsync();

            _logger.LogInformation(
                "Assigned robot {RobotId} to job {QueueItemCode} (distance: {Distance:F2}m)",
                assignment.RobotId,
                job.QueueItemCode,
                assignment.Distance
            );

            // Submit to AMR system
            var submitted = await SubmitJobToAmrAsync(job, dbContext);

            if (submitted)
            {
                job.Status = MissionQueueStatus.SubmittedToAmr;
                job.StartedUtc = DateTime.UtcNow;
                await dbContext.SaveChangesAsync();

                _logger.LogInformation(
                    "Successfully submitted job {QueueItemCode} to AMR system",
                    job.QueueItemCode
                );
            }
            else
            {
                // Submission failed, retry later
                job.RetryCount++;
                job.LastRetryUtc = DateTime.UtcNow;

                if (job.RetryCount >= _options.MaxRetryAttempts)
                {
                    job.Status = MissionQueueStatus.Failed;
                    job.ErrorMessage = $"Failed to submit after {job.RetryCount} attempts";
                    _logger.LogError(
                        "Job {QueueItemCode} failed after {RetryCount} submission attempts",
                        job.QueueItemCode,
                        job.RetryCount
                    );
                }
                else
                {
                    job.Status = MissionQueueStatus.Pending; // Retry
                    _logger.LogWarning(
                        "Job {QueueItemCode} submission failed, will retry (attempt {Attempt}/{Max})",
                        job.QueueItemCode,
                        job.RetryCount,
                        _options.MaxRetryAttempts
                    );
                }

                await dbContext.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing job {QueueItemCode}", job.QueueItemCode);

            job.RetryCount++;
            job.LastRetryUtc = DateTime.UtcNow;
            job.ErrorMessage = ex.Message;

            if (job.RetryCount >= _options.MaxRetryAttempts)
            {
                job.Status = MissionQueueStatus.Failed;
            }

            await dbContext.SaveChangesAsync();
        }
    }

    private async Task<bool> SubmitJobToAmrAsync(MissionQueueItem job, ApplicationDbContext dbContext)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var httpClientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
            var missionOptions = scope.ServiceProvider.GetRequiredService<IOptions<MissionServiceOptions>>().Value;

            var httpClient = httpClientFactory.CreateClient();

            // Parse mission steps from JSON
            var missionSteps = JsonSerializer.Deserialize<List<MissionDataItem>>(job.MissionStepsJson);
            if (missionSteps == null || !missionSteps.Any())
            {
                _logger.LogError("Job {QueueItemCode} has no mission steps", job.QueueItemCode);
                return false;
            }

            // Parse robot constraints
            List<string>? robotModels = null;
            List<string>? robotIds = null;

            if (!string.IsNullOrEmpty(job.RobotModelsJson))
            {
                robotModels = JsonSerializer.Deserialize<List<string>>(job.RobotModelsJson);
            }

            if (!string.IsNullOrEmpty(job.RobotIdsJson))
            {
                robotIds = JsonSerializer.Deserialize<List<string>>(job.RobotIdsJson);
            }

            // Build submission request
            var submitRequest = new SubmitMissionRequest
            {
                MissionCode = job.MissionCode,
                RequestId = job.RequestId,
                Priority = job.Priority,
                RobotModels = robotModels?.AsReadOnly() ?? new List<string>().AsReadOnly(),
                RobotIds = robotIds?.AsReadOnly() ?? new List<string>().AsReadOnly(),
                MissionData = missionSteps.AsReadOnly()
            };

            var requestJson = JsonSerializer.Serialize(submitRequest);
            var content = new StringContent(requestJson, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"));

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, missionOptions.SubmitMissionUrl)
            {
                Content = content
            };

            httpRequest.Headers.Add("language", "en");
            httpRequest.Headers.Add("accept", "*/*");
            httpRequest.Headers.Add("wizards", "FRONT_END");

            var response = await httpClient.SendAsync(httpRequest);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "AMR API returned error for job {QueueItemCode}: {StatusCode} - {Response}",
                    job.QueueItemCode,
                    response.StatusCode,
                    responseContent
                );
                return false;
            }

            var submitResponse = JsonSerializer.Deserialize<SubmitMissionResponse>(responseContent);

            if (submitResponse?.Success == true)
            {
                return true;
            }

            _logger.LogWarning(
                "AMR API returned non-success response for job {QueueItemCode}: {Message}",
                job.QueueItemCode,
                submitResponse?.Message ?? "Unknown error"
            );

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception submitting job {QueueItemCode} to AMR", job.QueueItemCode);
            return false;
        }
    }

    private async void CheckJobCompletionAsync(object? state)
    {
        if (_isCheckingCompletion)
        {
            return;
        }

        _isCheckingCompletion = true;

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var jobOpportunityEvaluator = scope.ServiceProvider.GetRequiredService<IJobOpportunityEvaluator>();
            var httpClientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
            var missionOptions = scope.ServiceProvider.GetRequiredService<IOptions<MissionServiceOptions>>().Value;

            // Get all jobs that are currently executing or submitted to AMR
            var executingJobs = await dbContext.MissionQueueItems
                .Where(q => q.Status == MissionQueueStatus.SubmittedToAmr || q.Status == MissionQueueStatus.Executing)
                .ToListAsync();

            if (!executingJobs.Any())
            {
                return;
            }

            _logger.LogDebug("Checking completion status for {Count} executing jobs", executingJobs.Count);

            foreach (var job in executingJobs)
            {
                await CheckSingleJobCompletionAsync(
                    job,
                    dbContext,
                    jobOpportunityEvaluator,
                    httpClientFactory,
                    missionOptions
                );
            }

            await dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking job completion");
        }
        finally
        {
            _isCheckingCompletion = false;
        }
    }

    private async Task CheckSingleJobCompletionAsync(
        MissionQueueItem job,
        ApplicationDbContext dbContext,
        IJobOpportunityEvaluator jobOpportunityEvaluator,
        IHttpClientFactory httpClientFactory,
        MissionServiceOptions missionOptions)
    {
        try
        {
            // Query AMR system for job status
            var httpClient = httpClientFactory.CreateClient();

            var jobQueryRequest = new JobQueryRequest
            {
                JobCode = job.MissionCode,
                Limit = 1
            };

            var requestJson = JsonSerializer.Serialize(jobQueryRequest);
            var content = new StringContent(requestJson, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"));

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, missionOptions.JobQueryUrl)
            {
                Content = content
            };

            httpRequest.Headers.Add("language", "en");
            httpRequest.Headers.Add("accept", "*/*");
            httpRequest.Headers.Add("wizards", "FRONT_END");

            var response = await httpClient.SendAsync(httpRequest);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Failed to query job status for {MissionCode}: {StatusCode}",
                    job.MissionCode,
                    response.StatusCode
                );
                return;
            }

            var jobQueryResponse = JsonSerializer.Deserialize<JobQueryResponse>(responseContent);

            if (jobQueryResponse?.Data == null || !jobQueryResponse.Data.Any())
            {
                _logger.LogDebug("No job data found for {MissionCode}", job.MissionCode);
                return;
            }

            var jobData = jobQueryResponse.Data.First();

            // Update job status based on AMR response
            if (jobData.Status == 100) // Completed (adjust code based on your AMR system)
            {
                job.Status = MissionQueueStatus.Completed;
                job.CompletedUtc = DateTime.UtcNow;

                _logger.LogInformation("Job {QueueItemCode} completed successfully", job.QueueItemCode);

                // Trigger opportunistic job evaluation
                if (_options.EnableOpportunisticJobEvaluation && !string.IsNullOrEmpty(job.AssignedRobotId))
                {
                    try
                    {
                        var evaluation = await jobOpportunityEvaluator.EvaluateOpportunityAsync(
                            job.AssignedRobotId,
                            job.Id
                        );

                        _logger.LogInformation(
                            "Opportunistic job evaluation for robot {RobotId}: {Decision} - {Reason}",
                            job.AssignedRobotId,
                            evaluation.Decision,
                            evaluation.Reason
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex,
                            "Error evaluating opportunistic job for robot {RobotId}",
                            job.AssignedRobotId
                        );
                    }
                }
            }
            else if (jobData.Status >= 200) // Failed/Cancelled (adjust based on your AMR system)
            {
                job.Status = MissionQueueStatus.Failed;
                job.ErrorMessage = $"AMR job failed with status code {jobData.Status}";

                _logger.LogWarning(
                    "Job {QueueItemCode} failed with AMR status code {StatusCode}",
                    job.QueueItemCode,
                    jobData.Status
                );
            }
            else if (job.Status == MissionQueueStatus.SubmittedToAmr)
            {
                // Job is now executing
                job.Status = MissionQueueStatus.Executing;
                _logger.LogDebug("Job {QueueItemCode} is now executing", job.QueueItemCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking completion for job {QueueItemCode}", job.QueueItemCode);
        }
    }

    public void Dispose()
    {
        _processingTimer?.Dispose();
        _completionCheckTimer?.Dispose();
    }
}
