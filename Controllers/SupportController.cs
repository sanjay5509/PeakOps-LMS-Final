using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PeakOps.Core;
using PeakOps.Core.Entities;
using PeakOps.Core.Entities.MasterData;

namespace PeakOps.Controllers
{
    public class SupportController : Controller
    {
        private readonly AppDbContext _context;

        public SupportController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
         
            var tickets = await _context.Tickets
                .Include(t => t.Project)
                .Include(t => t.TicketStatus)
                .Include(t => t.TicketPriority)
                .Include(t => t.Assignments).ThenInclude(a => a.AssignedUser)
                .OrderByDescending(t => t.CreatedOn)
                .ToListAsync();

            return View(tickets);
        }

        public IActionResult Create()
        {
            
            ViewData["TicketPriorityId"] = new SelectList(_context.TicketPriorities.Where(x => x.IsActive), "Id", "StatusName");
            ViewData["TicketTypeId"] = new SelectList(_context.TicketTypes.Where(x => x.IsActive), "Id", "TypeName");

          
            
            ViewData["ProjectId"] = new SelectList(_context.Projects.Where(p => p.Status != "Closed"), "Id", "ProjectName");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Ticket model, long AssignedUserId)          
        {
            var currentUser = await _context.Users.FirstOrDefaultAsync();
            if (currentUser == null) return Content("Error: User table khali hai.");

            model.CreatedById = currentUser.Id;
            model.CreatedOn = DateTime.Now;
            model.TicketStatusId = 1;
            model.IsArchived = false;

          
            ModelState.Remove("Project");   
            ModelState.Remove("TicketStatus");
            ModelState.Remove("TicketPriority");
            ModelState.Remove("TicketType");
            ModelState.Remove("CreatedBy");
            ModelState.Remove("Assignments");
            ModelState.Remove("Comments");
            ModelState.Remove("Attachments");
         


            if (!ModelState.IsValid)
            {
                ViewData["ProjectId"] = new SelectList(_context.Projects.Where(p => p.Status != "Closed"), "Id", "ProjectName", model.ProjectId);
                ViewData["TicketPriorityId"] = new SelectList(_context.TicketPriorities.Where(x => x.IsActive), "Id", "StatusName", model.TicketPriorityId);
                ViewData["TicketTypeId"] = new SelectList(_context.TicketTypes.Where(x => x.IsActive), "Id", "TypeName", model.TicketTypeId);
                return View(model);
            }

            try
            {
                _context.Add(model);
                await _context.SaveChangesAsync();

                
                if (AssignedUserId > 0)
                {
                    var assignment = new TicketAssignment
                    {
                        TicketId = model.Id,  
                        AssignedUserId = AssignedUserId, 
                        AssignedById = currentUser.Id,
                        AssignedOn = DateTime.Now
                    };
                    _context.Add(assignment);
                    await _context.SaveChangesAsync();
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                return Content($"Database Error: {ex.Message}");
            }
        }
       
        [HttpGet]
        public async Task<JsonResult> GetProjectUsers(long projectId)
        {
            
            var users = await _context.ProjectAssignments
                .Where(pa => pa.ProjectId == projectId) 
                .Select(pa => new {
                    id = pa.UserId, 
                    name = pa.User.FirstName + " " + pa.User.LastName
                })
                .ToListAsync();

            return Json(users);
        }


        public async Task<IActionResult> Details(long id)
        {
            var ticket = await _context.Tickets
                .Include(t => t.Project)
                .Include(t => t.CreatedBy)
                .Include(t => t.TicketStatus)
                .Include(t => t.Comments).ThenInclude(c => c.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (ticket == null) return NotFound();

            return View(ticket); 
        }


        [HttpPost]
        public async Task<IActionResult> AddComment(long TicketId, string CommentText)
        {
            var currentUser = await _context.Users.FirstOrDefaultAsync();

            var comment = new TicketComment
            {
                TicketId = TicketId,
                UserId = currentUser.Id,
             
                Description = CommentText,
              
                CreatedOn = DateTime.Now
            };

            _context.TicketComments.Add(comment);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = TicketId });
        }
        [HttpPost] 

        public async Task<IActionResult> AddGeneralComment(string CommentText)
        {
            var currentUser = await _context.Users.FirstOrDefaultAsync();

            var comment = new TicketComment
            {
                TicketId = null,
                UserId = currentUser.Id,
                Description = CommentText, 
                CreatedOn = DateTime.Now
            };

            _context.Add(comment);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> EditComment(long CommentId, string NewText)
        {
            var comment = await _context.TicketComments.FindAsync(CommentId);
            if (comment != null)
            {
                comment.Description = NewText; 
                comment.UpdatedOn = DateTime.Now;
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }
        [HttpPost]
       
        public async Task<IActionResult> DeleteComment(long CommentId)
        {
            try
            {
                var comment = await _context.TicketComments.FirstOrDefaultAsync(x => x.Id == CommentId);

                if (comment != null)
                {
                    _context.TicketComments.Remove(comment);
                    await _context.SaveChangesAsync();
                    return Json(new { success = true });
                }
                return Json(new { success = false, message = "Comment nahi mila." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }
    }
    }
