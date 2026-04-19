using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace BlindMatchPAS.Models;

// ─── Identity / Users ────────────────────────────────────────────────────────

public class ApplicationUser : IdentityUser
{
    [Required, StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    /// <summary>Student | Supervisor | ModuleLeader | SystemAdmin</summary>
    public string Role { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<Project>         Projects          { get; set; } = [];
    public ICollection<SupervisorExpertise> Expertise     { get; set; } = [];
}

// ─── Research Areas ───────────────────────────────────────────────────────────

public class ResearchArea
{
    public int    Id          { get; set; }

    [Required, StringLength(100)]
    public string Name        { get; set; } = string.Empty;

    [StringLength(300)]
    public string Description { get; set; } = string.Empty;

    public bool   IsActive    { get; set; } = true;

    public ICollection<Project>              Projects   { get; set; } = [];
    public ICollection<SupervisorExpertise>  Expertise  { get; set; } = [];
}

// ─── Supervisor Expertise (many‑to‑many) ─────────────────────────────────────

public class SupervisorExpertise
{
    public int    Id             { get; set; }
    public string SupervisorId   { get; set; } = string.Empty;
    public int    ResearchAreaId { get; set; }

    public ApplicationUser Supervisor   { get; set; } = null!;
    public ResearchArea    ResearchArea { get; set; } = null!;
}

// ─── Projects ─────────────────────────────────────────────────────────────────

public enum ProjectStatus { Pending, UnderReview, Matched, Withdrawn, Rejected }

public class Project
{
    public int    Id           { get; set; }

    [Required, StringLength(200)]
    public string Title        { get; set; } = string.Empty;

    [Required, StringLength(2000)]
    public string Abstract     { get; set; } = string.Empty;

    /// <summary>Comma-separated list of technologies.</summary>
    [Required, StringLength(500)]
    public string TechStack    { get; set; } = string.Empty;

    public int    ResearchAreaId { get; set; }
    public string StudentId      { get; set; } = string.Empty;

    /// <summary>Null until a supervisor confirms the match.</summary>
    public string? SupervisorId { get; set; }

    public ProjectStatus Status    { get; set; } = ProjectStatus.Pending;

    /// <summary>Set to true only after supervisor confirms – triggers identity reveal.</summary>
    public bool IdentityRevealed   { get; set; } = false;

    public DateTime SubmittedAt    { get; set; } = DateTime.UtcNow;
    public DateTime? MatchedAt     { get; set; }

    // Navigation
    public ResearchArea    ResearchArea { get; set; } = null!;
    public ApplicationUser Student     { get; set; } = null!;
    public ApplicationUser? Supervisor { get; set; }

    public ICollection<SupervisorInterest> Interests { get; set; } = [];
}

// ─── Supervisor Interest (expressed, not yet confirmed) ───────────────────────

public class SupervisorInterest
{
    public int    Id           { get; set; }
    public int    ProjectId    { get; set; }
    public string SupervisorId { get; set; } = string.Empty;
    public DateTime ExpressedAt { get; set; } = DateTime.UtcNow;

    public Project         Project    { get; set; } = null!;
    public ApplicationUser Supervisor { get; set; } = null!;
}
