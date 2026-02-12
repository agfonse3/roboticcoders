using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoboticCoders.Data;
using RoboticCoders.Models;

namespace RoboticCoders.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminModuleController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminModuleController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Create(int courseId)
        {
            ViewBag.CourseId = courseId;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Module module)
        {
            _context.Modules.Add(module);
            await _context.SaveChangesAsync();

            return RedirectToAction("Modules", "AdminCourse", new { id = module.CourseId });
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var module = await _context.Modules.FindAsync(id);
            if (module == null) return NotFound();

            return View(module);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Module module)
        {
            var existing = await _context.Modules.FindAsync(module.Id);
            if (existing == null) return NotFound();

            existing.Title = module.Title;

            _context.Modules.Update(existing);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Módulo actualizado correctamente.";
            return RedirectToAction("Modules", "AdminCourse", new { id = existing.CourseId });
        }

       public async Task<IActionResult> Lessons(int id)
        {
            var module = await _context.Modules
                .Include(m => m.Lessons.OrderBy(l => l.Id))
                .FirstOrDefaultAsync(m => m.Id == id);

            if (module == null) return NotFound();

            return View(module);
        }
    }
}
