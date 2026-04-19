using BlindMatchPAS.Controllers;
using BlindMatchPAS.Models;
using BlindMatchPAS.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using System.Security.Claims;
using Xunit;

namespace BlindMatchPAS.Tests.Functional;

/// <summary>
/// Functional tests for controller actions using Moq to isolate
/// IMatchingService and UserManager dependencies.
/// </summary>
public class SupervisorControllerTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static UserManager<ApplicationUser> MockUserManager(ApplicationUser user)
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        var mgr   = new Mock<UserManager<ApplicationUser>>(
            store.Object, null, null, null, null, null, null, null, null);

        mgr.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
           .ReturnsAsync(user);

        return mgr.Object;
    }

    private static SupervisorController BuildController(
        ApplicationUser user,
        Mock<IMatchingService> matchSvc,
        BlindMatchPAS.Data.ApplicationDbContext db)
    {
        var ctrl = new SupervisorController(db, MockUserManager(user), matchSvc.Object);

        ctrl.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    new[] { new Claim(ClaimTypes.NameIdentifier, user.Id) }, "Test"))
            }
        };

        // Provide TempData so controller actions can set it
        ctrl.TempData = new TempDataDictionary(
            ctrl.ControllerContext.HttpContext,
            Mock.Of<ITempDataProvider>());

        return ctrl;
    }

    private static BlindMatchPAS.Data.ApplicationDbContext MakeDb()
    {
        var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<BlindMatchPAS.Data.ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new BlindMatchPAS.Data.ApplicationDbContext(options);
    }

    // ── ExpressInterest ───────────────────────────────────────────────────────

    [Fact]
    public async Task ExpressInterest_RedirectsToDashboard_OnSuccess()
    {
        var user    = new ApplicationUser { Id = "sv1", Role = "Supervisor" };
        var matchSvc = new Mock<IMatchingService>();
        matchSvc.Setup(m => m.ExpressInterestAsync("sv1", 5)).ReturnsAsync(true);

        var ctrl   = BuildController(user, matchSvc, MakeDb());
        var result = await ctrl.ExpressInterest(5) as RedirectToActionResult;

        result.Should().NotBeNull();
        result!.ActionName.Should().Be("Dashboard");
    }

    [Fact]
    public async Task ExpressInterest_SetsErrorTempData_OnFailure()
    {
        var user    = new ApplicationUser { Id = "sv1", Role = "Supervisor" };
        var matchSvc = new Mock<IMatchingService>();
        matchSvc.Setup(m => m.ExpressInterestAsync("sv1", 5)).ReturnsAsync(false);

        var ctrl = BuildController(user, matchSvc, MakeDb());
        await ctrl.ExpressInterest(5);

        ctrl.TempData.ContainsKey("Error").Should().BeTrue();
    }

    // ── ConfirmMatch ──────────────────────────────────────────────────────────

    [Fact]
    public async Task ConfirmMatch_RedirectsToDashboard_OnSuccess()
    {
        var user    = new ApplicationUser { Id = "sv1", Role = "Supervisor" };
        var matchSvc = new Mock<IMatchingService>();
        matchSvc.Setup(m => m.ConfirmMatchAsync("sv1", 3)).ReturnsAsync(true);

        var ctrl   = BuildController(user, matchSvc, MakeDb());
        var result = await ctrl.ConfirmMatch(3) as RedirectToActionResult;

        result.Should().NotBeNull();
        result!.ActionName.Should().Be("Dashboard");
    }

    [Fact]
    public async Task ConfirmMatch_CallsMatchingServiceExactlyOnce()
    {
        var user    = new ApplicationUser { Id = "sv1", Role = "Supervisor" };
        var matchSvc = new Mock<IMatchingService>();
        matchSvc.Setup(m => m.ConfirmMatchAsync(It.IsAny<string>(), It.IsAny<int>())).ReturnsAsync(true);

        var ctrl = BuildController(user, matchSvc, MakeDb());
        await ctrl.ConfirmMatch(7);

        matchSvc.Verify(m => m.ConfirmMatchAsync("sv1", 7), Times.Once);
    }

    [Fact]
    public async Task ConfirmMatch_SetsTempDataSuccess_OnSuccess()
    {
        var user    = new ApplicationUser { Id = "sv1", Role = "Supervisor" };
        var matchSvc = new Mock<IMatchingService>();
        matchSvc.Setup(m => m.ConfirmMatchAsync("sv1", 1)).ReturnsAsync(true);

        var ctrl = BuildController(user, matchSvc, MakeDb());
        await ctrl.ConfirmMatch(1);

        ctrl.TempData.ContainsKey("Success").Should().BeTrue();
    }
}

