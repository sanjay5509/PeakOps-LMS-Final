using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using PeakOps.Core;
using System.Reflection;
using System.Collections;

namespace PeakOps.Controllers
{   
    [Authorize(Roles = "SuperAdmin")]
    public class MasterController : Controller
    {
        private readonly AppDbContext _context;

        public MasterController(AppDbContext context)
        {
            _context = context;
        }

       
        private Type GetTableType(string tableName)
        {
           
            var property = _context.GetType().GetProperty(tableName);

            if (property != null)
            {
               
                return property.PropertyType.GetGenericArguments()[0];
            }

            return null; 
        }

       
        public IActionResult Index(string tableName)
        {
            if (string.IsNullOrEmpty(tableName)) return BadRequest("Table Name zaroori hai.");

            var data = _context.GetTableData(tableName);
            ViewBag.TableName = tableName;
            return View(data);
        }

       
        [HttpGet]
        public IActionResult Create(string tableName)
        {
            if (string.IsNullOrEmpty(tableName)) return BadRequest();

            
            Type targetType = GetTableType(tableName);

            if (targetType == null)
            {
                return BadRequest($"Table '{tableName}' DbContext mein nahi mili. Spelling check karo.");
            }

            
            var instance = Activator.CreateInstance(targetType);
            ViewBag.TableName = tableName;
            return View(instance);
        }

        
        [HttpPost]
        public IActionResult Create(string tableName, IFormCollection form)
        {
            Type targetType = GetTableType(tableName);
            if (targetType == null) return BadRequest("Class not found.");

            var instance = Activator.CreateInstance(targetType);

          
            foreach (var prop in targetType.GetProperties())
            {
                if (form.ContainsKey(prop.Name))
                {
                    string value = form[prop.Name];

                  
                    if (prop.PropertyType == typeof(int))
                    {
                        prop.SetValue(instance, int.Parse(value));
                    }
                    else if (prop.PropertyType == typeof(bool))
                    {
                        prop.SetValue(instance, value.Contains("true") || value.Contains("on"));
                    }
                    else if (prop.PropertyType == typeof(string))
                    {
                        prop.SetValue(instance, value);
                    }
                }
            }

            _context.Add(instance);
            _context.SaveChanges();

            return RedirectToAction("Index", new { tableName = tableName });
        }

        [HttpGet]
        public IActionResult Edit(string tableName, int id)
        {
            Type targetType = GetTableType(tableName);
            if (targetType == null) return BadRequest("Type not found");

            var entity = _context.Find(targetType, id);

            if (entity == null) return NotFound();

            ViewBag.TableName = tableName;
            return View(entity);
        }

       
        [HttpPost]
        public IActionResult Edit(string tableName, int id, IFormCollection form)
        {
            Type targetType = GetTableType(tableName);
            if (targetType == null) return BadRequest("Type not found");

            
            var entity = _context.Find(targetType, id);
            if (entity == null) return NotFound();

            
            foreach (var prop in targetType.GetProperties())
            {
              
                if (prop.Name == "Id" || prop.Name == "CreatedOn") continue;

                if (form.ContainsKey(prop.Name))
                {
                    string value = form[prop.Name];
                    if (prop.PropertyType == typeof(int)) prop.SetValue(entity, int.Parse(value));
                    else if (prop.PropertyType == typeof(string)) prop.SetValue(entity, value);
                    else if (prop.PropertyType == typeof(bool)) prop.SetValue(entity, value.Contains("true") || value.Contains("on"));
                }
                else if (prop.PropertyType == typeof(bool))
                {
                    
                    prop.SetValue(entity, false);
                }
            }

           
            var modifiedProp = targetType.GetProperty("ModifiedOn");
            if (modifiedProp != null) modifiedProp.SetValue(entity, DateTime.Now);

            _context.Update(entity);
            _context.SaveChanges();

            return RedirectToAction("Index", new { tableName = tableName });
        }

        
        public IActionResult Delete(string tableName, int id)
        {
            Type targetType = GetTableType(tableName);
            if (targetType == null) return BadRequest("Type not found");

            var entity = _context.Find(targetType, id);
            if (entity != null)
            {
                _context.Remove(entity);
                _context.SaveChanges();
            }
            return RedirectToAction("Index", new { tableName = tableName });
        }
    }
}