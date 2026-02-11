using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RoboticCoders.Models;

namespace RoboticCoders.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    // ================= LOGIN =================
  [HttpGet]
public IActionResult Login(string? returnUrl = null)
{
    ViewData["ReturnUrl"] = returnUrl;
    return View();
}


    [HttpPost]
public async Task<IActionResult> Login(
    string email,
    string password,
    string? returnUrl = null)
{
    var user = await _userManager.FindByEmailAsync(email);

    if (user == null)
    {
        ModelState.AddModelError("", "Usuario no encontrado");
        return View();
    }

    var result = await _signInManager.PasswordSignInAsync(
        user.UserName!,
        password,
        false,
        false
    );

    if (!result.Succeeded)
    {
        ModelState.AddModelError("", "Credenciales incorrectas");
        return View();
    }

    // =========================
    // REDIRECCIÓN SEGURA POR ROL
    // =========================

    if (await _userManager.IsInRoleAsync(user, "Admin"))
        return RedirectToAction("Dashboard", "Admin");

    if (await _userManager.IsInRoleAsync(user, "Docente"))
        return RedirectToAction("Dashboard", "Teacher");

    if (await _userManager.IsInRoleAsync(user, "Estudiante"))
        return RedirectToAction("Dashboard", "Student");

    // fallback
    return RedirectToAction("Index", "Home");
}



    // ================= LOGOUT =================
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Login");
    }

  

    // ================= FORGOT PASSWORD =================
    [HttpGet]
    public IActionResult Forgot() => View();

    [HttpPost]
    public async Task<IActionResult> Forgot(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
            return View("ForgotConfirmation");

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);

        // Aquí iría el envío por correo
        Console.WriteLine($"TOKEN RESET: {token}");

        return View("ForgotConfirmation");
    }

    [HttpGet]
    public IActionResult AccessDenied(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }
}
