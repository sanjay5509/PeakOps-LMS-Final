using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PeakOps.Core;
using PeakOps.Core.Entities;

namespace PeakOps.Controllers
{
    public class ThemesController : Controller
    {
        private readonly AppDbContext _context;

        public ThemesController(AppDbContext context)
        {
            _context = context;
        }

        
        public async Task<IActionResult> Index()
        {
            var themes = await _context.Themes.OrderByDescending(t => t.CreatedOn).ToListAsync();
            return View(themes);
        }

        
        [HttpGet]
        //[HasPermission("Add_Edit_User")]  

        [Authorize(Policy = "Add_Edit_User")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        //[HasPermission("Add_Edit_User")]
        [Authorize(Policy = "Add_Edit_User")]
        public async Task<IActionResult> Create(Theme model)
        {
            
            var lastId = _context.Themes.Any() ? _context.Themes.Max(t => t.Id) : 0;

            
            long nextId = lastId + 1;

            
            model.ThemeCode = "TH-" + nextId.ToString("D5");

          

            model.CreatedOn = DateTime.Now;

           
            ModelState.Remove("ThemeCode");

            if (ModelState.IsValid)
            {
                _context.Add(model);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        [HttpGet]
        [Authorize(Policy = "Add_Edit_User")]
        public async Task<IActionResult> Edit(int id)
        {
            var theme = await _context.Themes.FindAsync(id);
            if (theme == null) return NotFound();
            return View(theme);
        }

        [HttpPost]
        [Authorize(Policy = "Add_Edit_User")]
        public async Task<IActionResult> Edit(Theme model)
        {
            if (ModelState.IsValid)
            {
                _context.Themes.Update(model);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }
    }
}