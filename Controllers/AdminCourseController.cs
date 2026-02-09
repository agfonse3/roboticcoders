using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoboticCoders.Data;
using RoboticCoders.Models;

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

    // VER MÓDULOS DEL CURSO
    public async Task<IActionResult> Modules(int id)
    {
        var course = await _context.Courses
            .Include(c => c.Modules)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (course == null) return NotFound();

        return View(course);
    }
}
