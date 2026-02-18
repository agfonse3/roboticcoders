using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoboticCoders.Data;
using RoboticCoders.Models;
using Microsoft.EntityFrameworkCore;
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
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var lesson = await _context.Lessons
            .Include(l => l.HtmlResources)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (lesson == null) return NotFound();

        return View(lesson);
    }


    [HttpPost]
    [ValidateAntiForgeryToken]

    [RequestSizeLimit(200_000_000)] // 200 MB
    [RequestFormLimits(MultipartBodyLengthLimit = 200_000_000)]
    public async Task<IActionResult> Edit(Lesson lesson, IFormFile? slides, List<IFormFile>? htmlFiles)
    {
        var existing = await _context.Lessons.FindAsync(lesson.Id);
        if (existing == null) return NotFound();

        existing.Title = lesson.Title;
        existing.Content = lesson.Content;
        existing.VideoUrl = lesson.VideoUrl;
        existing.SlidesType = lesson.SlidesType;
existing.SlidesEmbedUrl = lesson.SlidesEmbedUrl;


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
        if (htmlFiles != null && htmlFiles.Count > 0)
        {
            foreach (var file in htmlFiles)
            {
                if (file == null || file.Length == 0) continue;

                var save = await SaveHtmlFile(file);
                if (!save.Success)
                {
                    ModelState.AddModelError(string.Empty, save.ErrorMessage!);
                    return View(existing);
                }

                var titleDefault = Path.GetFileNameWithoutExtension(file.FileName);

                var res = new LessonHtmlResource
                {
                    LessonId = existing.Id,
                    Title = titleDefault,
                    OriginalFileName = file.FileName,
                    Url = save.RelativeUrl!
                };

                _context.LessonHtmlResources.Add(res);
            }
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

    private async Task<(bool Success, string? RelativeUrl, string? ErrorMessage)> SaveHtmlFile(IFormFile html)
    {
        var ext = Path.GetExtension(html.FileName);
        if (!string.Equals(ext, ".html", StringComparison.OrdinalIgnoreCase))
            return (false, null, "Formato no permitido. Solo .html.");

        var folder = Path.Combine(_env.WebRootPath, "uploads", "lesson-html");
        Directory.CreateDirectory(folder);

        var fileName = $"{Guid.NewGuid():N}.html";
        var path = Path.Combine(folder, fileName);

        using var stream = new FileStream(path, FileMode.Create);
        await html.CopyToAsync(stream);

        return (true, "/uploads/lesson-html/" + fileName, null);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteHtmlResource(int id, int lessonId)
    {
        var res = await _context.LessonHtmlResources.FindAsync(id);
        if (res != null)
        {
            // delete file
            try
            {
                var relative = res.Url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                var filePath = Path.Combine(_env.WebRootPath, relative);
                if (System.IO.File.Exists(filePath))
                    System.IO.File.Delete(filePath);
            }
            catch { }

            _context.LessonHtmlResources.Remove(res);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction("Edit", new { id = lessonId });
    }

}
