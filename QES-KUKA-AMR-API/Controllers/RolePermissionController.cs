using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QES_KUKA_AMR_API.Data;
using QES_KUKA_AMR_API.Data.Entities;
using QES_KUKA_AMR_API.Models.Page;
using QES_KUKA_AMR_API.Models.User;
using System;

namespace QES_KUKA_AMR_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RolePermissionController : Controller
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<RolePermissionController> _logger;


        public RolePermissionController(ApplicationDbContext context, ILogger<RolePermissionController> logger,IHttpClientFactory httpClientFactory)
        {
            _dbContext = context;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }


        [HttpPost("add")]
        public async Task<IActionResult> AddOrUpdateRolePermissionAsync([FromBody] RolePermissionRequest request, CancellationToken cancellationToken)
        {
            if (request == null || request.RoleId <= 0)
                return BadRequest("Invalid role or permissions.");

            var existingPermissions = await _dbContext.RolePermissions
                .Where(rp => rp.RoleId == request.RoleId)
                .ToListAsync(cancellationToken);

            if (existingPermissions.Any())
            {
                // Update existing permissions
                _dbContext.RolePermissions.RemoveRange(existingPermissions);

                foreach (var pageId in request.PageIds)
                {
                    _dbContext.RolePermissions.Add(new RolePermission
                    {
                        RoleId = request.RoleId,
                        PageId = pageId
                    });
                }

                await _dbContext.SaveChangesAsync(cancellationToken);
                return Ok("Permissions updated successfully.");
            }
            else
            {
                // Add new permissions
                foreach (var pageId in request.PageIds)
                {
                    _dbContext.RolePermissions.Add(new RolePermission
                    {
                        RoleId = request.RoleId,
                        PageId = pageId
                    });
                }

                await _dbContext.SaveChangesAsync(cancellationToken);
                return Ok("Permissions added successfully.");
            }
        }

        //Get Role
        [HttpGet("Roles")]
        public async Task<ActionResult<IEnumerable<Data.Entities.Role>>> GetRolesAsync(CancellationToken cancellationToken)
        {
            var roles = await _dbContext.Roles.AsNoTracking().OrderBy(r => r.Id).Select(r => new RoleSummaryDto
            {
                Id = r.Id,
                Name = r.Name
            }).ToListAsync(cancellationToken);

            _logger.LogInformation("{Count} Roles", roles.Count);

            return Ok(roles);
        }

        // Get Page 
        [HttpGet("Pages")]
        public async Task<ActionResult<IEnumerable<Page>>> GetPagesAsync(CancellationToken cancellationToken)
        {
            var pages = await _dbContext.Pages.AsNoTracking().OrderBy(p => p.Id).Select(p => new PageSummaryDto
            {
                Id = p.Id,
                PageName = p.PageName,
                PagePath = p.PagePath
            }).ToListAsync(cancellationToken);

            _logger.LogInformation("{Count} Pages", pages.Count);

            return Ok(pages);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<RolePermissionSummary>>> GetRolePermissionAsync(CancellationToken cancellationToken)
        {
            var rolePermissions = await (
                from rp in _dbContext.RolePermissions
                join r in _dbContext.Roles on rp.RoleId equals r.Id
                join p in _dbContext.Pages on rp.PageId equals p.Id
                group p by new { rp.RoleId, r.Name } into g
                select new RolePermissionSummary
                {
                    RoleId = g.Key.RoleId,
                    RoleName = g.Key.Name,
                    PageNames = g.Select(x => x.PageName).ToList()
                }
            ).ToListAsync(cancellationToken);

            return Ok(rolePermissions);
        }

        [HttpGet("{roleId}")]
        public async Task<ActionResult<RolePermissionDetail>> GetRolePermissionByIdAsync(int roleId, CancellationToken cancellationToken)
        {
            var role = await _dbContext.Roles
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == roleId, cancellationToken);

            if (role == null)
                return NotFound($"Role with ID {roleId} not found");

            var pageIds = await _dbContext.RolePermissions
                .Where(rp => rp.RoleId == roleId)
                .Select(rp => rp.PageId)
                .ToListAsync(cancellationToken);

            return Ok(new RolePermissionDetail
            {
                RoleId = roleId,
                RoleName = role.Name,
                PageIds = pageIds
            });
        }

        [HttpDelete("{roleId}")]
        public async Task<IActionResult> DeleteRolePermissionAsync(int roleId, CancellationToken cancellationToken)
        {
            var permissions = await _dbContext.RolePermissions
                .Where(rp => rp.RoleId == roleId)
                .ToListAsync(cancellationToken);

            if (!permissions.Any())
                return NotFound($"No permissions found for role ID {roleId}");

            _dbContext.RolePermissions.RemoveRange(permissions);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Deleted {Count} permissions for role ID {RoleId}", permissions.Count, roleId);
            return Ok($"Deleted all permissions for role ID {roleId}");
        }
    }
}
