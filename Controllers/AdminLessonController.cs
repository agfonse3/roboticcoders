using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoboticCoders.Data;
using RoboticCoders.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace RoboticCoders.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminLessonController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public AdminLessonController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [HttpGet]
        public IActionResult Create(int moduleId)
        {
            ViewBag.ModuleId = moduleId;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Lesson lesson, IFormFile? slides)
        {
            if (slides != null)
            {
                var folder = Path.Combine(_env.WebRootPath, "uploads", "slides");
                Directory.CreateDirectory(folder);

                var fileName = Guid.NewGuid() + Path.GetExtension(slides.FileName);
                var path = Path.Combine(folder, fileName);

                using var stream = new FileStream(path, FileMode.Create);
                await slides.CopyToAsync(stream);

                lesson.SlideUrl = "/uploads/slides/" + fileName;
            }

            _context.Lessons.Add(lesson);
            await _context.SaveChangesAsync();

            return RedirectToAction("Lessons", "AdminModule", new { id = lesson.ModuleId });
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var lesson = await _context.Lessons.FindAsync(id);
            if (lesson == null) return NotFound();

            return View(lesson);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Lesson lesson, IFormFile? slides)
        {
            var existing = await _context.Lessons.FindAsync(lesson.Id);
            if (existing == null) return NotFound();

            existing.Title = lesson.Title;
            existing.Content = lesson.Content;
            existing.VideoUrl = lesson.VideoUrl;

            if (slides != null)
            {
                var folder = Path.Combine(_env.WebRootPath, "uploads", "slides");
                Directory.CreateDirectory(folder);

                var fileName = Guid.NewGuid() + Path.GetExtension(slides.FileName);
                var path = Path.Combine(folder, fileName);

                using var stream = new FileStream(path, FileMode.Create);
                await slides.CopyToAsync(stream);

                existing.SlideUrl = "/uploads/slides/" + fileName;
            }

            _context.Lessons.Update(existing);
            await _context.SaveChangesAsync();

            if (slides != null)
                TempData["Message"] = "Lección y diapositivas actualizadas correctamente.";
            else
                TempData["Message"] = "Lección actualizada correctamente.";

            return RedirectToAction("Lessons", "AdminModule", new { id = existing.ModuleId });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteSlides(int id)
        {
            var lesson = await _context.Lessons.FindAsync(id);
            if (lesson == null) return NotFound();

            if (!string.IsNullOrEmpty(lesson.SlideUrl))
            {
                try
                {
                    var relative = lesson.SlideUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                    var filePath = Path.Combine(_env.WebRootPath, relative);
                    if (System.IO.File.Exists(filePath))
                        System.IO.File.Delete(filePath);
                }
                catch
                {
                    // ignore file system errors, still clear DB entry
                }

                lesson.SlideUrl = null;
                _context.Lessons.Update(lesson);
                await _context.SaveChangesAsync();

                TempData["Message"] = "Diapositivas eliminadas correctamente.";
            }

            TempData["Message"] = "Diapositivas eliminadas correctamente.";
            return RedirectToAction("Lessons", "AdminModule", new { id = lesson.ModuleId });
        }
    }
}
