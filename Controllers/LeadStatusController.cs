using Microsoft.AspNetCore.Mvc;
using PeakOps.Core; 
using PeakOps.Core.Entities.MasterData; 
using Microsoft.AspNetCore.Authorization;
using PeakOps.Core.ViewModels.MasterData;

namespace PeakOps.Controllers
{
    [Authorize(Roles = "SuperAdmin")] 
    public class LeadStatusController : Controller
    {
        private readonly AppDbContext _context;

        public LeadStatusController(AppDbContext context)
        {
            _context = context;
        }

    
        public IActionResult Index()
        {
            var statusList = _context.LeadStatuses.ToList();
            return View(statusList);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Create(LeadStatusViewModel model) 
        {
            if (ModelState.IsValid)
            {
                
                var entity = new LeadStatus
                {
                    StatusName = model.StatusName,
                    DisplayOrder = model.DisplayOrder,
                    IsActive = model.IsActive,

                    
                };

                _context.LeadStatuses.Add(entity);
                _context.SaveChanges();

                return RedirectToAction("Index");
            }

         
            return View(model);
        }


        [HttpGet]
        public IActionResult Edit(int id)
        {
            
            var entity = _context.LeadStatuses.Find(id);
            if (entity == null) return NotFound();

            
            var model = new LeadStatusViewModel
            {
                Id = entity.Id,
                StatusName = entity.StatusName,
                DisplayOrder = entity.DisplayOrder,
                IsActive = entity.IsActive
            };

            return View(model);
        }

        [HttpPost]
        public IActionResult Edit(LeadStatusViewModel model)
        {
            if (ModelState.IsValid)
            {
               
                var entity = _context.LeadStatuses.Find(model.Id);
                if (entity == null) return NotFound();

               
                entity.StatusName = model.StatusName;
                entity.DisplayOrder = model.DisplayOrder;
                entity.IsActive = model.IsActive;
                

                
                _context.LeadStatuses.Update(entity);
                _context.SaveChanges();

                return RedirectToAction("Index");
            }
            return View(model);
        }


        public IActionResult Delete(int id)
        {
            var entity = _context.LeadStatuses.Find(id);
            if (entity != null)
            {
                _context.LeadStatuses.Remove(entity);
                _context.SaveChanges();
            }
            return RedirectToAction("Index");
        }
    }
}