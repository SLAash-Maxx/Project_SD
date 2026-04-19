using BlindMatchPAS.Data;
using BlindMatchPAS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlindMatchPAS.Controllers;

// ─── System Admin ─────────────────────────────────────────────────────────────

[Authorize(Roles = "SystemAdmin")]
public class AdminController : Controller
{
    private readonly ApplicationDbContext        _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminController(ApplicationDbContext db,
                           UserManager<ApplicationUser> userManager)
    {
        _db          = db;
        _userManager = userManager;
    }

    public async Task<IActionResult> Dashboard()
    {
        ViewData["UserCount"]    = await _db.Users.CountAsync();
        ViewData["ProjectCount"] = await _db.Projects.CountAsync();
        ViewData["MatchedCount"] = await _db.Projects
            .CountAsync(p => p.Status == ProjectStatus.Matched);
        return View();
    }

    // User management (full CRUD for all roles)
    public async Task<IActionResult> Users()
    {
        var users = await _db.Users
            .OrderBy(u => u.Role)
            .ThenBy(u => u.FullName)
            .ToListAsync();
        return View(users);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteUser(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null) return NotFound();

        // Prevent self-deletion
        if (user.Id == _userManager.GetUserId(User))
        {
            TempData["Error"] = "You cannot delete your own account.";
            return RedirectToAction(nameof(Users));
        }

        await _userManager.DeleteAsync(user);
        TempData["Success"] = $"User {user.FullName} deleted.";
        return RedirectToAction(nameof(Users));
    }

    [HttpGet]
    public IActionResult CreateUser() => View(new CreateUserViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateUser(CreateUserViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email    = model.Email,
            FullName = model.FullName,
            Role     = model.Role,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, model.Role);
            TempData["Success"] = $"Account created for {model.FullName} as {model.Role}.";
            return RedirectToAction(nameof(Users));
        }

        foreach (var e in result.Errors)
            ModelState.AddModelError(string.Empty, e.Description);
        return View(model);
    }
}

// ─── Home ─────────────────────────────────────────────────────────────────────

public class HomeController : Controller
{
    public IActionResult Index()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            if (User.IsInRole("Student"))     return RedirectToAction("Dashboard", "Student");
            if (User.IsInRole("Supervisor"))  return RedirectToAction("Dashboard", "Supervisor");
            if (User.IsInRole("ModuleLeader"))return RedirectToAction("Dashboard", "ModuleLeader");
            if (User.IsInRole("SystemAdmin")) return RedirectToAction("Dashboard", "Admin");
        }
        return View();
    }

    public IActionResult Privacy() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View();
}
