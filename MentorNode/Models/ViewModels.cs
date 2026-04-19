using System.ComponentModel.DataAnnotations;

namespace BlindMatchPAS.Models;

// ─── Account ──────────────────────────────────────────────────────────────────

public class LoginViewModel
{
    [Required, EmailAddress]
    public string Email    { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }
}

public class RegisterViewModel
{
    [Required, StringLength(100)]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email    { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), StringLength(100, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password), Compare(nameof(Password))]
    [Display(Name = "Confirm Password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required]
    public string Role { get; set; } = "Student";
}

// ─── Student ──────────────────────────────────────────────────────────────────

public class SubmitProjectViewModel
{
    [Required, StringLength(200)]
    public string Title        { get; set; } = string.Empty;

    [Required, StringLength(2000)]
    public string Abstract     { get; set; } = string.Empty;

    [Required, StringLength(500)]
    [Display(Name = "Technology Stack")]
    public string TechStack    { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Research Area")]
    public int ResearchAreaId  { get; set; }

    public IEnumerable<ResearchArea> ResearchAreas { get; set; } = [];
}

public class EditProjectViewModel : SubmitProjectViewModel
{
    public int Id { get; set; }
}

public class StudentDashboardViewModel
{
    public ApplicationUser Student  { get; set; } = null!;
    public IEnumerable<Project> Projects { get; set; } = [];
}

// ─── Supervisor ───────────────────────────────────────────────────────────────

/// <summary>Blind project card – no student identity exposed.</summary>
public class BlindProjectCardViewModel
{
    public int    ProjectId    { get; set; }
    public string Title        { get; set; } = string.Empty;
    public string Abstract     { get; set; } = string.Empty;
    public string TechStack    { get; set; } = string.Empty;
    public string ResearchArea { get; set; } = string.Empty;
    public ProjectStatus Status { get; set; }
    public bool AlreadyInterested { get; set; }
}

public class SupervisorDashboardViewModel
{
    public IEnumerable<BlindProjectCardViewModel> AvailableProjects { get; set; } = [];
    public IEnumerable<Project>                   ConfirmedProjects  { get; set; } = [];
    public IEnumerable<ResearchArea>              AllAreas           { get; set; } = [];
    public IEnumerable<int>                       SelectedAreaIds    { get; set; } = [];
}

// ─── Module Leader ────────────────────────────────────────────────────────────

public class AllocationOverviewViewModel
{
    public IEnumerable<Project>         AllProjects  { get; set; } = [];
    public IEnumerable<ApplicationUser> Students     { get; set; } = [];
    public IEnumerable<ApplicationUser> Supervisors  { get; set; } = [];
}

public class CreateUserViewModel
{
    [Required, StringLength(100)]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email    { get; set; } = string.Empty;

    [Required]
    public string Role     { get; set; } = "Student";

    [Required, DataType(DataType.Password), StringLength(100, MinimumLength = 8)]
    [Display(Name = "Temporary Password")]
    public string Password { get; set; } = string.Empty;
}

public class ManageAreasViewModel
{
    public IEnumerable<ResearchArea> Areas { get; set; } = [];
    public string NewAreaName        { get; set; } = string.Empty;
    public string NewAreaDescription { get; set; } = string.Empty;
}
