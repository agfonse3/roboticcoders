using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoboticCoders.Data;
using RoboticCoders.Models;
using RoboticCoders.ViewModels.Student;

namespace RoboticCoders.Controllers
{
    public class CourseController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CourseController(ApplicationDbContext context)
        {
            _context = context;
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

            var vm = new StudentLessonViewModel
            {
                Lesson = lesson,
                NextLessonId = nextId,
                IsCompleted = false,
                CourseId = courseId
            };

            return View("../Student/Lesson", vm);
        }
    }
}
