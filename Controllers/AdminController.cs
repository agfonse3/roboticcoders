using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoboticCoders.Data;
using RoboticCoders.Models;

namespace RoboticCoders.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;

    public AdminController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context)
    {
        _userManager = userManager;
        _context = context;
    }

   
    // ================= DASHBOARD =================
    public async Task<IActionResult> Dashboard()
    {
        var courses = await _context.Courses
            .Include(c => c.Modules)
                .ThenInclude(m => m.Lessons)
            .ToListAsync();

        ViewBag.Courses = courses;
        
        var users = _userManager.Users.ToList();
        var model = new List<AdminUserViewModel>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);

            model.Add(new AdminUserViewModel
            {
                UserId = user.Id,
                Email = user.Email!,
                Role = roles.FirstOrDefault() ?? "Sin rol"
            });
        }

        return View(model);
    }

    // ================= CREAR CURSO =================
    [HttpGet]
    public async Task<IActionResult> CreateCourse()
    {
        var teachers = await _userManager.GetUsersInRoleAsync("Docente");

        ViewBag.Teachers = teachers;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> CreateCourse(CreateCourseViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Teachers = await _userManager.GetUsersInRoleAsync("Docente");
            return View(model);
        }

        var course = new Course
        {
            Title = model.Title,
            Description = model.Description,
            ImageUrl = model.ImageUrl
            // TeacherId se asignará luego en ManageCourse
        };

        _context.Courses.Add(course);
        await _context.SaveChangesAsync();

        return RedirectToAction("ManageCourse", new { id = course.Id });
    }



    // 👉 Crear usuario
    // ================= REGISTER (SOLO ADMIN) =================
    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email
        };

        var result = await _userManager.CreateAsync(user, model.Password);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return View(model);
        }

        return RedirectToAction("Dashboard");
    }

    // 👉 Asignar estudiantes a un curso
    [HttpGet]
    public async Task<IActionResult> AssignStudents(int courseId)
    {
        var course = await _context.Courses.FindAsync(courseId);
        if (course == null) return NotFound();

        var students = await _userManager.GetUsersInRoleAsync("Estudiante");

        var enrolled = await _context.CourseEnrollments
            .Where(e => e.CourseId == courseId)
            .Select(e => e.UserId)
            .ToListAsync();

        ViewBag.Course = course;
        ViewBag.Students = students;
        ViewBag.Enrolled = enrolled;

        return View();
    }

    [HttpPost]
    public async Task<IActionResult> AssignStudents(int courseId, string[]? selectedStudentIds)
    {
        var course = await _context.Courses.FindAsync(courseId);
        if (course == null) return NotFound();

        selectedStudentIds ??= Array.Empty<string>();

        // existing enrollments
        var existing = await _context.CourseEnrollments
            .Where(e => e.CourseId == courseId)
            .ToListAsync();

        // remove enrollments not selected
        var toRemove = existing.Where(e => !selectedStudentIds.Contains(e.UserId)).ToList();
        if (toRemove.Any())
            _context.CourseEnrollments.RemoveRange(toRemove);

        // add new enrollments
        var existingIds = existing.Select(e => e.UserId).ToHashSet();
        var toAdd = selectedStudentIds.Where(id => !existingIds.Contains(id))
            .Select(id => new CourseEnrollment { CourseId = courseId, UserId = id })
            .ToList();

        if (toAdd.Any())
            _context.CourseEnrollments.AddRange(toAdd);

        await _context.SaveChangesAsync();

        TempData["Message"] = "Asignaciones actualizadas";
        return RedirectToAction("Dashboard");
    }

    // 👉 Gestionar curso (asignar docente y estudiantes)
    [HttpGet]
    public async Task<IActionResult> ManageCourse(int id)
    {
        var course = await _context.Courses.FindAsync(id);
        if (course == null) return NotFound();

        var teachers = await _userManager.GetUsersInRoleAsync("Docente");
        var students = await _userManager.GetUsersInRoleAsync("Estudiante");

        ViewBag.Course = course;
        ViewBag.Teachers = teachers;
        ViewBag.Students = students;
        ViewBag.AssignedTeacherId = course.TeacherId;
        ViewBag.EnrolledStudentIds = await _context.CourseEnrollments
            .Where(e => e.CourseId == id)
            .Select(e => e.UserId)
            .ToListAsync();

        return View();
    }

    [HttpPost]
    public async Task<IActionResult> ManageCourse(int id, string? selectedTeacherId, string[]? selectedStudentIds)
    {
        var course = await _context.Courses.FindAsync(id);
        if (course == null) return NotFound();

        // Diagnostic logging to help debug enrollment binding
        Console.WriteLine($"[ManageCourse] courseId={id} selectedTeacherId={selectedTeacherId}");
        if (selectedStudentIds == null)
        {
            Console.WriteLine("[ManageCourse] selectedStudentIds = null");
        }
        else
        {
            Console.WriteLine($"[ManageCourse] selectedStudentIds ({selectedStudentIds.Length}) = {string.Join(",", selectedStudentIds)}");
        }

        // Asignar docente
        course.TeacherId = selectedTeacherId;

        // Actualizar inscritos
        selectedStudentIds ??= Array.Empty<string>();
        var existing = await _context.CourseEnrollments
            .Where(e => e.CourseId == id)
            .ToListAsync();

        Console.WriteLine($"[ManageCourse] existing enrollments count = {existing.Count}");

        var toRemove = existing.Where(e => !selectedStudentIds.Contains(e.UserId)).ToList();
        if (toRemove.Any())
            _context.CourseEnrollments.RemoveRange(toRemove);

        var existingIds = existing.Select(e => e.UserId).ToHashSet();
        var toAdd = selectedStudentIds.Where(sid => !existingIds.Contains(sid))
            .Select(sid => new CourseEnrollment { CourseId = id, UserId = sid })
            .ToList();

        if (toAdd.Any())
            _context.CourseEnrollments.AddRange(toAdd);

        _context.Courses.Update(course);
        await _context.SaveChangesAsync();

        TempData["Message"] = "Curso configurado exitosamente";
        return RedirectToAction("Dashboard");
    }
    [HttpGet]
    public async Task<IActionResult> ExportStudentsCSV(int courseId)
    {
        var course = await _context.Courses.FindAsync(courseId);
        if (course == null) return NotFound();

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

    // Diagnostic: list all enrollments (admin only)
    [HttpGet]
    public async Task<IActionResult> Enrollments()
    {
        var enrollments = await _context.CourseEnrollments
            .Include(e => e.Course)
            .Include(e => e.User)
            .ToListAsync();

        return View(enrollments);
    }
    }
}

