using BlindMatchPAS.Data;
using BlindMatchPAS.Models;
using BlindMatchPAS.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BlindMatchPAS.Tests.Integration;

/// <summary>
/// Integration tests validating EF Core persistence behaviour,
/// relational constraints, and the full match lifecycle.
/// Uses InMemory provider — swap to UseSqlite(":memory:") for FK enforcement.
/// </summary>
public class DatabaseIntegrationTests : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly IMatchingService     _sut;

    public DatabaseIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db  = new ApplicationDbContext(options);
        _sut = new MatchingService(_db);
    }

    public void Dispose() => _db.Dispose();


    private ApplicationUser MakeUser(string id, string role) => new()
    {
        Id = id, UserName = $"{id}@test.com", Email = $"{id}@test.com",
        NormalizedEmail = $"{id}@TEST.COM", NormalizedUserName = $"{id}@TEST.COM",
        FullName = $"User {id}", Role = role,
        SecurityStamp = Guid.NewGuid().ToString(), EmailConfirmed = true
    };

    private async Task<(ApplicationUser student, ApplicationUser supervisor, ResearchArea area)> SetupBaseAsync()
    {
        var area = new ResearchArea { Id = 10, Name = "Cybersecurity", Description = "Security", IsActive = true };
        var student    = MakeUser("s1", "Student");
        var supervisor = MakeUser("sv1", "Supervisor");

        _db.ResearchAreas.Add(area);
        _db.Users.AddRange(student, supervisor);
        _db.SupervisorExpertise.Add(new SupervisorExpertise
            { SupervisorId = "sv1", ResearchAreaId = 10 });

        await _db.SaveChangesAsync();
        return (student, supervisor, area);
    }


    [Fact]
    public async Task ResearchArea_CanBeCreatedAndRetrieved()
    {
        _db.ResearchAreas.Add(new ResearchArea { Name = "IoT", Description = "Internet of Things", IsActive = true });
        await _db.SaveChangesAsync();

        var area = await _db.ResearchAreas.FirstOrDefaultAsync(a => a.Name == "IoT");
        area.Should().NotBeNull();
        area!.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task ResearchArea_CanBeDeactivated()
    {
        _db.ResearchAreas.Add(new ResearchArea { Id = 50, Name = "Legacy", Description = "Old area", IsActive = true });
        await _db.SaveChangesAsync();

        var area = await _db.ResearchAreas.FindAsync(50);
        area!.IsActive = false;
        await _db.SaveChangesAsync();

        var loaded = await _db.ResearchAreas.FindAsync(50);
        loaded!.IsActive.Should().BeFalse();
    }


    [Fact]
    public async Task Project_IsPersisted_WithCorrectDefaults()
    {
        var (student, _, area) = await SetupBaseAsync();

        _db.Projects.Add(new Project
        {
            Title = "Test Project", Abstract = "An abstract.",
            TechStack = "C#", ResearchAreaId = area.Id, StudentId = student.Id
        });
        await _db.SaveChangesAsync();

        var proj = await _db.Projects.FirstOrDefaultAsync(p => p.Title == "Test Project");
        proj.Should().NotBeNull();
        proj!.Status.Should().Be(ProjectStatus.Pending);
        proj.IdentityRevealed.Should().BeFalse();
        proj.SupervisorId.Should().BeNull();
        proj.MatchedAt.Should().BeNull();
    }


    [Fact]
    public async Task FullMatchLifecycle_PendingToRevealedInThreeSteps()
    {
        var (student, supervisor, area) = await SetupBaseAsync();

        _db.Projects.Add(new Project
        {
            Id = 1, Title = "Blockchain Voting", Abstract = "Secure voting via blockchain.",
            TechStack = "Solidity, React", ResearchAreaId = area.Id, StudentId = student.Id
        });
        await _db.SaveChangesAsync();

        var blind = (await _sut.GetBlindProjectsForSupervisorAsync(supervisor.Id)).ToList();
        blind.Should().HaveCount(1);
        blind[0].Title.Should().Be("Blockchain Voting");

        await _sut.ExpressInterestAsync(supervisor.Id, 1);
        var afterInterest = await _db.Projects.FindAsync(1);
        afterInterest!.Status.Should().Be(ProjectStatus.UnderReview);

        await _sut.ConfirmMatchAsync(supervisor.Id, 1);
        var afterMatch = await _db.Projects
            .Include(p => p.Student)
            .Include(p => p.Supervisor)
            .FirstAsync(p => p.Id == 1);

        afterMatch.Status.Should().Be(ProjectStatus.Matched);
        afterMatch.IdentityRevealed.Should().BeTrue();
        afterMatch.SupervisorId.Should().Be(supervisor.Id);
        afterMatch.Student.FullName.Should().Be(student.FullName); 
    }


    [Fact]
    public async Task SupervisorInterest_IsUnique_PerSupervisorAndProject()
    {
        var (student, supervisor, area) = await SetupBaseAsync();

        _db.Projects.Add(new Project
        {
            Id = 2, Title = "P2", Abstract = "Abstract", TechStack = "Java",
            ResearchAreaId = area.Id, StudentId = student.Id
        });
        await _db.SaveChangesAsync();

        await _sut.ExpressInterestAsync(supervisor.Id, 2);
        await _sut.ExpressInterestAsync(supervisor.Id, 2); 

        var count = await _db.SupervisorInterests.CountAsync(
            i => i.SupervisorId == supervisor.Id && i.ProjectId == 2);
        count.Should().Be(1);
    }


    [Fact]
    public async Task Withdraw_ProjectRecordRemains_WithWithdrawnStatus()
    {
        var (student, _, area) = await SetupBaseAsync();

        _db.Projects.Add(new Project
        {
            Id = 3, Title = "P3", Abstract = "Abs", TechStack = "Go",
            ResearchAreaId = area.Id, StudentId = student.Id
        });
        await _db.SaveChangesAsync();

        await _sut.WithdrawProjectAsync(student.Id, 3);

        var proj = await _db.Projects.FindAsync(3);
        proj.Should().NotBeNull();
        proj!.Status.Should().Be(ProjectStatus.Withdrawn);
    }


    [Fact]
    public async Task SupervisorExpertise_CanBeUpdated()
    {
        await SetupBaseAsync();

        var newArea = new ResearchArea { Id = 20, Name = "Quantum", Description = "Quantum computing", IsActive = true };
        _db.ResearchAreas.Add(newArea);
        await _db.SaveChangesAsync();

        _db.SupervisorExpertise.RemoveRange(
            _db.SupervisorExpertise.Where(e => e.SupervisorId == "sv1"));
        _db.SupervisorExpertise.Add(new SupervisorExpertise
            { SupervisorId = "sv1", ResearchAreaId = 20 });
        await _db.SaveChangesAsync();

        var areas = await _db.SupervisorExpertise
            .Where(e => e.SupervisorId == "sv1")
            .Select(e => e.ResearchAreaId)
            .ToListAsync();

        areas.Should().ContainSingle().Which.Should().Be(20);
    }
}
