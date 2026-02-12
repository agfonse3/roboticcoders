using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoboticCoders.Data;
using RoboticCoders.Models;
using RoboticCoders.ViewModels.Teacher;

namespace RoboticCoders.Controllers;

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

    public async Task<IActionResult> Dashboard()
    {
        var teacher = await _userManager.GetUserAsync(User);
        if (teacher == null) return Challenge();

        var assignments = await _context.CourseTeacherAssignments
            .Where(a => a.TeacherId == teacher.Id)
            .Include(a => a.Course)
                .ThenInclude(c => c.Modules)
                    .ThenInclude(m => m.Lessons)
            .Include(a => a.StudentEnrollments)
            .ToListAsync();

        var models = assignments.Select(a => new TeacherCourseViewModel
        {
            CourseId = a.CourseId,
            Title = a.Course.Title,
            TotalLessons = a.Course.Modules.SelectMany(m => m.Lessons).Count(),
            StudentCount = a.StudentEnrollments.Count
        }).ToList();

        var assignmentCourseIds = assignments.Select(a => a.CourseId).ToHashSet();
        var legacyCourses = await _context.Courses
            .Where(c => c.TeacherId == teacher.Id && !assignmentCourseIds.Contains(c.Id))
            .Include(c => c.Modules)
                .ThenInclude(m => m.Lessons)
            .ToListAsync();

        var legacyModels = legacyCourses.Select(c => new TeacherCourseViewModel
        {
            CourseId = c.Id,
            Title = c.Title,
            TotalLessons = c.Modules.SelectMany(m => m.Lessons).Count(),
            StudentCount = _context.CourseEnrollments.Count(e => e.CourseId == c.Id)
        });

        var model = models
            .Concat(legacyModels)
            .GroupBy(x => x.CourseId)
            .Select(g => g.First())
            .OrderBy(x => x.Title)
            .ToList();

        return View(model);
    }

    public async Task<IActionResult> Progress(int courseId)
    {
        var teacher = await _userManager.GetUserAsync(User);
        if (teacher == null) return Challenge();

        var assignment = await _context.CourseTeacherAssignments
            .FirstOrDefaultAsync(a => a.CourseId == courseId && a.TeacherId == teacher.Id);

        var isLegacyTeacher = await _context.Courses.AnyAsync(c => c.Id == courseId && c.TeacherId == teacher.Id);
        if (assignment == null && !isLegacyTeacher) return NotFound();

        var enrollmentsQuery = _context.CourseEnrollments.Where(e => e.CourseId == courseId);
        if (assignment != null)
            enrollmentsQuery = enrollmentsQuery.Where(e => e.CourseTeacherAssignmentId == assignment.Id);

        var studentIds = await enrollmentsQuery
            .Select(e => e.UserId)
            .ToListAsync();

        var data = await _context.StudentLessonProgresses
            .Where(p => studentIds.Contains(p.UserId) && p.Lesson.Module.CourseId == courseId)
            .Include(p => p.User)
            .GroupBy(p => p.User.Email)
            .Select(g => new TeacherProgressViewModel
            {
                StudentEmail = g.Key ?? string.Empty,
                CompletedLessons = g.Count(x => x.IsCompleted)
            })
            .ToListAsync();

        return View(data);
    }

    public async Task<IActionResult> Course(int courseId)
    {
        var teacher = await _userManager.GetUserAsync(User);
        if (teacher == null) return Challenge();

        var assignment = await _context.CourseTeacherAssignments
            .FirstOrDefaultAsync(a => a.CourseId == courseId && a.TeacherId == teacher.Id);

        var isLegacyTeacher = await _context.Courses.AnyAsync(c => c.Id == courseId && c.TeacherId == teacher.Id);
        if (assignment == null && !isLegacyTeacher) return NotFound();

        var course = await _context.Courses
            .Where(c => c.Id == courseId)
            .Include(c => c.Modules)
                .ThenInclude(m => m.Lessons)
            .FirstOrDefaultAsync();

        if (course == null)
            return NotFound();

        var enrollmentsQuery = _context.CourseEnrollments.Where(e => e.CourseId == courseId);
        if (assignment != null)
            enrollmentsQuery = enrollmentsQuery.Where(e => e.CourseTeacherAssignmentId == assignment.Id);

        var enrollments = await enrollmentsQuery
            .Include(e => e.User)
            .ToListAsync();

        var lessonsOrdered = course.Modules
            .OrderBy(m => m.Id)
            .SelectMany(m => m.Lessons.OrderBy(l => l.Id))
            .ToList();

        var lessonIds = lessonsOrdered.Select(l => l.Id).ToList();
        var studentIds = enrollments.Select(e => e.UserId).Distinct().ToList();

        var completedProgress = await _context.StudentLessonProgresses
            .Where(p => p.IsCompleted)
            .Where(p => lessonIds.Contains(p.LessonId))
            .Where(p => studentIds.Contains(p.UserId))
            .Select(p => new { p.UserId, p.LessonId })
            .ToListAsync();

        var completedByStudent = completedProgress
            .GroupBy(x => x.UserId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.LessonId).ToHashSet());

        var totalLessons = lessonsOrdered.Count;

        var students = enrollments
            .GroupBy(e => e.UserId)
            .Select(g => g.First())
            .Select(en =>
            {
                var completedSet = completedByStudent.TryGetValue(en.UserId, out var set)
                    ? set
                    : new HashSet<int>();

                var missingLessons = lessonsOrdered
                    .Where(l => !completedSet.Contains(l.Id))
                    .Select(l => $"{l.Module.Title}: {l.Title}")
                    .ToList();

                var completedCount = completedSet.Count;
                var percent = totalLessons == 0 ? 0 : (completedCount * 100) / totalLessons;

                return new TeacherStudentProgressViewModel
                {
                    StudentId = en.UserId,
                    StudentEmail = en.User?.Email ?? string.Empty,
                    CompletedLessons = completedCount,
                    MissingLessons = missingLessons.Count,
                    ProgressPercent = percent,
                    MissingLessonTitles = missingLessons
                };
            })
            .OrderBy(s => s.StudentEmail)
            .ToList();

        var reviewedList = await _context.StudentLessonProgresses
            .Where(p => p.UserId == teacher.Id && p.IsCompleted && p.Lesson.Module.CourseId == courseId)
            .Select(p => p.LessonId)
            .ToListAsync();

        var reviewedIds = reviewedList.ToHashSet();

        var model = new TeacherCourseDetailsViewModel
        {
            Course = course,
            Students = students,
            ReviewedLessonIds = reviewedIds
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CompleteLesson(int courseId, int lessonId)
    {
        var teacher = await _userManager.GetUserAsync(User);
        if (teacher == null) return Challenge();

        var isAssigned = await _context.CourseTeacherAssignments
            .AnyAsync(a => a.CourseId == courseId && a.TeacherId == teacher.Id);
        var isLegacyTeacher = await _context.Courses.AnyAsync(c => c.Id == courseId && c.TeacherId == teacher.Id);
        if (!isAssigned && !isLegacyTeacher) return NotFound();

        var belongsToCourse = await _context.Lessons
            .AnyAsync(l => l.Id == lessonId && l.Module.CourseId == courseId);
        if (!belongsToCourse) return NotFound();

        var progress = await _context.StudentLessonProgresses
            .FirstOrDefaultAsync(p => p.UserId == teacher.Id && p.LessonId == lessonId);

        if (progress == null)
        {
            progress = new StudentLessonProgress
            {
                UserId = teacher.Id,
                LessonId = lessonId,
                IsCompleted = true,
                CompletedAt = DateTime.UtcNow
            };
            _context.StudentLessonProgresses.Add(progress);
        }
        else if (!progress.IsCompleted)
        {
            progress.IsCompleted = true;
            progress.CompletedAt = DateTime.UtcNow;
            _context.StudentLessonProgresses.Update(progress);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Course), new { courseId });
    }

    public async Task<IActionResult> ExportStudentsCSV(int courseId)
    {
        var teacher = await _userManager.GetUserAsync(User);
        if (teacher == null) return Challenge();

        var assignment = await _context.CourseTeacherAssignments
            .FirstOrDefaultAsync(a => a.CourseId == courseId && a.TeacherId == teacher.Id);

        var isLegacyTeacher = await _context.Courses.AnyAsync(c => c.Id == courseId && c.TeacherId == teacher.Id);
        if (assignment == null && !isLegacyTeacher) return NotFound();

        var course = await _context.Courses
            .FirstOrDefaultAsync(c => c.Id == courseId);
        if (course == null) return NotFound();

        var enrollmentsQuery = _context.CourseEnrollments.Where(e => e.CourseId == courseId);
        if (assignment != null)
            enrollmentsQuery = enrollmentsQuery.Where(e => e.CourseTeacherAssignmentId == assignment.Id);

        var enrollments = await enrollmentsQuery
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
