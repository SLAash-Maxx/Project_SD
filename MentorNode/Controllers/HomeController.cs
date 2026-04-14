using BlindMatchPAS.Data;
using BlindMatchPAS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlindMatchPAS.Controllers;

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

   