/// <summary>
/// Functional tests for StudentController actions.
/// </summary>
public class StudentControllerTests
{
    private static UserManager<ApplicationUser> MockUserManager(ApplicationUser user)
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        var mgr   = new Mock<UserManager<ApplicationUser>>(
            store.Object, null, null, null, null, null, null, null, null);
        mgr.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        return mgr.Object;
    }

    private static BlindMatchPAS.Data.ApplicationDbContext MakeDb(Action<BlindMatchPAS.Data.ApplicationDbContext>? seed = null)
    {
        var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<BlindMatchPAS.Data.ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new BlindMatchPAS.Data.ApplicationDbContext(options);
        seed?.Invoke(db);
        return db;
    }

    private static StudentController BuildController(
        ApplicationUser user,
        Mock<IMatchingService> matchSvc,
        BlindMatchPAS.Data.ApplicationDbContext db)
    {
        var ctrl = new StudentController(db, MockUserManager(user), matchSvc.Object);
        ctrl.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    new[] { new Claim(ClaimTypes.NameIdentifier, user.Id) }, "Test"))
            }
        };
        ctrl.TempData = new TempDataDictionary(
            ctrl.ControllerContext.HttpContext,
            Mock.Of<ITempDataProvider>());
        return ctrl;
    }

    [Fact]
    public async Task Submit_Post_RedirectsToDashboard_OnValidModel()
    {
        var user = new ApplicationUser { Id = "s1", Role = "Student" };
        var db   = MakeDb(d => d.ResearchAreas.Add(
            new ResearchArea { Id = 1, Name = "AI", Description = "", IsActive = true }));
        await db.SaveChangesAsync();

        var matchSvc = new Mock<IMatchingService>();
        var ctrl     = BuildController(user, matchSvc, db);

        var model = new SubmitProjectViewModel
        {
            Title = "My Project", Abstract = "Abstract text here.", TechStack = "C#", ResearchAreaId = 1
        };

        var result = await ctrl.Submit(model) as RedirectToActionResult;
        result.Should().NotBeNull();
        result!.ActionName.Should().Be("Dashboard");
    }

    [Fact]
    public async Task Withdraw_CallsMatchingService_WithCorrectArguments()
    {
        var user     = new ApplicationUser { Id = "s1", Role = "Student" };
        var matchSvc = new Mock<IMatchingService>();
        matchSvc.Setup(m => m.WithdrawProjectAsync("s1", 42)).ReturnsAsync(true);

        var ctrl = BuildController(user, matchSvc, MakeDb());
        await ctrl.Withdraw(42);

        matchSvc.Verify(m => m.WithdrawProjectAsync("s1", 42), Times.Once);
    }

    [Fact]
    public async Task Withdraw_SetsErrorTempData_WhenMatchingServiceReturnsFalse()
    {
        var user     = new ApplicationUser { Id = "s1", Role = "Student" };
        var matchSvc = new Mock<IMatchingService>();
        matchSvc.Setup(m => m.WithdrawProjectAsync(It.IsAny<string>(), It.IsAny<int>())).ReturnsAsync(false);

        var ctrl = BuildController(user, matchSvc, MakeDb());
        await ctrl.Withdraw(1);

        ctrl.TempData.ContainsKey("Error").Should().BeTrue();
    }
}
