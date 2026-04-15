using BlindMatchPAS.Data;
using BlindMatchPAS.Models;
using Microsoft.EntityFrameworkCore;

namespace BlindMatchPAS.Services;


public interface IMatchingService
{
    
    Task<IEnumerable<BlindProjectCardViewModel>> GetBlindProjectsForSupervisorAsync(string supervisorId);

    Task<bool> ExpressInterestAsync(string supervisorId, int projectId);

    Task<bool> ConfirmMatchAsync(string supervisorId, int projectId);

    Task<bool> WithdrawProjectAsync(string studentId, int projectId);

    Task<bool> ReassignProjectAsync(int projectId, string newSupervisorId);
}


public class MatchingService : IMatchingService
{
    private readonly ApplicationDbContext _db;

    public MatchingService(ApplicationDbContext db) => _db = db;

    public async Task<IEnumerable<BlindProjectCardViewModel>> GetBlindProjectsForSupervisorAsync(string supervisorId)
    {
        var areaIds = await _db.SupervisorExpertise
            .Where(e => e.SupervisorId == supervisorId)
            .Select(e => e.ResearchAreaId)
            .ToListAsync();

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
        });
    }

    public async Task<bool> ExpressInterestAsync(string supervisorId, int projectId)
    {
        var project = await _db.Projects.FindAsync(projectId);
        if (project is null || project.Status == ProjectStatus.Matched
                             || project.Status == ProjectStatus.Withdrawn)
            return false;

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

    public async Task<bool> ConfirmMatchAsync(string supervisorId, int projectId)
    {
        var project = await _db.Projects
            .Include(p => p.Student)
            .FirstOrDefaultAsync(p => p.Id == projectId);

        if (project is null || project.Status == ProjectStatus.Matched
                             || project.Status == ProjectStatus.Withdrawn)
            return false;

        bool hasInterest = await _db.SupervisorInterests
            .AnyAsync(i => i.SupervisorId == supervisorId && i.ProjectId == projectId);
        if (!hasInterest) return false;

        project.SupervisorId      = supervisorId;
        project.Status            = ProjectStatus.Matched;
        project.IdentityRevealed  = true;  
        project.MatchedAt         = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }

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
