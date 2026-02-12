using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoboticCoders.Data;
using RoboticCoders.Models;

namespace RoboticCoders.Controllers;

[Authorize(Roles = "Admin")]
public class AdminLessonController : Controller
{
    private static readonly HashSet<string> AllowedSlideExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf",
        ".ppt",
        ".pptx"
    };

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
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Lesson lesson, IFormFile? slides)
    {
        if (slides != null)
        {
            var slideResult = await SaveSlidesFile(slides);
            if (!slideResult.Success)
            {
                ModelState.AddModelError(string.Empty, slideResult.ErrorMessage!);
                ViewBag.ModuleId = lesson.ModuleId;
                return View(lesson);
            }

            lesson.SlideUrl = slideResult.RelativeUrl;
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
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Lesson lesson, IFormFile? slides)
    {
        var existing = await _context.Lessons.FindAsync(lesson.Id);
        if (existing == null) return NotFound();

        existing.Title = lesson.Title;
        existing.Content = lesson.Content;
        existing.VideoUrl = lesson.VideoUrl;

        if (slides != null)
        {
            var slideResult = await SaveSlidesFile(slides);
            if (!slideResult.Success)
            {
                ModelState.AddModelError(string.Empty, slideResult.ErrorMessage!);
                return View(existing);
            }

            existing.SlideUrl = slideResult.RelativeUrl;
        }

        _context.Lessons.Update(existing);
        await _context.SaveChangesAsync();

        TempData["Message"] = "Leccion actualizada correctamente.";
        return RedirectToAction("Lessons", "AdminModule", new { id = existing.ModuleId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
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
                // keep DB consistent even if file deletion fails
            }

            lesson.SlideUrl = null;
            _context.Lessons.Update(lesson);
            await _context.SaveChangesAsync();
        }

        TempData["Message"] = "Diapositivas eliminadas correctamente.";
        return RedirectToAction("Lessons", "AdminModule", new { id = lesson.ModuleId });
    }

    private async Task<(bool Success, string? RelativeUrl, string? ErrorMessage)> SaveSlidesFile(IFormFile slides)
    {
        var ext = Path.GetExtension(slides.FileName);
        if (string.IsNullOrWhiteSpace(ext) || !AllowedSlideExtensions.Contains(ext))
        {
            return (false, null, "Formato no permitido. Usa PDF, PPT o PPTX.");
        }

        var folder = Path.Combine(_env.WebRootPath, "uploads", "slides");
        Directory.CreateDirectory(folder);

        var fileName = $"{Guid.NewGuid()}{ext.ToLowerInvariant()}";
        var path = Path.Combine(folder, fileName);

        using var stream = new FileStream(path, FileMode.Create);
        await slides.CopyToAsync(stream);

        return (true, "/uploads/slides/" + fileName, null);
    }
}
