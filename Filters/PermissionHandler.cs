using Microsoft.AspNetCore.Authorization;
using PeakOps.Core;
using PeakOps.Core.Entities; 
using System.Security.Claims;
using Microsoft.EntityFrameworkCore; 
using System.Linq;

public class PermissionRequirement : IAuthorizationRequirement
{
    public string Permission { get; }
    public PermissionRequirement(string permission) { Permission = permission; }
}

public class PermissionHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly AppDbContext _db;
    public PermissionHandler(AppDbContext db) { _db = db; }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);

        if (userIdClaim != null && long.TryParse(userIdClaim.Value, out long userId))
        {
           
            var userOverride = await (from up in _db.UserPermissions
                                      join p in _db.Permissions on up.PermissionId equals p.Id
                                      where up.UserId == userId && p.PermissionKey == requirement.Permission
                                      select (bool?)up.IsAllowed).FirstOrDefaultAsync();

            if (userOverride.HasValue)
            {
                
                if (userOverride.Value)
                {
                    context.Succeed(requirement);
                }
                return; 
            }

            
         
            var hasRoleAccess = await (from ur in _db.UserRoles
                                       join rp in _db.RolePermissions on ur.RoleId equals rp.RoleId
                                       join p in _db.Permissions on rp.PermissionId equals p.Id
                                       where ur.UserId == userId
                                             && p.PermissionKey == requirement.Permission
                                             && rp.IsAllowed == true
                                       select p).AnyAsync();

            if (hasRoleAccess)
            {
                context.Succeed(requirement);
            }
        }
    }
}