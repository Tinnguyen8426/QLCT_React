using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace QuanLyTiemCatToc.Controllers.Api;

[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Staff")]
public abstract class StaffApiControllerBase : ControllerBase
{
    protected int? GetStaffId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId) || !int.TryParse(userId, out var staffId))
        {
            return null;
        }

        return staffId;
    }
}
