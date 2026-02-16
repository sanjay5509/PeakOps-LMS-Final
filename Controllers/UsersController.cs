using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PeakOps.Core;
using PeakOps.Core.Entities;
using PeakOps.Core.Entities.MasterData;
using PeakOps.Services;

namespace PeakOps.Controllers
{
    [Authorize]
    public class UsersController : Controller
    {
        private readonly AppDbContext _context;

        public UsersController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var users = _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .Where(u => !u.IsDeleted)
                .OrderByDescending(u => u.CreatedOn)
                .ToList();

            return View(users);
        }
        //[HasPermission("Add_Edit_User")]
        [Authorize(Policy = "Add_Edit_User")]
        public IActionResult Create()
        {
            ViewBag.Roles = new SelectList(_context.Roles.Where(r => r.IsActive), "Id", "RoleName");
            return View();
        }

        [HttpPost]
        //[HasPermission("Add_Edit_User")]
        [Authorize(Policy = "Add_Edit_User")]
        public IActionResult Create(User user, int SelectedRoleId)
        {
            if (_context.Users.Any(u => u.Email == user.Email))
            {
                ModelState.AddModelError("Email", "Ye Email pehle se exist karta hai!");
                ViewBag.Roles = new SelectList(_context.Roles.Where(r => r.IsActive), "Id", "RoleName");
                return View(user);
            }

            user.CreatedOn = DateTime.Now;
            user.ModifiedOn = null;
            user.IsActive = true;
            user.IsDeleted = false;

            user.InviteToken = Guid.NewGuid().ToString();
            user.InviteSentOn = DateTime.Now;

            _context.Users.Add(user);
            _context.SaveChanges();

            if (SelectedRoleId > 0)
            {
                var userRole = new UserRole
                {
                    UserId = user.Id,
                    RoleId = SelectedRoleId,
                    CreatedOn = DateTime.Now
                };

                _context.UserRoles.Add(userRole);
                _context.SaveChanges();
            }
            string link = Url.Action("SetPassword", "Users",
                new { email = user.Email, token = user.InviteToken },
                protocol: Request.Scheme);

            TempData["InviteLink"] = link;
            TempData["Message"] = "User ban gaya! Password set karne ke liye neeche di gayi link copy karein.";
            //try
            //{
            //    string link = Url.Action("SetPassword", "Users",
            //  new { email = user.Email, token = user.InviteToken },
            //  protocol: Request.Scheme);

            //    var emailService = new EmailService();
            //    emailService.SendSetPasswordEmail(user.Email, link);
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine("Email Error: " + ex.Message);  
            //}

            return RedirectToAction("Index");
        }

        
        public IActionResult Edit(long id)
        {
            var user = _context.Users
                .Include(u => u.UserRoles)
                .FirstOrDefault(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            var currentRoleId = user.UserRoles.FirstOrDefault()?.RoleId ?? 0;

            ViewBag.Roles = new SelectList(_context.Roles.Where(r => r.IsActive), "Id", "RoleName", currentRoleId);

            return View(user);
        }

        [HttpPost]
        public IActionResult Edit(long id, User user, int SelectedRoleId)
        {
            if (id != user.Id)
            {
                return NotFound();
            }

            var existingUser = _context.Users
                .Include(u => u.UserRoles)
                .FirstOrDefault(u => u.Id == id);

            if (existingUser == null)
            {
                return NotFound();
            }

            existingUser.FirstName = user.FirstName;
            existingUser.LastName = user.LastName;
            existingUser.Mobile = user.Mobile;
            existingUser.Designation = user.Designation;

            existingUser.ModifiedOn = DateTime.Now;

            var oldRole = existingUser.UserRoles.FirstOrDefault();
            if (oldRole != null)
            {
                _context.UserRoles.Remove(oldRole);
            }

            if (SelectedRoleId > 0)
            {
                var newRole = new UserRole
                {
                    UserId = existingUser.Id,
                    RoleId = SelectedRoleId,
                    CreatedOn = DateTime.Now
                };
                _context.UserRoles.Add(newRole);
            }

            _context.SaveChanges();
            return RedirectToAction("Index");
        }

        //[HasPermission("Delete_User")]
        [Authorize(Policy = "Delete_User")]
        public IActionResult Delete(long id)
        {
            var user = _context.Users.Find(id);

            if (user == null)
            {
                return NotFound();
            }

            user.IsDeleted = true;
            user.IsActive = false;

            user.ModifiedOn = DateTime.Now;

            _context.SaveChanges();

            return RedirectToAction("Index");
        }


        [HttpGet]
        public IActionResult SetPassword(string email, string token)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
            {
                return BadRequest("Invalid Link");
            }

            var model = new SetPasswordViewModel
            {
                Email = email,
                Token = token
            };

            return View(model);
        }


        [HttpPost]
        public IActionResult SetPassword(SetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = _context.Users.FirstOrDefault(u => u.Email == model.Email && u.InviteToken == model.Token);

            if (user == null)
            {
                ViewBag.Message = "Invalid or Expired Link!";
                return View(model);
            }

            user.PasswordHash = model.NewPassword;

            user.InviteToken = null;
            user.IsActive = true;

            _context.SaveChanges();

            return RedirectToAction("Login", "Auth");
        }



    } 

    public class SetPasswordViewModel
    {
        public string Email { get; set; }
        public string Token { get; set; }
        public string NewPassword { get; set; }
        public string ConfirmPassword { get; set; }
    }

}