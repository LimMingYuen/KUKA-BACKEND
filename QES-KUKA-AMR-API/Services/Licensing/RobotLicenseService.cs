using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using QES_KUKA_AMR_API.Options;
using QES_KUKA_AMR_API.Services.Licensing.Models;

namespace QES_KUKA_AMR_API.Services.Licensing;

public class RobotLicenseService : IRobotLicenseService
{
    private readonly ILogger<RobotLicenseService> _logger;
    private readonly LicenseOptions _options;
    private readonly IMachineFingerprintService _fingerprintService;
    private readonly JsonSerializerOptions _jsonOptions;

    public RobotLicenseService(
        ILogger<RobotLicenseService> logger,
        IOptions<LicenseOptions> options,
        IMachineFingerprintService fingerprintService)
    {
        _logger = logger;
        _options = options.Value;
        _fingerprintService = fingerprintService;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task<RobotLicenseValidationResult> ValidateRobotLicenseAsync(string robotId)
    {
        try
        {
            var licensePath = GetRobotLicensePath(robotId);

            // Check if license file exists
            if (!File.Exists(licensePath))
            {
                _logger.LogDebug("Robot license file not found for robot: {RobotId}", robotId);
                return RobotLicenseValidationResult.Failure("ROBOT_LICENSE_NOT_FOUND",
                    $"No license file found for robot {robotId}.");
            }

            // Read license file
            var licenseJson = await File.ReadAllTextAsync(licensePath);
            var licenseFile = JsonSerializer.Deserialize<LicenseFile>(licenseJson, _jsonOptions);

            if (licenseFile == null)
            {
                return RobotLicenseValidationResult.Failure("ROBOT_LICENSE_PARSE_ERROR",
                    "Robot license file is corrupted or invalid format.");
            }

            return ValidateRobotLicenseFile(licenseFile, robotId);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse robot license file for robot: {RobotId}", robotId);
            return RobotLicenseValidationResult.Failure("ROBOT_LICENSE_PARSE_ERROR",
                "Robot license file is corrupted or invalid format.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate robot license for robot: {RobotId}", robotId);
            return RobotLicenseValidationResult.Failure("ROBOT_LICENSE_ERROR",
                $"Failed to validate robot license: {ex.Message}");
        }
    }

    public async Task<Dictionary<string, RobotLicenseValidationResult>> ValidateAllRobotLicensesAsync(IEnumerable<string> robotIds)
    {
        var results = new Dictionary<string, RobotLicenseValidationResult>();

        foreach (var robotId in robotIds)
        {
            results[robotId] = await ValidateRobotLicenseAsync(robotId);
        }

        return results;
    }

    public async Task<RobotLicenseValidationResult> ActivateRobotLicenseAsync(string robotId, Stream licenseFileStream)
    {
        try
        {
            // Read the uploaded license file
            using var reader = new StreamReader(licenseFileStream);
            var licenseJson = await reader.ReadToEndAsync();

            var licenseFile = JsonSerializer.Deserialize<LicenseFile>(licenseJson, _jsonOptions);
            if (licenseFile == null)
            {
                return RobotLicenseValidationResult.Failure("ROBOT_LICENSE_PARSE_ERROR",
                    "Robot license file is corrupted or invalid format.");
            }

            // Validate the license
            var result = ValidateRobotLicenseFile(licenseFile, robotId);

            if (result.IsValid)
            {
                // Save the license file
                Directory.CreateDirectory(_options.RobotLicensesPath);
                var licensePath = GetRobotLicensePath(robotId);
                await File.WriteAllTextAsync(licensePath, licenseJson);

                _logger.LogInformation("Robot license activated successfully for robot: {RobotId}", robotId);
            }

            return result;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse uploaded robot license file for robot: {RobotId}", robotId);
            return RobotLicenseValidationResult.Failure("ROBOT_LICENSE_PARSE_ERROR",
                "Robot license file is corrupted or invalid format.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to activate robot license for robot: {RobotId}", robotId);
            return RobotLicenseValidationResult.Failure("ROBOT_LICENSE_ERROR",
                $"Failed to activate robot license: {ex.Message}");
        }
    }

    public async Task<List<RobotLicenseStatus>> GetAllRobotLicenseStatusesAsync(IEnumerable<string> robotIds)
    {
        var statuses = new List<RobotLicenseStatus>();

        foreach (var robotId in robotIds)
        {
            statuses.Add(await GetRobotLicenseStatusAsync(robotId));
        }

        return statuses;
    }

    public async Task<RobotLicenseStatus> GetRobotLicenseStatusAsync(string robotId)
    {
        var result = await ValidateRobotLicenseAsync(robotId);

        return new RobotLicenseStatus
        {
            RobotId = robotId,
            IsLicensed = result.IsValid,
            ErrorCode = result.ErrorCode,
            ErrorMessage = result.ErrorMessage,
            CustomerName = result.LicenseData?.CustomerName,
            ExpiresAt = result.LicenseData?.ExpiresAt,
            DaysRemaining = result.LicenseData?.ExpiresAt.HasValue == true
                ? (int)(result.LicenseData.ExpiresAt.Value - DateTime.UtcNow).TotalDays
                : null
        };
    }

    private RobotLicenseValidationResult ValidateRobotLicenseFile(LicenseFile licenseFile, string expectedRobotId)
    {
        // Check public key is configured
        if (string.IsNullOrWhiteSpace(_options.PublicKey))
        {
            _logger.LogError("Public key not configured in appsettings.json");
            return RobotLicenseValidationResult.Failure("ROBOT_LICENSE_CONFIG_ERROR",
                "License validation is not configured properly.");
        }

        // Validate signature
        if (!ValidateSignature(licenseFile))
        {
            _logger.LogWarning("Robot license signature validation failed for robot: {RobotId}", expectedRobotId);
            return RobotLicenseValidationResult.Failure("ROBOT_LICENSE_INVALID_SIGNATURE",
                "Robot license file has been tampered with.");
        }

        // Extract license data
        var licenseData = ExtractRobotLicenseData(licenseFile);

        // Validate robot ID matches
        if (!string.Equals(licenseData.RobotId, expectedRobotId, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Robot ID mismatch. License is for: {LicenseRobotId}, Expected: {ExpectedRobotId}",
                licenseData.RobotId, expectedRobotId);
            return RobotLicenseValidationResult.Failure("ROBOT_LICENSE_ROBOT_MISMATCH",
                $"License is for robot '{licenseData.RobotId}', not '{expectedRobotId}'.");
        }

        // Validate machine ID
        var currentMachineId = _fingerprintService.GenerateFingerprint();
        if (!string.Equals(licenseData.MachineId, currentMachineId, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Machine ID mismatch for robot license. Robot: {RobotId}", expectedRobotId);
            return RobotLicenseValidationResult.Failure("ROBOT_LICENSE_MACHINE_MISMATCH",
                "Robot license is not valid for this machine.");
        }

        // Validate expiration
        if (licenseData.ExpiresAt.HasValue && licenseData.ExpiresAt.Value < DateTime.UtcNow)
        {
            _logger.LogWarning("Robot license expired for robot: {RobotId} on {Date}",
                expectedRobotId, licenseData.ExpiresAt.Value);
            return RobotLicenseValidationResult.Failure("ROBOT_LICENSE_EXPIRED",
                $"Robot license has expired on {licenseData.ExpiresAt.Value:yyyy-MM-dd}.");
        }

        // License is valid
        _logger.LogDebug("Robot license validated successfully for robot: {RobotId}", expectedRobotId);
        return RobotLicenseValidationResult.Success(licenseData);
    }

    private bool ValidateSignature(LicenseFile licenseFile)
    {
        try
        {
            using var rsa = RSA.Create();
            rsa.ImportFromPem(_options.PublicKey);

            var payloadBytes = Convert.FromBase64String(licenseFile.Data);
            var signatureBytes = Convert.FromBase64String(licenseFile.Signature);

            return rsa.VerifyData(
                payloadBytes,
                signatureBytes,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate robot license signature");
            return false;
        }
    }

    private RobotLicenseData ExtractRobotLicenseData(LicenseFile licenseFile)
    {
        var payloadBytes = Convert.FromBase64String(licenseFile.Data);
        var json = Encoding.UTF8.GetString(payloadBytes);
        return JsonSerializer.Deserialize<RobotLicenseData>(json, _jsonOptions)
            ?? throw new InvalidOperationException("Failed to parse robot license data");
    }

    private string GetRobotLicensePath(string robotId)
    {
        // Sanitize robot ID for file name (remove invalid characters)
        var safeRobotId = string.Concat(robotId.Split(Path.GetInvalidFileNameChars()));
        return Path.Combine(_options.RobotLicensesPath, $"{safeRobotId}.lic");
    }
}
