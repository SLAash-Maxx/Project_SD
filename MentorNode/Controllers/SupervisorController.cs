using BlindMatchPAS.Data;
using BlindMatchPAS.Models;
using BlindMatchPAS.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlindMatchPAS.Controllers;

[Authorize(Roles = "Supervisor")]
public class SupervisorController : Controller
{
    private readonly ApplicationDbContext        _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IMatchingService            _matching;

    public SupervisorController(ApplicationDbContext db,
                                UserManager<ApplicationUser> userManager,
                                IMatchingService matching)
    {
        _db          = db;
        _userManager = userManager;
        _matching    = matching;
    }

    public async Task<IActionResult> Dashboard()
    {
        var user = await _userManager.GetUserAsync(User);

        var selectedAreaIds = await _db.SupervisorExpertise
            .Where(e => e.SupervisorId == user!.Id)
            .Select(e => e.ResearchAreaId)
            .ToListAsync();

        var blindProjects = await _matching.GetBlindProjectsForSupervisorAsync(user!.Id);

        var confirmedProjects = await _db.Projects
            .Include(p => p.ResearchArea)
            .Include(p => p.Student)
            .Where(p => p.SupervisorId == user.Id && p.IdentityRevealed)
            .ToListAsync();

        var vm = new SupervisorDashboardViewModel
        {
            AvailableProjects = blindProjects,
            ConfirmedProjects  = confirmedProjects,
            AllAreas           = await _db.ResearchAreas.Where(r => r.IsActive).ToListAsync(),
            SelectedAreaIds    = selectedAreaIds
        };

        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateExpertise(int[] areaIds)
    {
        var user = await _userManager.GetUserAsync(User);

        var existing = _db.SupervisorExpertise.Where(e => e.SupervisorId == user!.Id);
        _db.SupervisorExpertise.RemoveRange(existing);

        foreach (var areaId in areaIds)
            _db.SupervisorExpertise.Add(new SupervisorExpertise
            {
                SupervisorId   = user!.Id,
                ResearchAreaId = areaId
            });

        await _db.SaveChangesAsync();
        TempData["Success"] = "Research area preferences updated.";
        return RedirectToAction(nameof(Dashboard));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ExpressInterest(int projectId)
    {
        var user = await _userManager.GetUserAsync(User);
        var ok   = await _matching.ExpressInterestAsync(user!.Id, projectId);

        TempData[ok ? "Success" : "Error"] = ok
            ? "Interest expressed. You can confirm the match from your dashboard."
            : "Could not express interest in this project.";

        return RedirectToAction(nameof(Dashboard));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmMatch(int projectId)
    {
        var user = await _userManager.GetUserAsync(User);
        var ok   = await _matching.ConfirmMatchAsync(user!.Id, projectId);

        TempData[ok ? "Success" : "Error"] = ok
            ? "Match confirmed! Student identity has been revealed."
            : "Could not confirm match.";

        return RedirectToAction(nameof(Dashboard));
    }

    public async Task<IActionResult> MatchedProject(int id)
    {
        var user    = await _userManager.GetUserAsync(User);
        var project = await _db.Projects
            .Include(p => p.ResearchArea)
            .Include(p => p.Student)
            .FirstOrDefaultAsync(p => p.Id == id
                                   && p.SupervisorId == user!.Id
                                   && p.IdentityRevealed);

        if (project is null) return NotFound();
        return View(project);
    }
}
