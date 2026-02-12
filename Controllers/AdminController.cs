using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoboticCoders.Data;
using RoboticCoders.Models;
using RoboticCoders.ViewModels.Admin;

namespace RoboticCoders.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;
    private readonly RoleManager<IdentityRole> _roleManager;

    public AdminController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ApplicationDbContext context)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _context = context;
    }

    public async Task<IActionResult> EditUser(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        var roles = await _userManager.GetRolesAsync(user);

        return View(new EditUserViewModel
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Address = user.Address,
            City = user.City,
            Role = roles.FirstOrDefault() ?? "Estudiante"
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditUser(EditUserViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.FindByIdAsync(model.Id);
        if (user == null)
            return NotFound();

        if (user.Email == "admin@roboticcoders.com")
        {
            ModelState.AddModelError(string.Empty, "No puedes modificar este administrador.");
            return View(model);
        }

        user.Email = model.Email;
        user.UserName = model.Email;
        user.FirstName = model.FirstName;
        user.LastName = model.LastName;
        user.Address = model.Address;
        user.City = model.City;
        await _userManager.UpdateAsync(user);

        if (!await _roleManager.RoleExistsAsync(model.Role))
        {
            ModelState.AddModelError(string.Empty, "El rol seleccionado no existe.");
            return View(model);
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        if (!currentRoles.Contains(model.Role))
        {
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRoleAsync(user, model.Role);
        }

        return RedirectToAction(nameof(Dashboard));
    }

    public async Task<IActionResult> DeleteUser(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        return View(user);
    }

    [HttpPost, ActionName("DeleteUser")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteUserConfirmed(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user != null)
            await _userManager.DeleteAsync(user);

        return RedirectToAction(nameof(Dashboard));
    }

    public async Task<IActionResult> Dashboard()
    {
        var courses = await _context.Courses
            .Include(c => c.Modules)
                .ThenInclude(m => m.Lessons)
            .Include(c => c.TeacherAssignments)
                .ThenInclude(a => a.Teacher)
            .ToListAsync();

        ViewBag.Courses = courses;

        var lessonCounts = courses.ToDictionary(
            c => c.Id,
            c => c.Modules.SelectMany(m => m.Lessons).Count());

        var teacherAssignments = courses
            .SelectMany(c => c.TeacherAssignments.Select(a => new
            {
                CourseId = c.Id,
                TeacherId = a.TeacherId,
                TeacherEmail = a.Teacher.Email ?? "(sin correo)"
            }))
            .ToList();

        var teacherCompletions = await _context.StudentLessonProgresses
            .Where(p => p.IsCompleted)
            .Select(p => new
            {
                p.UserId,
                CourseId = p.Lesson.Module.CourseId
            })
            .GroupBy(x => new { x.CourseId, x.UserId })
            .Select(g => new
            {
                g.Key.CourseId,
                g.Key.UserId,
                Completed = g.Count()
            })
            .ToListAsync();

        var completionMap = teacherCompletions.ToDictionary(
            x => (x.CourseId, x.UserId),
            x => x.Completed);

        var progressByCourse = new Dictionary<int, List<AdminTeacherCourseProgressViewModel>>();
        foreach (var ta in teacherAssignments)
        {
            var total = lessonCounts.TryGetValue(ta.CourseId, out var count) ? count : 0;
            var completed = completionMap.TryGetValue((ta.CourseId, ta.TeacherId), out var done) ? done : 0;
            var percent = total == 0 ? 0 : (completed * 100) / total;

            if (!progressByCourse.TryGetValue(ta.CourseId, out var values))
            {
                values = new List<AdminTeacherCourseProgressViewModel>();
                progressByCourse[ta.CourseId] = values;
            }

            values.Add(new AdminTeacherCourseProgressViewModel
            {
                TeacherId = ta.TeacherId,
                TeacherEmail = ta.TeacherEmail,
                CompletedLessons = completed,
                TotalLessons = total,
                ProgressPercent = percent
            });
        }

        ViewBag.TeacherProgressByCourse = progressByCourse;

        var users = await _userManager.Users.ToListAsync();
        var model = new List<AdminUserViewModel>();

        var teacherCourses = await _context.CourseTeacherAssignments
            .Include(x => x.Course)
            .GroupBy(x => x.TeacherId)
            .ToDictionaryAsync(
                g => g.Key,
                g => g.Select(x => x.Course.Title).Distinct().OrderBy(t => t).ToList());

        var legacyTeacherCourses = await _context.Courses
            .Where(c => c.TeacherId != null)
            .Select(c => new { TeacherId = c.TeacherId!, c.Title })
            .ToListAsync();

        foreach (var legacy in legacyTeacherCourses)
        {
            if (!teacherCourses.TryGetValue(legacy.TeacherId, out var titles))
            {
                titles = new List<string>();
                teacherCourses[legacy.TeacherId] = titles;
            }

            if (!titles.Contains(legacy.Title))
                titles.Add(legacy.Title);
        }

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            model.Add(new AdminUserViewModel
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FullName = $"{user.FirstName} {user.LastName}".Trim(),
                City = user.City,
                Role = roles.FirstOrDefault() ?? "Sin rol"
            });
        }

        ViewBag.TeacherCoursesByUserId = teacherCourses;
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> CreateCourse()
    {
        ViewBag.Teachers = await _userManager.GetUsersInRoleAsync("Docente");
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
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
        };

        _context.Courses.Add(course);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(ManageCourse), new { id = course.Id });
    }

    [HttpGet]
    public IActionResult Register() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FirstName = model.FirstName,
            LastName = model.LastName,
            Address = model.Address,
            City = model.City
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return View(model);
        }

        if (await _roleManager.RoleExistsAsync(model.Role))
            await _userManager.AddToRoleAsync(user, model.Role);
        else
            await _userManager.AddToRoleAsync(user, "Estudiante");

        return RedirectToAction(nameof(Dashboard));
    }

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
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignStudents(int courseId, string[]? selectedStudentIds)
    {
        var course = await _context.Courses.FindAsync(courseId);
        if (course == null) return NotFound();

        selectedStudentIds ??= Array.Empty<string>();

        var existing = await _context.CourseEnrollments
            .Where(e => e.CourseId == courseId)
            .ToListAsync();

        _context.CourseEnrollments.RemoveRange(existing);

        var toAdd = selectedStudentIds.Distinct()
            .Select(id => new CourseEnrollment { CourseId = courseId, UserId = id })
            .ToList();

        if (toAdd.Count > 0)
            _context.CourseEnrollments.AddRange(toAdd);

        await _context.SaveChangesAsync();

        TempData["Message"] = "Asignaciones actualizadas.";
        return RedirectToAction(nameof(Dashboard));
    }

    [HttpGet]
    public async Task<IActionResult> ManageCourse(int id)
    {
        var model = await BuildManageCourseViewModel(id);
        if (model == null) return NotFound();

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ManageCourse(
        int id,
        List<string>? selectedTeacherIds,
        Dictionary<string, string>? studentTeacherAssignments)
    {
        var course = await _context.Courses.FindAsync(id);
        if (course == null) return NotFound();

        selectedTeacherIds ??= new List<string>();
        selectedTeacherIds = selectedTeacherIds
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .ToList();

        var teachers = await _userManager.GetUsersInRoleAsync("Docente");
        var teacherIdSet = teachers.Select(t => t.Id).ToHashSet();

        if (selectedTeacherIds.Any(tid => !teacherIdSet.Contains(tid)))
        {
            ModelState.AddModelError(string.Empty, "Hay docentes seleccionados que no son válidos.");
            var invalidModel = await BuildManageCourseViewModel(id, selectedTeacherIds, studentTeacherAssignments);
            return View(invalidModel);
        }

        studentTeacherAssignments ??= new Dictionary<string, string>();

        var validStudents = await _userManager.GetUsersInRoleAsync("Estudiante");
        var validStudentIds = validStudents.Select(s => s.Id).ToHashSet();

        var normalizedAssignments = studentTeacherAssignments
            .Where(x => validStudentIds.Contains(x.Key))
            .Where(x => !string.IsNullOrWhiteSpace(x.Value))
            .Select(x => new { StudentId = x.Key, TeacherId = x.Value })
            .Where(x => teacherIdSet.Contains(x.TeacherId))
            .GroupBy(x => x.StudentId)
            .Select(g => g.First())
            .ToList();

        // Keep teacher list in sync with actual assignments so assignments are not lost
        // when admin sets a teacher in section 2 but forgets to check section 1.
        selectedTeacherIds = selectedTeacherIds
            .Union(normalizedAssignments.Select(x => x.TeacherId))
            .Distinct()
            .ToList();

        var existingTeacherAssignments = await _context.CourseTeacherAssignments
            .Where(x => x.CourseId == id)
            .ToListAsync();

        var removedTeacherAssignmentIds = existingTeacherAssignments
            .Where(x => !selectedTeacherIds.Contains(x.TeacherId))
            .Select(x => x.Id)
            .ToList();

        if (removedTeacherAssignmentIds.Count > 0)
        {
            var enrollmentsForRemovedTeachers = await _context.CourseEnrollments
                .Where(e => e.CourseId == id && e.CourseTeacherAssignmentId.HasValue)
                .Where(e => removedTeacherAssignmentIds.Contains(e.CourseTeacherAssignmentId ?? 0))
                .ToListAsync();

            _context.CourseEnrollments.RemoveRange(enrollmentsForRemovedTeachers);
            _context.CourseTeacherAssignments.RemoveRange(existingTeacherAssignments.Where(x => removedTeacherAssignmentIds.Contains(x.Id)));
        }

        var existingTeacherIds = existingTeacherAssignments.Select(x => x.TeacherId).ToHashSet();
        var newTeacherAssignments = selectedTeacherIds
            .Where(tid => !existingTeacherIds.Contains(tid))
            .Select(tid => new CourseTeacherAssignment
            {
                CourseId = id,
                TeacherId = tid
            })
            .ToList();

        if (newTeacherAssignments.Count > 0)
            _context.CourseTeacherAssignments.AddRange(newTeacherAssignments);

        await _context.SaveChangesAsync();

        var teacherAssignmentsByTeacherId = await _context.CourseTeacherAssignments
            .Where(x => x.CourseId == id)
            .ToDictionaryAsync(x => x.TeacherId, x => x.Id);

        var existingEnrollments = await _context.CourseEnrollments
            .Where(e => e.CourseId == id)
            .ToListAsync();

        _context.CourseEnrollments.RemoveRange(existingEnrollments);

        var desiredEnrollments = normalizedAssignments
            .Where(x => teacherAssignmentsByTeacherId.ContainsKey(x.TeacherId))
            .Select(x => new CourseEnrollment
            {
                CourseId = id,
                UserId = x.StudentId,
                CourseTeacherAssignmentId = teacherAssignmentsByTeacherId[x.TeacherId]
            })
            .ToList();

        if (desiredEnrollments.Count > 0)
            _context.CourseEnrollments.AddRange(desiredEnrollments);

        course.TeacherId = selectedTeacherIds.FirstOrDefault();
        _context.Courses.Update(course);

        await _context.SaveChangesAsync();

        TempData["Message"] = "Curso configurado correctamente.";
        return RedirectToAction(nameof(Dashboard));
    }

    private async Task<ManageCourseViewModel?> BuildManageCourseViewModel(
        int courseId,
        IEnumerable<string>? selectedTeacherIds = null,
        IDictionary<string, string>? selectedAssignments = null)
    {
        var course = await _context.Courses
            .Include(c => c.Modules)
                .ThenInclude(m => m.Lessons)
            .FirstOrDefaultAsync(c => c.Id == courseId);

        if (course == null) return null;

        var teacherAssignments = await _context.CourseTeacherAssignments
            .Where(x => x.CourseId == courseId)
            .ToListAsync();

        var teacherIdList = (selectedTeacherIds ?? teacherAssignments.Select(x => x.TeacherId))
            .Distinct()
            .ToList();

        var enrollments = await _context.CourseEnrollments
            .Where(e => e.CourseId == courseId)
            .Include(e => e.CourseTeacherAssignment)
            .ToListAsync();

        var persistedAssignments = enrollments
            .Where(e => e.CourseTeacherAssignment?.TeacherId != null)
            .GroupBy(e => e.UserId)
            .ToDictionary(g => g.Key, g => g.First().CourseTeacherAssignment!.TeacherId);

        var assignmentMap = new Dictionary<string, string?>();
        foreach (var pair in persistedAssignments)
            assignmentMap[pair.Key] = pair.Value;

        if (selectedAssignments != null)
        {
            foreach (var pair in selectedAssignments)
                assignmentMap[pair.Key] = string.IsNullOrWhiteSpace(pair.Value) ? null : pair.Value;
        }

        var students = await _userManager.GetUsersInRoleAsync("Estudiante");
        var teachers = await _userManager.GetUsersInRoleAsync("Docente");

        var totalLessons = course.Modules.SelectMany(m => m.Lessons).Count();
        var studentProgress = await _context.StudentLessonProgresses
            .Where(p => p.IsCompleted && p.Lesson.Module.CourseId == courseId)
            .GroupBy(p => p.UserId)
            .Select(g => new { UserId = g.Key, Completed = g.Count() })
            .ToDictionaryAsync(x => x.UserId, x => x.Completed);

        return new ManageCourseViewModel
        {
            CourseId = course.Id,
            CourseTitle = course.Title,
            SelectedTeacherIds = teacherIdList,
            Teachers = teachers.OrderBy(t => t.Email).ToList(),
            Students = students
                .OrderBy(s => s.Email)
                .Select(student =>
                {
                    var completed = studentProgress.TryGetValue(student.Id, out var value) ? value : 0;
                    var percent = totalLessons == 0 ? 0 : (completed * 100) / totalLessons;

                    return new StudentTeacherAssignmentOptionViewModel
                    {
                        StudentId = student.Id,
                        StudentEmail = student.Email ?? "(sin correo)",
                        AssignedTeacherId = assignmentMap.TryGetValue(student.Id, out var tid) ? tid : null,
                        ProgressPercent = percent
                    };
                })
                .ToList()
        };
    }

    [HttpGet]
    public async Task<IActionResult> ExportStudentsCSV(int courseId)
    {
        var course = await _context.Courses.FindAsync(courseId);
        if (course == null) return NotFound();

        var enrollments = await _context.CourseEnrollments
            .Where(e => e.CourseId == courseId)
            .Include(e => e.User)
            .Include(e => e.CourseTeacherAssignment)
                .ThenInclude(a => a!.Teacher)
            .ToListAsync();

        var totalLessons = await _context.Modules
            .Where(m => m.CourseId == courseId)
            .SelectMany(m => m.Lessons)
            .CountAsync();

        var csvBuilder = new System.Text.StringBuilder();
        csvBuilder.AppendLine("Email Estudiante,Docente,Lecciones Completadas,Total Lecciones,Progreso (porcentaje)");

        foreach (var e in enrollments)
        {
            var completed = await _context.StudentLessonProgresses
                .Where(p => p.UserId == e.UserId && p.IsCompleted && p.Lesson.Module.CourseId == courseId)
                .CountAsync();

            var percent = totalLessons > 0 ? (completed * 100) / totalLessons : 0;
            var teacherEmail = e.CourseTeacherAssignment?.Teacher?.Email ?? "Sin docente";
            csvBuilder.AppendLine($"\"{e.User?.Email}\",\"{teacherEmail}\",{completed},{totalLessons},{percent}");
        }

        var bytes = System.Text.Encoding.UTF8.GetBytes(csvBuilder.ToString());
        var sanitizedTitle = course.Title.Replace(" ", "_").Replace(",", "");
        return File(bytes, "text/csv", $"estudiantes_{sanitizedTitle}_{DateTime.Now:yyyyMMdd}.csv");
    }

    [HttpGet]
    public async Task<IActionResult> Enrollments()
    {
        var enrollments = await _context.CourseEnrollments
            .Include(e => e.Course)
            .Include(e => e.User)
            .Include(e => e.CourseTeacherAssignment)
                .ThenInclude(a => a!.Teacher)
            .ToListAsync();

        return View(enrollments);
    }
}
