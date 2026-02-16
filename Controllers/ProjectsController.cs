using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PeakOps.Core;
using PeakOps.Core.Entities;
using PeakOps.Core.Interfaces;

namespace PeakOps.Controllers
{
    public class ProjectsController : Controller
    {   
        private readonly AppDbContext _context;
        private readonly IEncryptionService _encryptionService; 

        public ProjectsController(AppDbContext context, IEncryptionService encryptionService)
        {
            _context = context;
            _encryptionService = encryptionService;
        }

        public async Task<IActionResult> Index()
        {
            var projects = await _context.Projects.Where(p => !p.IsDeleted).ToListAsync();
            return View(projects);
        }

       
        [HttpGet]
        public IActionResult Create()
        {
            ViewData["ThemeId"] = new SelectList(_context.Themes.Where(t => t.IsActive), "Id", "ThemeName");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Project model)
        {
            
            model.Status = "New";
            model.IsActive = true;
            model.CreatedOn = DateTime.Now;

           
            ModelState.Remove("Status");
            ModelState.Remove("Assignments");
            ModelState.Remove("Credentials");
            ModelState.Remove("Theme"); 

            if (ModelState.IsValid)
            {
                _context.Projects.Add(model);
                await _context.SaveChangesAsync();
                return RedirectToAction("Details", new { id = model.Id });
            }

            
            ViewData["ThemeId"] = new SelectList(_context.Themes.Where(t => t.IsActive), "Id", "ThemeName", model.ThemeId);

            return View(model);
        }


        public async Task<IActionResult> Details(long? id)
        {
            if (id == null) return NotFound();

            var project = await _context.Projects
                .Include(p => p.Assignments).ThenInclude(a => a.User)
                .Include(p => p.Assignments).ThenInclude(a => a.ProjectRole)
                .Include(p => p.Credentials)
             
                .FirstOrDefaultAsync(m => m.Id == id);

            if (project == null) return NotFound();

            ViewData["Users"] = new SelectList(_context.Users.Where(u => u.IsActive), "Id", "FirstName");
            ViewData["Roles"] = new SelectList(_context.ProjectRoles, "Id", "RoleName");

            return View(project);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAssignment(long ProjectId, long UserId, int ProjectRoleId)
        {
            
            var assignment = new ProjectAssignment
            {
                ProjectId = ProjectId,
                UserId = UserId,
                ProjectRoleId = ProjectRoleId,
                AssignedOn = DateTime.Now
            };

            if (ModelState.IsValid)
            {
                _context.ProjectAssignments.Add(assignment);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Details), new { id = ProjectId });
            }

            return RedirectToAction(nameof(Details), new { id = ProjectId });
        }

        [HttpPost]
        public async Task<IActionResult> AddMember(long ProjectId, long UserId, int RoleId)
        {
            
            var exists = await _context.ProjectAssignments
                .AnyAsync(pa => pa.ProjectId == ProjectId && pa.UserId == UserId);

            if (!exists)
            {
                var assignment = new ProjectAssignment
                {
                    ProjectId = ProjectId,
                    UserId = UserId,
                    ProjectRoleId = RoleId
                };
                _context.ProjectAssignments.Add(assignment);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Details", new { id = ProjectId });
        }

        [HttpGet] 
        public async Task<IActionResult> RemoveMember(long id)
        {
            var assignment = await _context.ProjectAssignments.FindAsync(id);
            if (assignment != null)
            {
                long projectId = assignment.ProjectId;
                _context.ProjectAssignments.Remove(assignment);
                await _context.SaveChangesAsync();
                return RedirectToAction("Details", new { id = projectId });
            }
            return RedirectToAction("Index");
        }

      

        [HttpPost]
        public async Task<IActionResult> AddCredential(long ProjectId, string Title, string Username, string PlainPassword, string HostOrUrl)
        {
           
            string encryptedPassword = _encryptionService.Encrypt(PlainPassword);

            var credential = new ProjectCredential
            {
                ProjectId = ProjectId,
                Title = Title,
                Username = Username,
                EncryptedPassword = encryptedPassword, 
                HostOrUrl = HostOrUrl,
                Notes = "Added via Vault"
            };

            _context.ProjectCredentials.Add(credential);
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", new { id = ProjectId });
        }

        
        [HttpGet]
        public async Task<IActionResult> RevealPassword(long id)
        {
            var credential = await _context.ProjectCredentials.FindAsync(id);
            if (credential == null) return NotFound();

         
            string realPassword = _encryptionService.Decrypt(credential.EncryptedPassword);

          
            return Json(new { password = realPassword });
        }

        
        public async Task<IActionResult> DeleteCredential(long id)
        {
            var cred = await _context.ProjectCredentials.FindAsync(id); 
            if (cred != null)
            {
                long pid = cred.ProjectId;
                _context.ProjectCredentials.Remove(cred);
                await _context.SaveChangesAsync();
                return RedirectToAction("Details", new { id = pid });
            }
            return RedirectToAction("Index");
        }

        
        public async Task<IActionResult> Edit(long? id)
        {
            if (id == null) return NotFound();

            var project = await _context.Projects.FindAsync(id);
            if (project == null) return NotFound();

         
            ViewData["ThemeId"] = new SelectList(_context.Themes.Where(t => t.IsActive), "Id", "ThemeName", project.ThemeId);

            return View(project);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, Project model)
        {
            if (id != model.Id) return NotFound();

          
            var existingProject = await _context.Projects.FindAsync(id);
                
            if (existingProject == null)
            {
                return NotFound();
            }

            
            existingProject.ProjectName = model.ProjectName;  
            existingProject.ProjectDescription = model.ProjectDescription; 
            existingProject.Status = model.Status;
            existingProject.StartDate = model.StartDate;
            existingProject.TargetDate = model.TargetDate;
            existingProject.ThemeId = model.ThemeId;

            

            try
            {
               
                _context.Update(existingProject);
                await _context.SaveChangesAsync();

               
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Projects.Any(e => e.Id == model.Id)) return NotFound();
                else throw;
            }

           
            ViewData["ThemeId"] = new SelectList(_context.Themes.Where(t => t.IsActive), "Id", "ThemeName", model.ThemeId);
            return View(model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            var project = await _context.Projects.FindAsync(id);
            if (project != null)
            {
                
                _context.Projects.Remove(project);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}