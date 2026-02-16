using Microsoft.AspNetCore.Mvc;
using PeakOps.Core.Entities;
using PeakOps.Core;
using PeakOps.Core.ViewModels;
using Microsoft.EntityFrameworkCore;

public class PermissionsController : Controller
{
    private readonly AppDbContext _context;
    public PermissionsController(AppDbContext context) { _context = context; }

   
    public IActionResult Index()
    {
        var permissions = _context.Permissions.ToList();
        return View(permissions);
    }

 
    public IActionResult Create()
    {
        
        var parents = _context.Permissions.Where(p => p.ParentId == null).ToList();

        
        ViewBag.Parents = parents ?? new List<PeakOps.Core.Entities.Permission>();

        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Create(Permission permission)
    {
        
        if (ModelState.IsValid)
        {
            _context.Add(permission);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        
        return View(permission);
    }

    public IActionResult Delete(int id)
    {
        var p = _context.Permissions.Find(id);
        if (p != null)
        {
            _context.Permissions.Remove(p);
            _context.SaveChanges();
        }
        return RedirectToAction("Index");
    }

    

public IActionResult RolePermissions(int? roleId)
{

    ViewBag.Roles = _context.Roles.Where(r => r.IsActive).ToList();

   
    var allPermissions = _context.Permissions.ToList();

        
    var assignedIds = roleId.HasValue
        ? _context.RolePermissions
            .Where(rp => rp.RoleId == roleId.Value && rp.IsAllowed)
            .Select(rp => rp.PermissionId)
            .ToList()
        : new List<int>();

    
    var viewModel = new PermissionViewModel
    {
        RoleId = roleId ?? 0,
        AllPermissions = allPermissions,
        AssignedPermissionIds = assignedIds
    };

    return View(viewModel);     
}

[HttpPost]
    public IActionResult SaveRolePermissions(int roleId, List<int> permissionIds)
    {
        
        var oldPermissions = _context.RolePermissions.Where(rp => rp.RoleId == roleId).ToList();
        _context.RolePermissions.RemoveRange(oldPermissions);

      
        if (permissionIds != null)
        {
            foreach (var pId in permissionIds)
            {
                _context.RolePermissions.Add(new RolePermission
                {
                    RoleId = roleId,
                    PermissionId = pId, 
                    IsAllowed = true
                   
                });
            }
        }

        _context.SaveChanges();
        return RedirectToAction("RolePermissions", new { roleId = roleId });
    }

    [HttpGet]
    public JsonResult GetPermissionTree(int roleId)
    {
  
        var permissionsWithAccess = (from p in _context.Permissions
                                     join rp in _context.RolePermissions.Where(x => x.RoleId == roleId)
                                     on p.Id equals rp.PermissionId into joined
                                     from subRp in joined.DefaultIfEmpty()
                                        
                                     select new
                                     {
                                         Id = p.Id,
                                         PermissionName = p.PermissionName,
                                         ParentId = p.ParentId,
                                         IsAllowed = subRp != null && subRp.IsAllowed
                                     }).ToList();

     
        var tree = permissionsWithAccess
            .Where(p => p.ParentId == null)
            .Select(p => new {
                id = p.Id,
                permissionName = p.PermissionName,
                isAllowed = p.IsAllowed,
                children = GetChildren(p.Id, permissionsWithAccess)
            }).ToList();

        return Json(tree);
    }

   
    private object GetChildren(int parentId, IEnumerable<dynamic> allData)
    {
        var children = allData.Where(x => x.ParentId == parentId).ToList();
        return children.Any() ? children.Select(x => new {
            id = x.Id,
            permissionName = x.PermissionName,
            isAllowed = x.IsAllowed,
            children = GetChildren(x.Id, allData)
        }).ToList() : null;
    }


    public async Task<IActionResult> UserMapping(long? userId)
    {
        ViewBag.Users = await _context.Users.Where(u => !u.IsDeleted && u.IsActive).ToListAsync();
        ViewBag.SelectedUserId = userId;

        if (userId.HasValue)
        {
            var allPermissions = await _context.Permissions.ToListAsync();
           
            return View("UserMapping", allPermissions);
        }

        return View("UserMapping", new List<Permission>());
    }


    [HttpPost]
    public async Task<IActionResult> SaveUserOverride(long userId, int permissionId, bool isAllowed)
    {
        var existing = await _context.UserPermissions
            .FirstOrDefaultAsync(up => up.UserId == userId && up.PermissionId == permissionId);

        if (existing != null)
        {
            existing.IsAllowed = isAllowed; 
        }
        else
        {
            _context.UserPermissions.Add(new UserPermission
            {
                UserId = userId,
                PermissionId = permissionId,
                IsAllowed = isAllowed
            });
        }

        await _context.SaveChangesAsync();
        return Json(new { success = true });
    }
    [HttpGet]
    public async Task<IActionResult> GetUserOverrides(long userId)
    {
       
        var overrides = await _context.UserPermissions
            .Where(up => up.UserId == userId)
            .Select(up => new { up.PermissionId, up.IsAllowed })
            .ToListAsync();

        return Json(overrides);
    }
    [HttpPost]
    public async Task<IActionResult> ResetUserPermissions(long userId)
    {
        var overrides = _context.UserPermissions.Where(up => up.UserId == userId);
        _context.UserPermissions.RemoveRange(overrides);
        await _context.SaveChangesAsync();
        return Json(new { success = true });
    }
}