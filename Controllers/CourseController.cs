using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using RoboticCoders.Data;
using RoboticCoders.Models;
using RoboticCoders.ViewModels.Student;

namespace RoboticCoders.Controllers
{
    public class CourseController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CourseController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // 📚 Listado público de cursos
        public async Task<IActionResult> Index()
        {
            var courses = await _context.Courses.ToListAsync();
            return View(courses);
        }

        // 📖 Detalle del curso
        public async Task<IActionResult> Details(int id)
        {
            var course = await _context.Courses
                .Include(c => c.Modules)
                    .ThenInclude(m => m.Lessons)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (course == null)
                return NotFound("Curso no encontrado");

            return View(course);
        }

        public async Task<IActionResult> Lesson(int id)
        {
            var lesson = await _context.Lessons
                .Include(l => l.Module)
                .ThenInclude(m => m.Course)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (lesson == null) return NotFound();

            var courseId = lesson.Module?.CourseId ?? 0;

            var lessonsOrdered = await _context.Modules
                .Where(m => m.CourseId == courseId)
                .OrderBy(m => m.Id)
                .SelectMany(m => m.Lessons.OrderBy(l => l.Id))
                .ToListAsync();

            var idx = lessonsOrdered.FindIndex(l => l.Id == id);
            int? nextId = null;
            if (idx >= 0 && idx < lessonsOrdered.Count - 1)
                nextId = lessonsOrdered[idx + 1].Id;

            var course = await _context.Courses
                .Where(c => c.Id == courseId)
                .Include(c => c.Modules)
                    .ThenInclude(m => m.Lessons)
                .FirstOrDefaultAsync();

            var user = await _userManager.GetUserAsync(User);
            var completedIds = new HashSet<int>();
            if (user != null)
            {
                var completedList = await _context.StudentLessonProgresses
                    .Where(p => p.UserId == user.Id && p.IsCompleted && p.Lesson.Module.CourseId == courseId)
                    .Select(p => p.LessonId)
                    .ToListAsync();

                completedIds = completedList.ToHashSet();
            }

            var totalLessons = lessonsOrdered.Count;
            var progressPercent = totalLessons == 0 ? 0 : (completedIds.Count * 100) / totalLessons;

            var vm = new StudentLessonViewModel
            {
                Lesson = lesson,
                NextLessonId = nextId,
                IsCompleted = false,
                CourseId = courseId,
                Course = course,
                Modules = course?.Modules.OrderBy(m => m.Id).ToList() ?? new List<Module>(),
                CompletedLessonIds = completedIds,
                ProgressPercent = progressPercent
            };

            return View("../Student/Lesson", vm);
        }
    }
}
