using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoboticCoders.Data;
using RoboticCoders.Models;
using RoboticCoders.ViewModels.Student;

[Authorize(Roles = "Estudiante")]
public class StudentController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public StudentController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Dashboard()
    {
        var user = await _userManager.GetUserAsync(User);

        var roles = await _userManager.GetRolesAsync(user!);
        Console.WriteLine("ROLES DEL USUARIO: " + string.Join(",", roles));
       
        var courses = await _context.CourseEnrollments
            .Where(e => e.UserId == user!.Id)
            .Include(e => e.Course!)
                .ThenInclude(c => c.Modules)
                    .ThenInclude(m => m.Lessons)
            .Select(e => e.Course!)
            .ToListAsync();

        var progress = await _context.StudentLessonProgresses
            .Where(p => p.UserId == user!.Id)
            .ToListAsync();

        var viewModel = courses.Select(course =>
        {
            var totalLessons = course.Modules.SelectMany(m => m.Lessons).Count();
            var completedLessons = progress.Count(p =>
                course.Modules.SelectMany(m => m.Lessons)
                .Any(l => l.Id == p.LessonId && p.IsCompleted));

            var percent = totalLessons == 0
                ? 0
                : (completedLessons * 100) / totalLessons;

            return new StudentCourseViewModel
            {
                CourseId = course.Id,
                Title = course.Title,
                Description = course.Description,
                ProgressPercent = percent
            };
        }).ToList();

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Lesson(int id)
    {
        var lesson = await _context.Lessons
            .Include(l => l.Module)
            .ThenInclude(m => m.Course)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (lesson == null) return NotFound();

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var courseId = lesson.Module?.CourseId ?? 0;

        var enrolled = await _context.CourseEnrollments
            .AnyAsync(e => e.CourseId == courseId && e.UserId == user.Id);

        if (!enrolled) return NotFound();

        var course = await _context.Courses
            .Where(c => c.Id == courseId)
            .Include(c => c.Modules)
                .ThenInclude(m => m.Lessons)
            .FirstOrDefaultAsync();

        var lessonsOrdered = course?.Modules
            .OrderBy(m => m.Id)
            .SelectMany(m => m.Lessons.OrderBy(l => l.Id))
            .ToList() ?? new List<Lesson>();

        var idx = lessonsOrdered.FindIndex(l => l.Id == id);
        int? nextId = null;
        if (idx >= 0 && idx < lessonsOrdered.Count - 1)
            nextId = lessonsOrdered[idx + 1].Id;

        var progressForUser = await _context.StudentLessonProgresses
            .Where(p => p.UserId == user.Id && course != null)
            .ToListAsync();

        var completedIds = progressForUser
            .Where(p => p.IsCompleted)
            .Select(p => p.LessonId)
            .ToHashSet();

        var totalLessons = lessonsOrdered.Count;
        var completedCount = completedIds.Count;
        var progressPercent = totalLessons == 0 ? 0 : (completedCount * 100) / totalLessons;

        var vm = new StudentLessonViewModel
        {
            Lesson = lesson,
            NextLessonId = nextId,
            IsCompleted = progressForUser.FirstOrDefault(p => p.LessonId == id)?.IsCompleted ?? false,
            CourseId = courseId,
            Course = course,
            Modules = course?.Modules.OrderBy(m => m.Id).ToList() ?? new List<Module>(),
            CompletedLessonIds = completedIds,
            ProgressPercent = progressPercent
        };

        return View("Lesson", vm);
    }

    [HttpGet]
    public async Task<IActionResult> Course(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var enrolled = await _context.CourseEnrollments
            .AnyAsync(e => e.CourseId == id && e.UserId == user.Id);

        if (!enrolled)
            return NotFound();

        var course = await _context.Courses
            .Where(c => c.Id == id)
            .Include(c => c.Modules)
                .ThenInclude(m => m.Lessons)
            .FirstOrDefaultAsync();

        if (course == null) return NotFound();

        return View(course);
    }

    [HttpPost]
    public async Task<IActionResult> CompleteLesson(int lessonId, int? nextLessonId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var progress = await _context.StudentLessonProgresses
            .FirstOrDefaultAsync(p => p.UserId == user.Id && p.LessonId == lessonId);

        if (progress == null)
        {
            progress = new StudentLessonProgress
            {
                UserId = user.Id,
                LessonId = lessonId,
                IsCompleted = true,
                CompletedAt = DateTime.UtcNow
            };
            _context.StudentLessonProgresses.Add(progress);
        }
        else
        {
            progress.IsCompleted = true;
            progress.CompletedAt = DateTime.UtcNow;
            _context.StudentLessonProgresses.Update(progress);
        }

        await _context.SaveChangesAsync();

        if (nextLessonId.HasValue)
            return RedirectToAction("Lesson", new { id = nextLessonId.Value });

        var lesson = await _context.Lessons.Include(l => l.Module).FirstOrDefaultAsync(l => l.Id == lessonId);
        var courseId = lesson?.Module?.CourseId;
        if (courseId.HasValue && courseId.Value > 0)
            return RedirectToAction("Course", new { id = courseId.Value });

        return RedirectToAction("Dashboard");
    }
}
