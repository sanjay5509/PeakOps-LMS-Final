using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PeakOps.Core;
using PeakOps.Core.Entities;
using PeakOps.Core.ViewModels;
using System.Security.Claims; 

namespace PeakOps.Controllers
{
    public class LeadsController : Controller
    {
        private readonly AppDbContext _context;

        public LeadsController(AppDbContext context)
        {
            _context = context;
        }

        
    
        private void PopulateDropdowns(LeadViewModel model)
        {
            model.StatusList = _context.LeadStatuses
                .Where(x => x.IsActive)
                .Select(x => new SelectListItem { Text = x.StatusName, Value = x.Id.ToString() })
                .ToList();

            model.SourceList = _context.LeadSources
                .Where(x => x.IsActive)
                .Select(x => new SelectListItem { Text = x.Title, Value = x.Id.ToString() })
                .ToList();

            model.StageList = _context.LeadStages
                .Where(x => x.IsActive)
                .Select(x => new SelectListItem { Text = x.StageName, Value = x.Id.ToString() })
                .ToList();
        }


        public IActionResult Index(string searchTerm, int? statusId)
        {
            
            var query = _context.Leads
                .Include(l => l.Status)
                .Include(l => l.Source)
                .Where(l => l.IsActive == true);

            var userIdString = User.FindFirst("UserId")?.Value;
            long.TryParse(userIdString, out long currentUserId);

            
            if (!User.IsInRole("SuperAdmin"))
            {
                
                query = query.Where(l => l.AssignedToId == currentUserId);
            }

            if (!string.IsNullOrEmpty(searchTerm))
            {
               
                query = query.Where(l =>
                    l.LeadTitle.Contains(searchTerm) ||
                    l.ContactPerson.Contains(searchTerm) ||
                    l.Mobile.Contains(searchTerm) ||
                    (l.BusinessName != null && l.BusinessName.Contains(searchTerm))
                );
            }

           
            if (statusId.HasValue)
            {
                query = query.Where(l => l.StatusId == statusId.Value);
            }

            
            var leads = query.OrderByDescending(l => l.CreatedOn).ToList();

           
           
            ViewBag.StatusList = new SelectList(_context.LeadStatuses.Where(x => x.IsActive), "Id", "StatusName", statusId);

            
            ViewBag.CurrentSearch = searchTerm;

            return View(leads);
        }


        [HttpGet]
        [Authorize(Policy = "Add_Edit_User")]
        public IActionResult Create()
        {
            var model = new LeadViewModel();
            PopulateDropdowns(model); 
            return View(model);
        }

        
        [HttpPost]
        [Authorize(Policy = "Add_Edit_User")]
        public IActionResult Create(LeadViewModel model)
        {
            if (ModelState.IsValid)
            {
             
                bool exists = _context.Leads.Any(x => x.Mobile == model.Mobile);
                if (exists)
                {
                    ModelState.AddModelError("Mobile", "Ye Mobile Number pehle se exist karta hai!");
                    PopulateDropdowns(model);
                    return View(model);
                }

                var userIdString = User.FindFirst("UserId")?.Value;
                long.TryParse(userIdString, out long userId);

                
                var lead = new Lead
                {
                    LeadTitle = model.LeadTitle,
                    BusinessName = model.BusinessName,
                    ContactPerson = model.ContactPerson,
                    Mobile = model.Mobile,
                    AlternateMobile = model.AlternateMobile,
                    Email = model.Email,
                    City = model.City,
                    IndustryName = model.IndustryName,

                    StatusId = model.StatusId,
                    SourceId = model.SourceId,
                    LeadStageId = model.LeadStageId,

                    AssignedToId = userId,
                    CreatedBy = userId,
                    CreatedOn = DateTime.Now,
                    IsActive = true
                };

                _context.Leads.Add(lead);
                _context.SaveChanges();

                return RedirectToAction("Index"); 
            }

            
            PopulateDropdowns(model);
            return View(model);
        }

        
        [HttpGet]
        [Authorize(Policy = "Add_Edit_User")]
        public IActionResult Edit(long id)
        {
            var lead = _context.Leads.Find(id);
            if (lead == null) return NotFound();

            
            var model = new LeadViewModel
            {
                Id = lead.Id,
                LeadTitle = lead.LeadTitle,
                BusinessName = lead.BusinessName,
                ContactPerson = lead.ContactPerson,
                Mobile = lead.Mobile,
                AlternateMobile = lead.AlternateMobile,
                Email = lead.Email,
                City = lead.City,
                IndustryName = lead.IndustryName,
                StatusId = lead.StatusId,
                SourceId = lead.SourceId,
                LeadStageId = lead.LeadStageId
            };

            PopulateDropdowns(model); 
            return View(model);
        }

       

