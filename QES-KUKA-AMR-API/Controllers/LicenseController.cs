using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QES_KUKA_AMR_API.Data;
using QES_KUKA_AMR_API.Models;
using QES_KUKA_AMR_API.Models.License;
using QES_KUKA_AMR_API.Services.Licensing;
using QES_KUKA_AMR_API.Services.Licensing.Models;

namespace QES_KUKA_AMR_API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LicenseController : ControllerBase
{
    private readonly ILogger<LicenseController> _logger;
    private readonly IMachineFingerprintService _fingerprintService;
    private readonly ILicenseValidationService _validationService;
    private readonly ILicenseStateService _stateService;
    private readonly IRobotLicenseService _robotLicenseService;
    private readonly ApplicationDbContext _dbContext;

    public LicenseController(
        ILogger<LicenseController> logger,
        IMachineFingerprintService fingerprintService,
        ILicenseValidationService validationService,
        ILicenseStateService stateService,
        IRobotLicenseService robotLicenseService,
        ApplicationDbContext dbContext)
    {
        _logger = logger;
        _fingerprintService = fingerprintService;
        _validationService = validationService;
        _stateService = stateService;
        _robotLicenseService = robotLicenseService;
        _dbContext = dbContext;
    }

    /// <summary>
    /// Gets the machine ID (fingerprint) for this machine.
    /// This ID is used to generate a license file.
    /// </summary>
    [HttpGet("machine-id")]
    [AllowAnonymous]
    public ActionResult<ApiResponse<MachineIdResponse>> GetMachineId()
    {
        var machineId = _fingerprintService.GenerateFingerprint();
        var displayMachineId = _fingerprintService.GetDisplayFingerprint();

        return Ok(new ApiResponse<MachineIdResponse>
        {
            Success = true,
            Code = "SUCCESS",
            Msg = "Machine ID retrieved successfully",
            Data = new MachineIdResponse
            {
                MachineId = machineId,
                DisplayMachineId = displayMachineId
            }
        });
    }

