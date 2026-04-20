using BlindMatchPAS.Data;
using BlindMatchPAS.Models;
using BlindMatchPAS.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlindMatchPAS.Controllers;

[Authorize(Roles = "ModuleLeader")]
public class ModuleLeaderController : Controller
{
    private readonly ApplicationDbContext        _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IMatchingService            _matching;

    public ModuleLeaderController(ApplicationDbContext db,
                                  UserManager<ApplicationUser> userManager,
                                  IMatchingService matching)
    {
        _db          = db;
        _userManager = userManager;
        _matching    = matching;
    }

    public async Task<IActionResult> Dashboard()
    {
        var allProjects = await _db.Projects
            .Include(p => p.ResearchArea)
            .Include(p => p.Student)
            .Include(p => p.Supervisor)
            .OrderByDescending(p => p.SubmittedAt)
            .ToListAsync();

        var students    = await _userManager.GetUsersInRoleAsync("Student");
        var supervisors = await _userManager.GetUsersInRoleAsync("Supervisor");

        return View(new AllocationOverviewViewModel
        {
            AllProjects = allProjects,
            Students    = students,
            Supervisors = supervisors
        });
    }

    public async Task<IActionResult> ManageAreas()
        => View(new ManageAreasViewModel
        {
            Areas = await _db.ResearchAreas.OrderBy(a => a.Name).ToListAsync()
        });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddArea(string name, string description)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "Area name is required.";
            return RedirectToAction(nameof(ManageAreas));
        }

        _db.ResearchAreas.Add(new ResearchArea
        {
            Name        = name.Trim(),
            Description = description?.Trim() ?? string.Empty,
            IsActive    = true
        });
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Research area '{name}' added.";
        return RedirectToAction(nameof(ManageAreas));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleArea(int id)
    {
        var area = await _db.ResearchAreas.FindAsync(id);
        if (area is null) return NotFound();

        area.IsActive = !area.IsActive;
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Area '{area.Name}' {(area.IsActive ? "activated" : "deactivated")}.";
        return RedirectToAction(nameof(ManageAreas));
    }

    public async Task<IActionResult> Users()
    {
        var users = await _db.Users.OrderBy(u => u.FullName).ToListAsync();
        return View(users);
    }

    [HttpGet]
    public IActionResult CreateUser() => View(new CreateUserViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateUser(CreateUserViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var allowedRoles = new[] { "Student", "Supervisor", "ModuleLeader" };
        if (!allowedRoles.Contains(model.Role))
        {
            ModelState.AddModelError("Role", "Invalid role selected.");
            return View(model);
        }

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
            TempData["Success"] = $"{model.Role} account created for {model.FullName}.";
            return RedirectToAction(nameof(Users));
        }

        foreach (var error in result.Errors)
            ModelState.AddModelError(string.Empty, error.Description);
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Reassign(int projectId, string supervisorId)
    {
        var ok = await _matching.ReassignProjectAsync(projectId, supervisorId);
        TempData[ok ? "Success" : "Error"] = ok
            ? "Project reassigned successfully."
            : "Reassignment failed.";
        return RedirectToAction(nameof(Dashboard));
    }
}
