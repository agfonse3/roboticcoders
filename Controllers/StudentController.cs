using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoboticCoders.Data;
using RoboticCoders.Models;

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

        // build ordered list of lessons for the course
        var lessonsOrdered = await _context.Modules
            .Where(m => m.CourseId == courseId)
            .OrderBy(m => m.Id)
            .SelectMany(m => m.Lessons.OrderBy(l => l.Id))
            .ToListAsync();

        var idx = lessonsOrdered.FindIndex(l => l.Id == id);
        int? nextId = null;
        if (idx >= 0 && idx < lessonsOrdered.Count - 1)
            nextId = lessonsOrdered[idx + 1].Id;

        var progress = await _context.StudentLessonProgresses
            .FirstOrDefaultAsync(p => p.UserId == user.Id && p.LessonId == id);

        var vm = new RoboticCoders.Models.StudentLessonViewModel
        {
            Lesson = lesson,
            NextLessonId = nextId,
            IsCompleted = progress?.IsCompleted ?? false,
            CourseId = courseId
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
            progress = new RoboticCoders.Models.StudentLessonProgress
            {
                UserId = user.Id,
                LessonId = lessonId,
                IsCompleted = true
            };
            _context.StudentLessonProgresses.Add(progress);
        }
        else
        {
            progress.IsCompleted = true;
            _context.StudentLessonProgresses.Update(progress);
        }

        await _context.SaveChangesAsync();

        if (nextLessonId.HasValue)
            return RedirectToAction("Lesson", new { id = nextLessonId.Value });

        // otherwise redirect back to the course
        var lesson = await _context.Lessons.Include(l => l.Module).FirstOrDefaultAsync(l => l.Id == lessonId);
        var courseId = lesson?.Module?.CourseId;
        if (courseId.HasValue && courseId.Value > 0)
            return RedirectToAction("Course", new { id = courseId.Value });

        return RedirectToAction("Dashboard");
    }
}
