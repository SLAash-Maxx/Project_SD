using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814

namespace BlindMatchPAS.Migrations
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0058")]
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    Name = table.Column<string>(maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(nullable: true)
                },
                constraints: table => table.PrimaryKey("PK_AspNetRoles", x => x.Id));

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    FullName = table.Column<string>(maxLength: 100, nullable: false),
                    Role = table.Column<string>(nullable: false),
                    CreatedAt = table.Column<DateTime>(nullable: false),
                    UserName = table.Column<string>(maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(maxLength: 256, nullable: true),
                    Email = table.Column<string>(maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(nullable: false),
                    PasswordHash = table.Column<string>(nullable: true),
                    SecurityStamp = table.Column<string>(nullable: true),
                    ConcurrencyStamp = table.Column<string>(nullable: true),
                    PhoneNumber = table.Column<string>(nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(nullable: false),
                    TwoFactorEnabled = table.Column<bool>(nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(nullable: true),
                    LockoutEnabled = table.Column<bool>(nullable: false),
                    AccessFailedCount = table.Column<int>(nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_AspNetUsers", x => x.Id));

            migrationBuilder.CreateTable(
                name: "ResearchAreas",
                columns: table => new
                {
                    Id          = table.Column<int>(nullable: false).Annotation("Sqlite:Autoincrement", true),
                    Name        = table.Column<string>(maxLength: 100, nullable: false),
                    Description = table.Column<string>(maxLength: 300, nullable: false),
                    IsActive    = table.Column<bool>(nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_ResearchAreas", x => x.Id));

            // Identity junction tables
            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false).Annotation("Sqlite:Autoincrement", true),
                    RoleId = table.Column<string>(nullable: false),
                    ClaimType = table.Column<string>(nullable: true),
                    ClaimValue = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey("FK_AspNetRoleClaims_AspNetRoles_RoleId", x => x.RoleId,
                        "AspNetRoles", "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false).Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(nullable: false),
                    ClaimType = table.Column<string>(nullable: true),
                    ClaimValue = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey("FK_AspNetUserClaims_AspNetUsers_UserId", x => x.UserId,
                        "AspNetUsers", "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(nullable: false),
                    ProviderKey   = table.Column<string>(nullable: false),
                    ProviderDisplayName = table.Column<string>(nullable: true),
                    UserId = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey("FK_AspNetUserLogins_AspNetUsers_UserId", x => x.UserId,
                        "AspNetUsers", "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(nullable: false),
                    RoleId = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey("FK_AspNetUserRoles_AspNetRoles_RoleId", x => x.RoleId,
                        "AspNetRoles", "Id", onDelete: ReferentialAction.Cascade);
                    table.ForeignKey("FK_AspNetUserRoles_AspNetUsers_UserId", x => x.UserId,
                        "AspNetUsers", "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId        = table.Column<string>(nullable: false),
                    LoginProvider = table.Column<string>(nullable: false),
                    Name          = table.Column<string>(nullable: false),
                    Value         = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey("FK_AspNetUserTokens_AspNetUsers_UserId", x => x.UserId,
                        "AspNetUsers", "Id", onDelete: ReferentialAction.Cascade);
                });

            // Projects
            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    Id               = table.Column<int>(nullable: false).Annotation("Sqlite:Autoincrement", true),
                    Title            = table.Column<string>(maxLength: 200, nullable: false),
                    Abstract         = table.Column<string>(maxLength: 2000, nullable: false),
                    TechStack        = table.Column<string>(maxLength: 500, nullable: false),
                    ResearchAreaId   = table.Column<int>(nullable: false),
                    StudentId        = table.Column<string>(nullable: false),
                    SupervisorId     = table.Column<string>(nullable: true),
                    Status           = table.Column<int>(nullable: false),
                    IdentityRevealed = table.Column<bool>(nullable: false),
                    SubmittedAt      = table.Column<DateTime>(nullable: false),
                    MatchedAt        = table.Column<DateTime>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.Id);
                    table.ForeignKey("FK_Projects_ResearchAreas_ResearchAreaId", x => x.ResearchAreaId,
                        "ResearchAreas", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_Projects_AspNetUsers_StudentId", x => x.StudentId,
                        "AspNetUsers", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_Projects_AspNetUsers_SupervisorId", x => x.SupervisorId,
                        "AspNetUsers", "Id", onDelete: ReferentialAction.SetNull);
                });

            // Supervisor Expertise
            migrationBuilder.CreateTable(
                name: "SupervisorExpertise",
                columns: table => new
                {
                    Id             = table.Column<int>(nullable: false).Annotation("Sqlite:Autoincrement", true),
                    SupervisorId   = table.Column<string>(nullable: false),
                    ResearchAreaId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupervisorExpertise", x => x.Id);
                    table.ForeignKey("FK_SupervisorExpertise_AspNetUsers_SupervisorId", x => x.SupervisorId,
                        "AspNetUsers", "Id", onDelete: ReferentialAction.Cascade);
                    table.ForeignKey("FK_SupervisorExpertise_ResearchAreas_ResearchAreaId", x => x.ResearchAreaId,
                        "ResearchAreas", "Id", onDelete: ReferentialAction.Cascade);
                });

            // Supervisor Interests
            migrationBuilder.CreateTable(
                name: "SupervisorInterests",
                columns: table => new
                {
                    Id           = table.Column<int>(nullable: false).Annotation("Sqlite:Autoincrement", true),
                    ProjectId    = table.Column<int>(nullable: false),
                    SupervisorId = table.Column<string>(nullable: false),
                    ExpressedAt  = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupervisorInterests", x => x.Id);
                    table.ForeignKey("FK_SupervisorInterests_Projects_ProjectId", x => x.ProjectId,
                        "Projects", "Id", onDelete: ReferentialAction.Cascade);
                    table.ForeignKey("FK_SupervisorInterests_AspNetUsers_SupervisorId", x => x.SupervisorId,
                        "AspNetUsers", "Id", onDelete: ReferentialAction.Cascade);
                });

            // Unique index on SupervisorInterests
            migrationBuilder.CreateIndex("IX_SupervisorInterests_SupervisorId_ProjectId",
                "SupervisorInterests", new[] { "SupervisorId", "ProjectId" }, unique: true);

            // Seed research areas
            migrationBuilder.InsertData("ResearchAreas",
                new[] { "Id", "Name", "Description", "IsActive" },
                new object[,]
                {
                    { 1, "Artificial Intelligence",  "ML, Deep Learning, NLP",                 true },
                    { 2, "Web Development",           "Frontend, Backend, APIs",                 true },
                    { 3, "Cybersecurity",             "Network security, pen testing, cryptography", true },
                    { 4, "Cloud Computing",           "AWS, Azure, GCP, DevOps",                true },
                    { 5, "Mobile Development",        "iOS, Android, cross-platform",           true },
                    { 6, "Data Science",              "Analytics, visualisation, big data",     true }
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable("SupervisorInterests");
            migrationBuilder.DropTable("SupervisorExpertise");
            migrationBuilder.DropTable("Projects");
            migrationBuilder.DropTable("AspNetUserTokens");
            migrationBuilder.DropTable("AspNetUserRoles");
            migrationBuilder.DropTable("AspNetUserLogins");
            migrationBuilder.DropTable("AspNetUserClaims");
            migrationBuilder.DropTable("AspNetRoleClaims");
            migrationBuilder.DropTable("ResearchAreas");
            migrationBuilder.DropTable("AspNetRoles");
            migrationBuilder.DropTable("AspNetUsers");
        }
    }
}
