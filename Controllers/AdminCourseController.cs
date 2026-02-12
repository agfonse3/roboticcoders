using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoboticCoders.Data;
using RoboticCoders.Models;
using RoboticCoders.ViewModels.Admin;

[Authorize(Roles = "Admin")]
public class AdminCourseController : Controller
{
    private readonly ApplicationDbContext _context;

    public AdminCourseController(ApplicationDbContext context)
    {
        _context = context;
    }

    // LISTA DE CURSOS
    public async Task<IActionResult> Index()
    {
        return View(await _context.Courses.ToListAsync());
    }

    // CREAR CURSO
    [HttpGet]
    public IActionResult Create() => View();

    [HttpPost]
    public async Task<IActionResult> Create(Course course)
    {
        if (!ModelState.IsValid) return View(course);

        _context.Courses.Add(course);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    // VER MODULOS DEL CURSO
    public async Task<IActionResult> Modules(int id)
    {
        var course = await _context.Courses
            .Include(c => c.Modules)
                .ThenInclude(m => m.Lessons)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (course == null) return NotFound();

        var lessonsOrdered = course.Modules
            .OrderBy(m => m.Id)
            .SelectMany(m => m.Lessons.OrderBy(l => l.Id))
            .ToList();

        var lessonIds = lessonsOrdered.Select(l => l.Id).ToList();

        var enrollments = await _context.CourseEnrollments
            .Where(e => e.CourseId == id)
            .Include(e => e.User)
            .ToListAsync();

        var students = enrollments
            .Where(e => e.User != null)
            .GroupBy(e => e.UserId)
            .Select(g => g.First().User!)
            .OrderBy(u => u.Email)
            .ToList();

        var studentIds = students.Select(s => s.Id).ToList();

        var completedProgress = await _context.StudentLessonProgresses
            .Where(p => p.IsCompleted)
            .Where(p => lessonIds.Contains(p.LessonId))
            .Where(p => studentIds.Contains(p.UserId))
            .Select(p => new { p.UserId, p.LessonId })
            .ToListAsync();

        var completedByStudent = completedProgress
            .GroupBy(x => x.UserId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => x.LessonId).ToHashSet());

        var studentStatus = students.Select(student =>
        {
            var completedSet = completedByStudent.TryGetValue(student.Id, out var set)
                ? set
                : new HashSet<int>();

            var missingLessons = lessonsOrdered
                .Where(l => !completedSet.Contains(l.Id))
                .Select(l => $"{l.Module.Title}: {l.Title}")
                .ToList();

            var completedCount = completedSet.Count;
            var totalLessons = lessonsOrdered.Count;
            var progressPercent = totalLessons == 0 ? 0 : (completedCount * 100) / totalLessons;

            return new AdminCourseStudentLessonStatusViewModel
            {
                StudentId = student.Id,
                StudentEmail = student.Email ?? "(sin correo)",
                CompletedLessons = completedCount,
                MissingLessons = missingLessons.Count,
                ProgressPercent = progressPercent,
                MissingLessonTitles = missingLessons
            };
        }).ToList();

        var vm = new AdminCourseModulesViewModel
        {
            Course = course,
            TotalLessons = lessonsOrdered.Count,
            Students = studentStatus
        };

        return View(vm);
    }
}