    /// <summary>
    /// Gets the current license status.
    /// </summary>
    [HttpGet("status")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<LicenseStatusResponse>>> GetLicenseStatus(
        CancellationToken cancellationToken)
    {
        var result = await _validationService.ValidateLicenseAsync(cancellationToken);

        var response = new LicenseStatusResponse
        {
            IsValid = result.IsValid,
            ErrorCode = result.ErrorCode,
            ErrorMessage = result.ErrorMessage
        };

        if (result.LicenseInfo != null)
        {
            response.CustomerName = result.LicenseInfo.CustomerName;
            response.LicenseType = result.LicenseInfo.LicenseType;
            response.ExpiresAt = result.LicenseInfo.ExpiresAt;
            response.DaysRemaining = result.LicenseInfo.DaysRemaining;
            response.MaxRobots = result.LicenseInfo.MaxRobots;
        }

        return Ok(new ApiResponse<LicenseStatusResponse>
        {
            Success = result.IsValid,
            Code = result.IsValid ? "SUCCESS" : result.ErrorCode ?? "LICENSE_ERROR",
            Msg = result.IsValid ? "License is valid" : result.ErrorMessage,
            Data = response
        });
    }

    /// <summary>
    /// Activates a license by uploading a license file.
    /// </summary>
    [HttpPost("activate")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<LicenseStatusResponse>>> ActivateLicense(
        IFormFile licenseFile,
        CancellationToken cancellationToken)
    {
        if (licenseFile == null || licenseFile.Length == 0)
        {
            return BadRequest(new ApiResponse<LicenseStatusResponse>
            {
                Success = false,
                Code = "LICENSE_FILE_REQUIRED",
                Msg = "Please upload a license file"
            });
        }

        using var stream = licenseFile.OpenReadStream();
        var result = await _validationService.ActivateLicenseAsync(stream, cancellationToken);

        var response = new LicenseStatusResponse
        {
            IsValid = result.IsValid,
            ErrorCode = result.ErrorCode,
            ErrorMessage = result.ErrorMessage
        };

        if (result.LicenseInfo != null)
        {
            response.CustomerName = result.LicenseInfo.CustomerName;
            response.LicenseType = result.LicenseInfo.LicenseType;
            response.ExpiresAt = result.LicenseInfo.ExpiresAt;
            response.DaysRemaining = result.LicenseInfo.DaysRemaining;
            response.MaxRobots = result.LicenseInfo.MaxRobots;
        }

        if (!result.IsValid)
        {
            return BadRequest(new ApiResponse<LicenseStatusResponse>
            {
                Success = false,
                Code = result.ErrorCode ?? "LICENSE_ERROR",
                Msg = result.ErrorMessage ?? "License activation failed",
                Data = response
            });
        }

        _logger.LogInformation("License activated successfully for customer: {Customer}",
            result.LicenseInfo?.CustomerName);

        return Ok(new ApiResponse<LicenseStatusResponse>
        {
            Success = true,
            Code = "SUCCESS",
            Msg = "License activated successfully",
            Data = response
        });
    }

    // ============================================
    // ROBOT LICENSE ENDPOINTS
    // ============================================

    /// <summary>
    /// Gets the license status for all robots.
    /// </summary>
    [HttpGet("robots")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<List<RobotLicenseStatus>>>> GetAllRobotLicenseStatuses(
        CancellationToken cancellationToken)
    {
        var robotIds = await _dbContext.MobileRobots
            .AsNoTracking()
            .Select(r => r.RobotId)
            .ToListAsync(cancellationToken);

        var statuses = await _robotLicenseService.GetAllRobotLicenseStatusesAsync(robotIds);

        return Ok(new ApiResponse<List<RobotLicenseStatus>>
        {
            Success = true,
            Code = "SUCCESS",
            Msg = $"Retrieved license status for {statuses.Count} robots",
            Data = statuses
        });
    }

    /// <summary>
    /// Gets the license status for a specific robot.
    /// </summary>
    [HttpGet("robots/{robotId}")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<RobotLicenseStatus>>> GetRobotLicenseStatus(
        string robotId)
    {
        var status = await _robotLicenseService.GetRobotLicenseStatusAsync(robotId);

        return Ok(new ApiResponse<RobotLicenseStatus>
        {
            Success = status.IsLicensed,
            Code = status.IsLicensed ? "SUCCESS" : status.ErrorCode ?? "ROBOT_UNLICENSED",
            Msg = status.IsLicensed ? "Robot is licensed" : status.ErrorMessage ?? "Robot is not licensed",
            Data = status
        });
    }

    /// <summary>
    /// Activates a license for a specific robot.
    /// </summary>
    [HttpPost("robots/{robotId}/activate")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<RobotLicenseStatus>>> ActivateRobotLicense(
        string robotId,
        IFormFile licenseFile,
        CancellationToken cancellationToken)
    {
        if (licenseFile == null || licenseFile.Length == 0)
        {
            return BadRequest(new ApiResponse<RobotLicenseStatus>
            {
                Success = false,
                Code = "LICENSE_FILE_REQUIRED",
                Msg = "Please upload a license file"
            });
        }

        using var stream = licenseFile.OpenReadStream();
        var result = await _robotLicenseService.ActivateRobotLicenseAsync(robotId, stream);

        if (!result.IsValid)
        {
            return BadRequest(new ApiResponse<RobotLicenseStatus>
            {
                Success = false,
                Code = result.ErrorCode ?? "ROBOT_LICENSE_ERROR",
                Msg = result.ErrorMessage ?? "Robot license activation failed",
                Data = new RobotLicenseStatus
                {
                    RobotId = robotId,
                    IsLicensed = false,
                    ErrorCode = result.ErrorCode,
                    ErrorMessage = result.ErrorMessage
                }
            });
        }

        // Update the robot's license status in the database
        var robot = await _dbContext.MobileRobots.FirstOrDefaultAsync(r => r.RobotId == robotId, cancellationToken);
        if (robot != null)
        {
            robot.IsLicensed = true;
            robot.LicenseError = null;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation("Robot license activated successfully for robot: {RobotId}", robotId);

        return Ok(new ApiResponse<RobotLicenseStatus>
        {
            Success = true,
            Code = "SUCCESS",
            Msg = "Robot license activated successfully",
            Data = new RobotLicenseStatus
            {
                RobotId = robotId,
                IsLicensed = true,
                CustomerName = result.LicenseData?.CustomerName,
                ExpiresAt = result.LicenseData?.ExpiresAt,
                DaysRemaining = result.LicenseData?.ExpiresAt.HasValue == true
                    ? (int)(result.LicenseData.ExpiresAt.Value - DateTime.UtcNow).TotalDays
                    : null
            }
        });
    }
}
