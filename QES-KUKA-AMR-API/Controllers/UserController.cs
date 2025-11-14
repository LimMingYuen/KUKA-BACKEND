using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QES_KUKA_AMR_API.Data;
using QES_KUKA_AMR_API.Models.Config;
using QES_KUKA_AMR_API.Models.User;
using QES_KUKA_AMR_API.Options;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace QES_KUKA_AMR_API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private const int SyncPageNumber = -1;
    private const int SyncPageSize = 10_000;

    private readonly ApplicationDbContext _dbContext;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<UserController> _logger;
    private readonly UserServiceOptions _userOptions;

    public UserController(ApplicationDbContext dbContext,
        IHttpClientFactory httpClientFactory,
        ILogger<UserController> logger,
        IOptions<UserServiceOptions> userOptions)
    {
        _dbContext = dbContext;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _userOptions = userOptions.Value;
    }

    [HttpPost("sync")]
    public async Task<ActionResult<UserSyncResultDto>> SyncAsync(CancellationToken cancellationToken)
    {
        if (!AuthenticationHeaderValue.TryParse(Request.Headers.Authorization, out var authHeader) || string.IsNullOrWhiteSpace(authHeader.Parameter))
        {
            return StatusCode(StatusCodes.Status401Unauthorized, new
            {
                Code = StatusCodes.Status401Unauthorized,
                Message = "Missing or invalid Authorization header."
            });
        }

        var token = authHeader.Parameter;
        if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            token = token.Substring(7).Trim();
        }

        if (string.IsNullOrWhiteSpace(_userOptions.UserListUrl) ||
            !Uri.TryCreate(_userOptions.UserListUrl, UriKind.Absolute, out var requestUri))
        {
            _logger.LogError("User list URL is not configured correctly.");
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                Code = StatusCodes.Status500InternalServerError,
                Message = "User list URL is not configured."
            });
        }

        var httpClient = _httpClientFactory.CreateClient();

        var apiRequest = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = JsonContent.Create(new QueryUserRequest
            {
                PageNum = SyncPageNumber,
                PageSize = SyncPageSize
            })
        };
        apiRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        apiRequest.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json")
        {
            CharSet = "UTF-8"
        };
        apiRequest.Headers.Add("language", "en");
        apiRequest.Headers.Add("accept", "*/*");
        apiRequest.Headers.Add("wizards", "FRONT_END");

        if (Request.Headers.TryGetValue("Cookie", out var cookies))
        {
            apiRequest.Headers.Add("Cookie", cookies.ToString());
        }

        _logger.LogInformation("=== User Sync Request Debug ===");
        _logger.LogInformation("Target URI: {Uri}", requestUri);
        _logger.LogInformation("=== End Request Debug ===");

        try
        {
            using var response = await httpClient.SendAsync(apiRequest, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            _logger.LogInformation("=== User Sync Response Debug ===");
            _logger.LogInformation("Response Status: {StatusCode}", response.StatusCode);
            _logger.LogInformation("=== End Response Debug ===");

            _logger.LogWarning("Raw API response content:\n{Content}", responseContent);

            if (string.IsNullOrWhiteSpace(responseContent) || responseContent.TrimStart().StartsWith('<'))
            {
                _logger.LogError("API returned HTML instead of JSON.");
                return StatusCode(StatusCodes.Status502BadGateway, new
                {
                    Code = (int)HttpStatusCode.BadGateway,
                    Message = "Backend returned HTML instead of JSON."
                });
            }

            var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            if (responseContent.Contains("\"success\""))
            {
                var realBackendResponse = JsonSerializer.Deserialize<RealBackendApiResponse<UserPage>>(
                    responseContent, jsonOptions);

                if (realBackendResponse is null || !realBackendResponse.Success)
                {
                    var message = realBackendResponse?.Message ?? "Failed to sync user list";
                    return StatusCode((int)response.StatusCode, new { Code = "ERROR", Message = message });
                }

                var userLists = realBackendResponse.Data?.Content;
                if (userLists is null || userLists.Count == 0)
                {
                    return Ok(new UserSyncResultDto { Total = 0, Inserted = 0, Updated = 0 });
                }

                return await ProcessUserListAsync(userLists, cancellationToken);
            }
            else
            {
                var simulatorResponse = JsonSerializer.Deserialize<SimulatorApiResponse<UserPage>>(
                    responseContent, jsonOptions);

                if (simulatorResponse is null || simulatorResponse.Succ is not true)
                {
                    var message = simulatorResponse?.Msg ?? "Failed to sync user list from simulator";
                    return StatusCode((int)response.StatusCode, new { Code = (int)response.StatusCode, Message = message });
                }

                var userLists = simulatorResponse.Data?.Content;
                if (userLists is null || userLists.Count == 0)
                {
                    return Ok(new UserSyncResultDto { Total = 0, Inserted = 0, Updated = 0 });
                }

                return await ProcessUserListAsync(userLists, cancellationToken);
            }

        }
        catch (HttpRequestException httpRequestException)
        {
            _logger.LogError(httpRequestException, "Error calling API simulator");
            return StatusCode(StatusCodes.Status502BadGateway, new
            {
                Code = (int)HttpStatusCode.BadGateway,
                Message = "Unable to reach the API simulator."
            });
        }
    }

    private async Task<ActionResult<UserSyncResultDto>> ProcessUserListAsync(IEnumerable<UserDto> userLists, CancellationToken cancellationToken)
    {
        var userList = userLists.ToList();

        // Sync roles from external system to Roles table
        await SyncRolesToTableAsync(userList, cancellationToken);

        // Load all existing users from DB
        var allExistingUsers = await _dbContext.Users
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        var existingUsers = allExistingUsers.ToDictionary(u => u.Id);

        var inserted = 0;
        var updated = 0;

        foreach (var user in userList)
        {
            if (!existingUsers.TryGetValue(user.id, out var entity))
            {
                // New user
                entity = new Data.Entities.User
                {
                    Id = user.id,
                    Roles = new List<Data.Entities.Role>() 
                };
                _dbContext.Users.Add(entity);
                inserted++;
            }
            else
            {
                updated++;
            }

            entity.CreateTime = ParseDateTime(user.createTime) ?? (entity.CreateTime == default ? DateTime.UtcNow : entity.CreateTime);
            entity.CreateBy = user.createBy ?? string.Empty;
            entity.CreateApp = user.createApp ?? string.Empty;
            entity.LastUpdateTime = ParseDateTime(user.lastUpdateTime) ?? (entity.LastUpdateTime == default ? DateTime.UtcNow : entity.LastUpdateTime);
            entity.LastUpdateBy = user.lastUpdateBy ?? string.Empty;
            entity.LastUpdateApp = user.lastUpdateApp ?? string.Empty;
            entity.Username = user.username ?? string.Empty;
            entity.Nickname = user.nickname ?? string.Empty;
            entity.IsSuperAdmin = user.isSuperAdmin;

            // Map roles directly to RolesJson
            entity.Roles = user.roles?.Select(roleDto => new Data.Entities.Role
            {
                Id = roleDto.id,
                Name = roleDto.name ?? string.Empty,
                RoleCode = roleDto.roleCode ?? string.Empty,
                IsProtected = string.IsNullOrEmpty(roleDto.isProtected)
                                ? null
                                : bool.TryParse(roleDto.isProtected, out var val) ? val : (bool?)null
            }).ToList() ?? new List<Data.Entities.Role>();
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new UserSyncResultDto
        {
            Total = userList.Count,
            Inserted = inserted,
            Updated = updated
        });
    }

    private async Task SyncRolesToTableAsync(List<UserDto> userList, CancellationToken cancellationToken)
    {
        // Extract all unique roles from user data
        var allRoles = userList
            .Where(u => u.roles != null)
            .SelectMany(u => u.roles!)
            .GroupBy(r => r.id) // Group by ID to get unique roles
            .Select(g => g.First())
            .ToList();

        if (!allRoles.Any())
        {
            _logger.LogWarning("No roles found to sync");
            return;
        }

        _logger.LogInformation("Found {Count} roles from external API", allRoles.Count);

        // Check for duplicate RoleCodes from external API
        var roleCodeGroups = allRoles
            .GroupBy(r => string.IsNullOrWhiteSpace(r.roleCode) ? $"ROLE_{r.id}" : r.roleCode)
            .Where(g => g.Count() > 1)
            .ToList();

        if (roleCodeGroups.Any())
        {
            foreach (var group in roleCodeGroups)
            {
                var duplicateRoles = string.Join(", ", group.Select(r => $"Id={r.id}, Name={r.name}"));
                _logger.LogWarning("Duplicate RoleCode detected: '{RoleCode}' used by: {Roles}", 
                    group.Key, duplicateRoles);
            }
        }

        // Load existing roles from Roles table
        var existingRoles = await _dbContext.Roles
            .AsNoTracking()
            .ToDictionaryAsync(r => r.Id, cancellationToken);

        var addedCount = 0;
        var updatedCount = 0;
        var skippedCount = 0;

        foreach (var roleDto in allRoles)
        {
            // Generate RoleCode if empty/null - use format: ROLE_{ID}
            var roleCode = string.IsNullOrWhiteSpace(roleDto.roleCode) 
                ? $"ROLE_{roleDto.id}" 
                : roleDto.roleCode;

            // Check if this RoleCode already exists in database (for different ID)
            var existingRoleWithSameCode = await _dbContext.Roles
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.RoleCode == roleCode && r.Id != roleDto.id, cancellationToken);

            if (existingRoleWithSameCode != null)
            {
                _logger.LogError(
                    "RoleCode conflict detected! RoleCode '{RoleCode}' already exists for Role Id={ExistingId}, Name={ExistingName}. " +
                    "Cannot add Role Id={NewId}, Name={NewName}. Skipping this role.",
                    roleCode, existingRoleWithSameCode.Id, existingRoleWithSameCode.Name, 
                    roleDto.id, roleDto.name);
                skippedCount++;
                continue;
            }
            
            if (!existingRoles.TryGetValue(roleDto.id, out var existingRole))
            {
                // Add new role
                var newRole = new Data.Entities.Role
                {
                    Id = roleDto.id,
                    Name = roleDto.name ?? string.Empty,
                    RoleCode = roleCode,
                    IsProtected = string.IsNullOrEmpty(roleDto.isProtected)
                        ? null
                        : bool.TryParse(roleDto.isProtected, out var val) ? val : (bool?)null
                };
                _dbContext.Roles.Add(newRole);
                _logger.LogInformation("Adding new role: Id={Id}, Name={Name}, RoleCode={RoleCode}", 
                    newRole.Id, newRole.Name, newRole.RoleCode);
                addedCount++;
            }
            else
            {
                // Update existing role (but keep the existing RoleCode and ID)
                existingRole.Name = roleDto.name ?? existingRole.Name;
                existingRole.IsProtected = string.IsNullOrEmpty(roleDto.isProtected)
                    ? existingRole.IsProtected
                    : bool.TryParse(roleDto.isProtected, out var val) ? val : existingRole.IsProtected;
                
                _dbContext.Roles.Update(existingRole);
                _logger.LogInformation("Updating existing role: Id={Id}, Name={Name}, RoleCode={RoleCode}", 
                    existingRole.Id, existingRole.Name, existingRole.RoleCode);
                updatedCount++;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation(
            "Role sync completed: {Added} added, {Updated} updated, {Skipped} skipped due to conflicts", 
            addedCount, updatedCount, skippedCount);
    }

    [HttpGet("roles")]
    public async Task<ActionResult<List<string>>> GetUserRolesAsync(
        [FromQuery] string username, 
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return BadRequest("Username is required");
        }

        try
        {
            var user = await _dbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Username == username, cancellationToken);

            if (user == null)
            {
                _logger.LogWarning("User {Username} not found", username);
                return NotFound($"User '{username}' not found");
            }

            // Parse roles from RolesJson and extract role names
            var roleNames = user.Roles?.Select(r => r.Name).ToList() ?? new List<string>();
            
            _logger.LogInformation("Retrieved {Count} roles for user {Username}", roleNames.Count, username);
            return Ok(roleNames);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving roles for user {Username}", username);
            return StatusCode(StatusCodes.Status500InternalServerError, "Error retrieving user roles");
        }
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserSummaryDto>>> GetAsync(CancellationToken cancellationToken)
    {
        // Load users with roles first
        var usersWithRoles = await _dbContext.Users
            .AsNoTracking()
            .OrderBy(u => u.Id)
            .ToListAsync(cancellationToken);

        // Project to DTO in-memory after loading from database
        var users = usersWithRoles.Select(u => new UserSummaryDto
        {
            UserName = u.Username,
            rolename = ParseRoleNames(u.RolesJson),
            lastUpdateTime = u.LastUpdateTime.ToString()
        }).ToList();

        _logger.LogInformation("Retrieved {Count} Users", users.Count);
        return Ok(users);
    }

    private static List<string> ParseRoleNames(string rolesJson)
    {
        if (string.IsNullOrWhiteSpace(rolesJson))
            return new List<string>();

        try
        {
            // Try to deserialize as JSON array
            return JsonSerializer.Deserialize<List<string>>(rolesJson) ?? new List<string>();
        }
        catch
        {
            // Fallback: treat as single role name
            return new List<string> { rolesJson };
        }
    }

    private static DateTime? ParseDateTime(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (DateTime.TryParse(value, out var parsed))
        {
            return parsed;
        }

        return null;
    }
}