        [HttpPost]
        [Authorize(Policy = "Add_Edit_User")]
        public IActionResult Edit(LeadViewModel model)
        {
            if (ModelState.IsValid)
            {
                var lead = _context.Leads.Find(model.Id);
                if (lead == null) return NotFound();

               
                lead.LeadTitle = model.LeadTitle;
                lead.BusinessName = model.BusinessName;
                lead.ContactPerson = model.ContactPerson;
                lead.Mobile = model.Mobile;
                lead.AlternateMobile = model.AlternateMobile;
                lead.Email = model.Email;
                lead.City = model.City;
                lead.IndustryName = model.IndustryName;
                lead.StatusId = model.StatusId;
                lead.SourceId = model.SourceId;
                lead.LeadStageId = model.LeadStageId;

                lead.ModifiedOn = DateTime.Now;

                _context.Leads.Update(lead);
                _context.SaveChanges();

                return RedirectToAction("Index");
            }

            PopulateDropdowns(model);
            return View(model);
        }

        [Authorize(Policy = "Delete_User")]
        public IActionResult Delete(long id)
        {
            var lead = _context.Leads.Find(id);
            if (lead != null)
            {
                
                lead.IsActive = false;
                _context.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        public IActionResult Details(long id)
        {
            var lead = _context.Leads
                .Include(l => l.Status)
                .Include(l => l.Source)
                .Include(l => l.LeadStage)
                .Include(l => l.AssignedTo)
             
                .Include(l => l.FollowUps.OrderByDescending(f => f.FollowUpDate))
                    .ThenInclude(f => f.FollowUpType)
                .Include(l => l.FollowUps)
                    .ThenInclude(f => f.FollowUpStatus)
                .FirstOrDefault(l => l.Id == id);

            if (lead == null) return NotFound();

            
            ViewBag.FollowUpTypes = new SelectList(_context.LeadFollowUpTypes.Where(x => x.IsActive), "Id", "TypeName");

          
            ViewBag.FollowUpStatuses = new SelectList(_context.FollowUpStatuses.Where(x => x.IsActive), "Id", "StatusName");

            return View(lead);
        }


        [HttpPost]
        public IActionResult AddFollowUp(long leadId, int followUpTypeId, int followUpStatusId,
                                 string remarks, string response,
                                 DateTime followUpDate, DateTime? nextFollowUpDate)
        {
          
            if (followUpTypeId == 0 || followUpStatusId == 0)
            {
                
                return RedirectToAction("Details", new { id = leadId });
            }

         
            var followUp = new LeadFollowUp
            {
                LeadId = leadId,
                FollowUpTypeId = followUpTypeId,
                FollowUpStatusId = followUpStatusId,
                Remarks = remarks,
                Response = response,
                FollowUpDate = followUpDate,
                NextFollowUpDate = nextFollowUpDate,
                CreatedById = 1,
                CreatedOn = DateTime.Now
            };

            _context.LeadFollowUps.Add(followUp);

         
            var mainLead = _context.Leads.Find(leadId);
            if (mainLead != null)
            {
                
                mainLead.ModifiedOn = DateTime.Now;
                _context.Leads.Update(mainLead);
            }

            _context.SaveChanges();

            return RedirectToAction("Details", new { id = leadId });
        }

        public IActionResult DeleteFollowUp(long id)
        {
            var followUp = _context.LeadFollowUps.Find(id);

            if (followUp != null)
            {
                long leadId = followUp.LeadId; 

                _context.LeadFollowUps.Remove(followUp);
                _context.SaveChanges();

                return RedirectToAction("Details", new { id = leadId });
            }

            return RedirectToAction("Index");
        }


        public IActionResult DeleteActivity(long id)
        {
       
            var activity = _context.LeadActivities.Find(id);

            
            if (activity != null)
            {
                long leadId = activity.LeadId;

                _context.LeadActivities.Remove(activity);
                _context.SaveChanges();

               
                return RedirectToAction("Details", new { id = leadId });
            }

           
            return RedirectToAction("Index");
        }
    }
}