using BlindMatchPAS.Data;
using BlindMatchPAS.Models;
using BlindMatchPAS.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BlindMatchPAS.Tests.Unit;

/// <summary>
/// Unit tests for MatchingService — the core blind-match business logic.
/// Uses EF Core InMemory provider and no mocked DB so we can validate
/// the full state-machine transitions end-to-end without I/O.
/// </summary>
public class MatchingServiceTests : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly IMatchingService     _sut;

    // ── Fixture helpers ───────────────────────────────────────────────────────

    private const string StudentId    = "student-1";
    private const string SupervisorId = "supervisor-1";

    public MatchingServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())   // fresh DB per test
            .Options;

        _db  = new ApplicationDbContext(options);
        _sut = new MatchingService(_db);

        SeedDatabase();
    }

    private void SeedDatabase()
    {
        var area = new ResearchArea { Id = 1, Name = "AI", Description = "Artificial Intelligence", IsActive = true };
        _db.ResearchAreas.Add(area);

        var student = new ApplicationUser
        {
            Id = StudentId, UserName = "s@test.com", Email = "s@test.com",
            FullName = "Alice Student", Role = "Student", NormalizedEmail = "S@TEST.COM",
            NormalizedUserName = "S@TEST.COM", SecurityStamp = Guid.NewGuid().ToString()
        };
        var supervisor = new ApplicationUser
        {
            Id = SupervisorId, UserName = "sv@test.com", Email = "sv@test.com",
            FullName = "Dr Bob", Role = "Supervisor", NormalizedEmail = "SV@TEST.COM",
            NormalizedUserName = "SV@TEST.COM", SecurityStamp = Guid.NewGuid().ToString()
        };
        _db.Users.AddRange(student, supervisor);

        // Supervisor has selected area 1
        _db.SupervisorExpertise.Add(new SupervisorExpertise
        {
            Id = 1, SupervisorId = SupervisorId, ResearchAreaId = 1
        });

        // One pending project
        _db.Projects.Add(new Project
        {
            Id = 1, Title = "Smart Proctoring", Abstract = "AI exam proctoring.",
            TechStack = "Python, TensorFlow", ResearchAreaId = 1,
            StudentId = StudentId, Status = ProjectStatus.Pending
        });

        _db.SaveChanges();
    }

    public void Dispose() => _db.Dispose();

    // ── Tests: GetBlindProjects ───────────────────────────────────────────────

    [Fact]
    public async Task GetBlindProjects_ReturnsProjectsMatchingSupervisorAreas()
    {
        var result = await _sut.GetBlindProjectsForSupervisorAsync(SupervisorId);

        result.Should().HaveCount(1);
        result.First().Title.Should().Be("Smart Proctoring");
    }

    [Fact]
    public async Task GetBlindProjects_DoesNotExposeStudentIdentity()
    {
        // The ViewModel must not carry any student-identifying info
        var result = (await _sut.GetBlindProjectsForSupervisorAsync(SupervisorId)).ToList();

        result.Should().HaveCount(1);
        // BlindProjectCardViewModel has no StudentId or StudentName property – compile-time guarantee.
        // Runtime check: serialise and confirm no student ID leaks.
        var json = System.Text.Json.JsonSerializer.Serialize(result.First());
        json.Should().NotContain(StudentId, because: "blind view must not expose student identity");
    }

    [Fact]
    public async Task GetBlindProjects_ExcludesMatchedProjects()
    {
        var proj = await _db.Projects.FindAsync(1);
        proj!.Status = ProjectStatus.Matched;
        await _db.SaveChangesAsync();

        var result = await _sut.GetBlindProjectsForSupervisorAsync(SupervisorId);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetBlindProjects_ExcludesWithdrawnProjects()
    {
        var proj = await _db.Projects.FindAsync(1);
        proj!.Status = ProjectStatus.Withdrawn;
        await _db.SaveChangesAsync();

        var result = await _sut.GetBlindProjectsForSupervisorAsync(SupervisorId);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetBlindProjects_EmptyWhenSupervisorHasNoAreas()
    {
        // Remove expertise
        _db.SupervisorExpertise.RemoveRange(_db.SupervisorExpertise);
        await _db.SaveChangesAsync();

        var result = await _sut.GetBlindProjectsForSupervisorAsync(SupervisorId);

        result.Should().BeEmpty();
    }

    // ── Tests: ExpressInterest ────────────────────────────────────────────────

    [Fact]
    public async Task ExpressInterest_ReturnsTrueAndChangesStatusToUnderReview()
    {
        var ok = await _sut.ExpressInterestAsync(SupervisorId, 1);

        ok.Should().BeTrue();

        var proj = await _db.Projects.FindAsync(1);
        proj!.Status.Should().Be(ProjectStatus.UnderReview);
    }

    [Fact]
    public async Task ExpressInterest_CreatesInterestRecord()
    {
        await _sut.ExpressInterestAsync(SupervisorId, 1);

        var interest = await _db.SupervisorInterests
            .FirstOrDefaultAsync(i => i.SupervisorId == SupervisorId && i.ProjectId == 1);

        interest.Should().NotBeNull();
    }

    [Fact]
    public async Task ExpressInterest_IsIdempotent_NoDuplicateRecords()
    {
        await _sut.ExpressInterestAsync(SupervisorId, 1);
        var ok = await _sut.ExpressInterestAsync(SupervisorId, 1); // second call

        ok.Should().BeTrue();
        var count = await _db.SupervisorInterests.CountAsync(
            i => i.SupervisorId == SupervisorId && i.ProjectId == 1);
        count.Should().Be(1);
    }

    [Fact]
    public async Task ExpressInterest_ReturnsFalseForAlreadyMatchedProject()
    {
        var proj = await _db.Projects.FindAsync(1);
        proj!.Status = ProjectStatus.Matched;
        await _db.SaveChangesAsync();

        var ok = await _sut.ExpressInterestAsync(SupervisorId, 1);

        ok.Should().BeFalse();
    }

    [Fact]
    public async Task ExpressInterest_ReturnsFalseForNonExistentProject()
    {
        var ok = await _sut.ExpressInterestAsync(SupervisorId, 999);
        ok.Should().BeFalse();
    }

    // ── Tests: ConfirmMatch (Identity Reveal) ─────────────────────────────────

    [Fact]
    public async Task ConfirmMatch_SetsMatchedStatusAndRevealsIdentity()
    {
        await _sut.ExpressInterestAsync(SupervisorId, 1);
        var ok = await _sut.ConfirmMatchAsync(SupervisorId, 1);

        ok.Should().BeTrue();

        var proj = await _db.Projects.FindAsync(1);
        proj!.Status.Should().Be(ProjectStatus.Matched);
        proj.IdentityRevealed.Should().BeTrue();
        proj.SupervisorId.Should().Be(SupervisorId);
        proj.MatchedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ConfirmMatch_ReturnsFalseWithoutPriorInterest()
    {
        // No ExpressInterest call first
        var ok = await _sut.ConfirmMatchAsync(SupervisorId, 1);
        ok.Should().BeFalse();
    }

    [Fact]
    public async Task ConfirmMatch_ReturnsFalseForAlreadyMatchedProject()
    {
        await _sut.ExpressInterestAsync(SupervisorId, 1);
        await _sut.ConfirmMatchAsync(SupervisorId, 1);            // first confirm

        var ok = await _sut.ConfirmMatchAsync(SupervisorId, 1);  // duplicate
        ok.Should().BeFalse();
    }

    [Fact]
    public async Task ConfirmMatch_SetsMatchedAtTimestamp()
    {
        var before = DateTime.UtcNow;
        await _sut.ExpressInterestAsync(SupervisorId, 1);
        await _sut.ConfirmMatchAsync(SupervisorId, 1);
        var after = DateTime.UtcNow;

        var proj = await _db.Projects.FindAsync(1);
        proj!.MatchedAt.Should().BeOnOrAfter(before)
                               .And.BeOnOrBefore(after);
    }

    // ── Tests: WithdrawProject ────────────────────────────────────────────────

    [Fact]
    public async Task Withdraw_SetsStatusToWithdrawn()
    {
        var ok = await _sut.WithdrawProjectAsync(StudentId, 1);

        ok.Should().BeTrue();
        var proj = await _db.Projects.FindAsync(1);
        proj!.Status.Should().Be(ProjectStatus.Withdrawn);
    }

    [Fact]
    public async Task Withdraw_ReturnsFalseIfWrongStudent()
    {
        var ok = await _sut.WithdrawProjectAsync("wrong-student", 1);
        ok.Should().BeFalse();
    }

    [Fact]
    public async Task Withdraw_ReturnsFalseIfAlreadyMatched()
    {
        await _sut.ExpressInterestAsync(SupervisorId, 1);
        await _sut.ConfirmMatchAsync(SupervisorId, 1);

        var ok = await _sut.WithdrawProjectAsync(StudentId, 1);
        ok.Should().BeFalse();
    }

    // ── Tests: ReassignProject ────────────────────────────────────────────────

    [Fact]
    public async Task Reassign_ChangesProjectSupervisorAndRevealsIdentity()
    {
        var newSv = new ApplicationUser
        {
            Id = "sv-2", UserName = "sv2@test.com", Email = "sv2@test.com",
            FullName = "Dr Carol", Role = "Supervisor",
            SecurityStamp = Guid.NewGuid().ToString()
        };
        _db.Users.Add(newSv);
        await _db.SaveChangesAsync();

        var ok = await _sut.ReassignProjectAsync(1, "sv-2");

        ok.Should().BeTrue();
        var proj = await _db.Projects.FindAsync(1);
        proj!.SupervisorId.Should().Be("sv-2");
        proj.IdentityRevealed.Should().BeTrue();
        proj.Status.Should().Be(ProjectStatus.Matched);
    }

    [Fact]
    public async Task Reassign_ReturnsFalseForNonExistentProject()
    {
        var ok = await _sut.ReassignProjectAsync(999, SupervisorId);
        ok.Should().BeFalse();
    }
}
