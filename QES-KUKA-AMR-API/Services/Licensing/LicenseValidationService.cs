using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using QES_KUKA_AMR_API.Options;
using QES_KUKA_AMR_API.Services.Licensing.Models;

namespace QES_KUKA_AMR_API.Services.Licensing;

public class LicenseValidationService : ILicenseValidationService
{
    private readonly ILogger<LicenseValidationService> _logger;
    private readonly LicenseOptions _options;
    private readonly IMachineFingerprintService _fingerprintService;
    private readonly ILicenseStateService _stateService;
    private readonly JsonSerializerOptions _jsonOptions;

    public LicenseValidationService(
        ILogger<LicenseValidationService> logger,
        IOptions<LicenseOptions> options,
        IMachineFingerprintService fingerprintService,
        ILicenseStateService stateService)
    {
        _logger = logger;
        _options = options.Value;
        _fingerprintService = fingerprintService;
        _stateService = stateService;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task<LicenseValidationResult> ValidateLicenseAsync(CancellationToken ct = default)
    {
        try
        {
            // Check if license file exists
            if (!File.Exists(_options.LicenseFilePath))
            {
                _logger.LogWarning("License file not found at: {Path}", _options.LicenseFilePath);
                return CreateErrorResult("LICENSE_NOT_FOUND",
                    "License file not found. Please activate a license.");
            }

            // Read license file
            var licenseJson = await File.ReadAllTextAsync(_options.LicenseFilePath, ct);
            var licenseFile = JsonSerializer.Deserialize<LicenseFile>(licenseJson, _jsonOptions);

            if (licenseFile == null)
            {
                return CreateErrorResult("LICENSE_PARSE_ERROR",
                    "License file is corrupted or invalid format.");
            }

            return await ValidateLicenseFileAsync(licenseFile, ct);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse license file");
            return CreateErrorResult("LICENSE_PARSE_ERROR",
                "License file is corrupted or invalid format.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate license");
            return CreateErrorResult("LICENSE_ERROR",
                $"Failed to validate license: {ex.Message}");
        }
    }

    public async Task<LicenseValidationResult> ActivateLicenseAsync(Stream licenseFileStream, CancellationToken ct = default)
    {
        try
        {
            // Read the uploaded license file
            using var reader = new StreamReader(licenseFileStream);
            var licenseJson = await reader.ReadToEndAsync(ct);

            var licenseFile = JsonSerializer.Deserialize<LicenseFile>(licenseJson, _jsonOptions);
            if (licenseFile == null)
            {
                return CreateErrorResult("LICENSE_PARSE_ERROR",
                    "License file is corrupted or invalid format.");
            }

            // Validate the license
            var result = await ValidateLicenseFileAsync(licenseFile, ct);

            if (result.IsValid)
            {
                // Save the license file to the configured path
                var directory = Path.GetDirectoryName(_options.LicenseFilePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                await File.WriteAllTextAsync(_options.LicenseFilePath, licenseJson, ct);
                _logger.LogInformation("License activated successfully for customer: {Customer}",
                    result.LicenseInfo?.CustomerName);

                // Update state
                _stateService.SetLimitedMode(false);
                _stateService.SetLicenseInfo(result.LicenseInfo);
            }

            return result;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse uploaded license file");
            return CreateErrorResult("LICENSE_PARSE_ERROR",
                "License file is corrupted or invalid format.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to activate license");
            return CreateErrorResult("LICENSE_ERROR",
                $"Failed to activate license: {ex.Message}");
        }
    }

    public LicenseInfo? GetCurrentLicenseInfo()
    {
        return _stateService.CurrentLicenseInfo;
    }

    private Task<LicenseValidationResult> ValidateLicenseFileAsync(LicenseFile licenseFile, CancellationToken ct)
    {
        // Check public key is configured
        if (string.IsNullOrWhiteSpace(_options.PublicKey))
        {
            _logger.LogError("Public key not configured in appsettings.json");
            return Task.FromResult(CreateErrorResult("LICENSE_CONFIG_ERROR",
                "License validation is not configured properly."));
        }

        // Validate signature
        if (!ValidateSignature(licenseFile))
        {
            _logger.LogWarning("License signature validation failed");
            return Task.FromResult(CreateErrorResult("LICENSE_INVALID_SIGNATURE",
                "License file has been tampered with."));
        }

        // Extract license data
        var licenseData = ExtractLicenseData(licenseFile);

        // Validate machine ID
        var currentMachineId = _fingerprintService.GenerateFingerprint();
        if (!string.Equals(licenseData.MachineId, currentMachineId, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Machine ID mismatch. Expected: {Expected}, Got: {Got}",
                licenseData.MachineId, currentMachineId);
            return Task.FromResult(CreateErrorResult("LICENSE_MACHINE_MISMATCH",
                "License is not valid for this machine."));
        }

        // Validate expiration
        if (licenseData.ExpiresAt.HasValue && licenseData.ExpiresAt.Value < DateTime.UtcNow)
        {
            _logger.LogWarning("License expired on {Date}", licenseData.ExpiresAt.Value);
            return Task.FromResult(CreateErrorResult("LICENSE_EXPIRED",
                $"License has expired on {licenseData.ExpiresAt.Value:yyyy-MM-dd}."));
        }

        // License is valid
        var licenseInfo = new LicenseInfo
        {
            LicenseId = licenseData.LicenseId,
            CustomerName = licenseData.CustomerName,
            LicenseType = licenseData.LicenseType,
            ExpiresAt = licenseData.ExpiresAt,
            DaysRemaining = licenseData.ExpiresAt.HasValue
                ? (int)(licenseData.ExpiresAt.Value - DateTime.UtcNow).TotalDays
                : null,
            MaxRobots = licenseData.MaxRobots,
            Features = licenseData.Features
        };

        // Update state
        _stateService.SetLimitedMode(false);
        _stateService.SetLicenseInfo(licenseInfo);

        _logger.LogInformation("License validated successfully for customer: {Customer}", licenseData.CustomerName);

        return Task.FromResult(new LicenseValidationResult
        {
            IsValid = true,
            LicenseInfo = licenseInfo
        });
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
            _logger.LogError(ex, "Failed to validate license signature");
            return false;
        }
    }

    private LicenseData ExtractLicenseData(LicenseFile licenseFile)
    {
        var payloadBytes = Convert.FromBase64String(licenseFile.Data);
        var json = Encoding.UTF8.GetString(payloadBytes);
        return JsonSerializer.Deserialize<LicenseData>(json, _jsonOptions)
            ?? throw new InvalidOperationException("Failed to parse license data");
    }

    private static LicenseValidationResult CreateErrorResult(string code, string message)
    {
        return new LicenseValidationResult
        {
            IsValid = false,
            ErrorCode = code,
            ErrorMessage = message
        };
    }
}
