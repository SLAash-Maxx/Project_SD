using BlindMatchPAS.Data;
using BlindMatchPAS.Models;
using Microsoft.EntityFrameworkCore;

namespace BlindMatchPAS.Services;


public interface IMatchingService
{
    
    Task<IEnumerable<BlindProjectCardViewModel>> GetBlindProjectsForSupervisorAsync(string supervisorId);

    Task<bool> ExpressInterestAsync(string supervisorId, int projectId);

    /// <summary>Supervisor confirms a match. Triggers identity reveal.</summary>
    Task<bool> ConfirmMatchAsync(string supervisorId, int projectId);

    /// <summary>Student withdraws a proposal (only if not yet matched).</summary>
    Task<bool> WithdrawProjectAsync(string studentId, int projectId);

    /// <summary>Module leader manual reassignment.</summary>
    Task<bool> ReassignProjectAsync(int projectId, string newSupervisorId);
}

// ─── Implementation ───────────────────────────────────────────────────────────

public class MatchingService : IMatchingService
{
    private readonly ApplicationDbContext _db;

    public MatchingService(ApplicationDbContext db) => _db = db;

    // ── Blind project list ────────────────────────────────────────────────────
    public async Task<IEnumerable<BlindProjectCardViewModel>> GetBlindProjectsForSupervisorAsync(string supervisorId)
    {
        // Get areas this supervisor has selected
        var areaIds = await _db.SupervisorExpertise
            .Where(e => e.SupervisorId == supervisorId)
            .Select(e => e.ResearchAreaId)
            .ToListAsync();

        // Get projects that are Pending or UnderReview in those areas
        // BLIND: we never project student name/id in the returned VM
        var projects = await _db.Projects
            .Include(p => p.ResearchArea)
            .Include(p => p.Interests)
            .Where(p => areaIds.Contains(p.ResearchAreaId)
                     && p.Status != ProjectStatus.Matched
                     && p.Status != ProjectStatus.Withdrawn
                     && p.Status != ProjectStatus.Rejected)
            .ToListAsync();

        var interestedProjectIds = await _db.SupervisorInterests
            .Where(i => i.SupervisorId == supervisorId)
            .Select(i => i.ProjectId)
            .ToListAsync();

        return projects.Select(p => new BlindProjectCardViewModel
        {
            ProjectId       = p.Id,
            Title           = p.Title,
            Abstract        = p.Abstract,
            TechStack       = p.TechStack,
            ResearchArea    = p.ResearchArea.Name,
            Status          = p.Status,
            AlreadyInterested = interestedProjectIds.Contains(p.Id)
            // ⚠ StudentId / Student.FullName are intentionally omitted
        });
    }

    // ── Express interest ──────────────────────────────────────────────────────
    public async Task<bool> ExpressInterestAsync(string supervisorId, int projectId)
    {
        var project = await _db.Projects.FindAsync(projectId);
        if (project is null || project.Status == ProjectStatus.Matched
                             || project.Status == ProjectStatus.Withdrawn)
            return false;

        // Idempotent: don't duplicate interest records
        bool alreadyInterested = await _db.SupervisorInterests
            .AnyAsync(i => i.SupervisorId == supervisorId && i.ProjectId == projectId);
        if (alreadyInterested) return true;

        _db.SupervisorInterests.Add(new SupervisorInterest
        {
            SupervisorId = supervisorId,
            ProjectId    = projectId
        });

        project.Status = ProjectStatus.UnderReview;
        await _db.SaveChangesAsync();
        return true;
    }

    // ── Confirm match (identity reveal) ──────────────────────────────────────
    public async Task<bool> ConfirmMatchAsync(string supervisorId, int projectId)
    {
        var project = await _db.Projects
            .Include(p => p.Student)
            .FirstOrDefaultAsync(p => p.Id == projectId);

        if (project is null || project.Status == ProjectStatus.Matched
                             || project.Status == ProjectStatus.Withdrawn)
            return false;

        // Validate the supervisor previously expressed interest
        bool hasInterest = await _db.SupervisorInterests
            .AnyAsync(i => i.SupervisorId == supervisorId && i.ProjectId == projectId);
        if (!hasInterest) return false;

        // ── IDENTITY REVEAL ────────────────────────────────────────────────
        project.SupervisorId      = supervisorId;
        project.Status            = ProjectStatus.Matched;
        project.IdentityRevealed  = true;   // both parties can now see each other
        project.MatchedAt         = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }

    // ── Withdraw ──────────────────────────────────────────────────────────────
    public async Task<bool> WithdrawProjectAsync(string studentId, int projectId)
    {
        var project = await _db.Projects.FindAsync(projectId);
        if (project is null || project.StudentId != studentId
                             || project.Status == ProjectStatus.Matched)
            return false;

        project.Status = ProjectStatus.Withdrawn;
        await _db.SaveChangesAsync();
        return true;
    }

    // ── Manual reassign (Module Leader) ──────────────────────────────────────
    public async Task<bool> ReassignProjectAsync(int projectId, string newSupervisorId)
    {
        var project = await _db.Projects.FindAsync(projectId);
        if (project is null) return false;

        project.SupervisorId     = newSupervisorId;
        project.Status           = ProjectStatus.Matched;
        project.IdentityRevealed = true;
        project.MatchedAt        = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }
}
