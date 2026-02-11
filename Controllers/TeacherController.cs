using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoboticCoders.Data;
using RoboticCoders.Models;
using RoboticCoders.ViewModels.Teacher;

namespace RoboticCoders.Controllers
{
    [Authorize(Roles = "Docente")]
    public class TeacherController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public TeacherController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // 👉 Dashboard del docente
    public async Task<IActionResult> Dashboard()
    {
        var teacher = await _userManager.GetUserAsync(User);

        var courses = await _context.Courses
            .Where(c => c.TeacherId == teacher!.Id)
            .Include(c => c.Modules)
                .ThenInclude(m => m.Lessons)
            .ToListAsync();

        var model = courses.Select(course => new TeacherCourseViewModel
        {
            CourseId = course.Id,
            Title = course.Title,
            TotalLessons = course.Modules
                .SelectMany(m => m.Lessons)
                .Count()
            ,
            StudentCount = _context.CourseEnrollments.Count(e => e.CourseId == course.Id)
        }).ToList();

        return View(model);
    }

    // 👉 Progreso por curso
    public IActionResult Progress(int courseId)
    {
        var data = _context.StudentLessonProgresses
            .Where(p => p.Lesson.Module.CourseId == courseId)
            .Include(p => p.User)
            .GroupBy(p => p.User.Email)
            .Select(g => new TeacherProgressViewModel
            {
                StudentEmail = g.Key!,
                CompletedLessons = g.Count(x => x.IsCompleted)
            })
            .ToList();

        return View(data);
    }

    // 👉 Ver detalles del curso (contenido + estudiantes asignados)
    public async Task<IActionResult> Course(int courseId)
    {
        var teacher = await _userManager.GetUserAsync(User);

        var course = await _context.Courses
            .Where(c => c.Id == courseId && c.TeacherId == teacher!.Id)
            .Include(c => c.Modules)
                .ThenInclude(m => m.Lessons)
            .FirstOrDefaultAsync();

        if (course == null)
            return NotFound();

        var enrollments = await _context.CourseEnrollments
            .Where(e => e.CourseId == courseId)
            .Include(e => e.User)
            .ToListAsync();

        var students = new List<TeacherStudentProgressViewModel>();

        foreach (var en in enrollments)
        {
            var completed = await _context.StudentLessonProgresses
                .Where(p => p.UserId == en.UserId && p.IsCompleted && p.Lesson.Module.CourseId == courseId)
                .CountAsync();

            students.Add(new TeacherStudentProgressViewModel
            {
                StudentEmail = en.User?.Email ?? "",
                CompletedLessons = completed
            });
        }

        var model = new TeacherCourseDetailsViewModel
        {
            Course = course,
            Students = students
        };

        return View(model);
    }

    public async Task<IActionResult> ExportStudentsCSV(int courseId)
    {
        var teacher = await _userManager.GetUserAsync(User);

        var course = await _context.Courses
            .Where(c => c.Id == courseId && c.TeacherId == teacher!.Id)
            .FirstOrDefaultAsync();

        if (course == null)
            return NotFound();

        var enrollments = await _context.CourseEnrollments
            .Where(e => e.CourseId == courseId)
            .Include(e => e.User)
            .ToListAsync();

        var totalLessons = await _context.Modules
            .Where(m => m.CourseId == courseId)
            .SelectMany(m => m.Lessons)
            .CountAsync();

        var csvBuilder = new System.Text.StringBuilder();
        csvBuilder.AppendLine("Email,Lecciones Completadas,Total Lecciones,Progreso (porcentaje)");

        foreach (var e in enrollments)
        {
            var completed = await _context.StudentLessonProgresses
                .Where(p => p.UserId == e.UserId && p.IsCompleted && p.Lesson.Module.CourseId == courseId)
                .CountAsync();

            var percent = totalLessons > 0 ? (completed * 100) / totalLessons : 0;
            csvBuilder.AppendLine($"\"{e.User?.Email}\",{completed},{totalLessons},{percent}");
        }

        var bytes = System.Text.Encoding.UTF8.GetBytes(csvBuilder.ToString());
        var sanitizedTitle = course.Title.Replace(" ", "_").Replace(",", "");
        return File(bytes, "text/csv", $"estudiantes_{sanitizedTitle}_{DateTime.Now:yyyyMMdd}.csv");
    }
    }
}
