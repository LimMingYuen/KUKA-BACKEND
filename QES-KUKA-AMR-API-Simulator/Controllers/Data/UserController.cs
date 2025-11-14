using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using QES_KUKA_AMR_API_Simulator.Models;
using QES_KUKA_AMR_API_Simulator.Models.MobileRobot;
using QES_KUKA_AMR_API_Simulator.Models.User;
using System.Data;
using System.Xml.Linq;

namespace QES_KUKA_AMR_API_Simulator.Controllers.Data;

[ApiController]
[Authorize]
[Route("api/v1/data/sys-user")]
public class UserController : ControllerBase
{
    private static readonly IReadOnlyList<UserDto> Users = new[]
    {
        new UserDto
        {
            id = 1,
            createTime = "2025-08-21 14:32:26",
            createBy = "sys",
            createApp = "",
            lastUpdateTime = "2025-08-21 14:32:26",
            lastUpdateBy = "sys",
            lastUpdateApp = "",
            username = "admin",
            nickname = "admin",
            isSuperAdmin = 1,
            roles = new List<Role>
              {
                new Role
                {
                    id = 1,
                    name = "administrator",
                    roleCode = "administrator",
                    isProtected = null
                }
              }
        },
        new UserDto
        {
              id = 12,
              createTime = "2025-10-09 20:37:32",
              createBy = "admin",
              createApp = "SysUserManager:58",
              lastUpdateTime = "2025-10-09 20:37:32",
              lastUpdateBy = "admin",
              lastUpdateApp = "SysUserManager:58",
              username = "2333",
              nickname = "lim",
              isSuperAdmin = 0,
              roles = new List<Role>
                {
                  new Role
                  {
                      id = 3,
                      name = "normal",
                      roleCode = "normal",
                      isProtected = null
                  }
                }
            }
    };

    [HttpPost("list")]
    public ActionResult<ApiResponse<UserPage>> GetUserList([FromBody] UserRequest request)
    {
        if (request.PageSize <= 0)
        {
            request.PageSize = 10000;
        }

        if (request.PageNum <= 0)
        {
            request.PageNum = 1;
        }
        var totalRecords = Users.Count;
        var totalPages = (int)Math.Ceiling((double)totalRecords / request.PageSize);

        var skip = (request.PageNum - 1) * request.PageSize;
        var pagedUser = Users.Skip(skip).Take(request.PageSize).ToList();

        var response = new ApiResponse<UserPage>
        {
            Succ = true,
            Code = 200,
            Msg = string.Empty,
            Data = new UserPage
            {
                TotalPages = totalPages,
                TotalRecords = totalRecords,
                Page = request.PageNum,
                Size = request.PageSize,
                Content = pagedUser
            }
        };

        return Ok(response);
    }
}

