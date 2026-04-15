using BlindMatchPAS.Data;
using BlindMatchPAS.Models;
using BlindMatchPAS.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BlindMatchPAS.Controllers;

[Authorize(Roles = "Student")]
public class StudentController : Controller
{
    private readonly ApplicationDbContext        _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IMatchingService            _matching;

    public StudentController(ApplicationDbContext db,
                             UserManager<ApplicationUser> userManager,
                             IMatchingService matching)
    {
        _db          = dbdb;
        _userManager = userManagersystem;
        _matching    = matchingmathc;
    }

    //Dashboard
    public async Task<IActionResult> Dashboard()
    {
        var user = await _userManager.GetUserAsync(User);
        var projects = await _db.Projects
            .Include(p => p.ResearchArea)
            .Include(p => p.Supervisor)
            .Where(p => p.StudentId == user!.Id)
            .OrderByDescending(p => p.SubmittedAt)
            .ToListAsync();

        return View(new StudentDashboardViewModel { Student = user!, Projects = projects });
    }

    //Submit project
    [HttpGet]
    public async Task<IActionResult> Submit()
    {
        var vm = new SubmitProjectViewModel
        {
            ResearchAreas = await _db.ResearchAreas.Where(r => r.IsActive).ToListAsync()
        };
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(SubmitProjectViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.ResearchAreas = await _db.ResearchAreas.Where(r => r.IsActive).ToListAsync();
            return View(model);
        }

        var user = await _userManager.GetUserAsync(User);

        _db.Projects.Add(new Project
        {
            Title         = model.Title,
            Abstract      = model.Abstract,
            TechStack     = model.TechStack,
            ResearchAreaId = model.ResearchAreaId,
            StudentId     = user!.Id,
            Status        = ProjectStatus.Pending
        });

        await _db.SaveChangesAsync();
        TempData["Success"] = "Project proposal submitted successfully!";
        return RedirectToAction(nameof(Dashboard));
    }

    //Edit project
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var user    = await _userManager.GetUserAsync(User);
        var project = await _db.Projects.FindAsync(id);

        if (project is null || project.StudentId != user!.Id)
            return NotFound();
        if (project.Status == ProjectStatus.Matched || project.Status == ProjectStatus.Withdrawn)
            return Forbid();

        return View(new EditProjectViewModel
        {
            Id            = project.Id,
            Title         = project.Title,
            Abstract      = project.Abstract,
            TechStack     = project.TechStack,
            ResearchAreaId = project.ResearchAreaId,
            ResearchAreas  = await _db.ResearchAreas.Where(r => r.IsActive).ToListAsync()
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditProjectViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.ResearchAreas = await _db.ResearchAreas.Where(r => r.IsActive).ToListAsync();
            return View(model);
        }

        var user    = await _userManager.GetUserAsync(User);
        var project = await _db.Projects.FindAsync(model.Id);

        if (project is null || project.StudentId != user!.Id) return NotFound();
        if (project.Status == ProjectStatus.Matched) return Forbid();

        project.Title          = model.Title;
        project.Abstract       = model.Abstract;
        project.TechStack      = model.TechStack;
        project.ResearchAreaId = model.ResearchAreaId;

        await _db.SaveChangesAsync();
        TempData["Success"] = "Proposal updated successfully.";
        return RedirectToAction(nameof(Dashboard));
    }

    //Withdraw project
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Withdraw(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        var ok   = await _matching.WithdrawProjectAsync(user!.Id, id);

        TempData[ok ? "Success" : "Error"] = ok
            ? "Project withdrawn."
            : "Cannot withdraw a matched project.";

        return RedirectToAction(nameof(Dashboard));
    }

    //View project details (if matched)
    public async Task<IActionResult> Details(int id)
    {
        var user    = await _userManager.GetUserAsync(User);
        var project = await _db.Projects
            .Include(p => p.ResearchArea)
            .Include(p => p.Supervisor)
            .FirstOrDefaultAsync(p => p.Id == id && p.StudentId == user!.Id);

        if (project is null) return NotFound();
        return View(project);
    }
}
