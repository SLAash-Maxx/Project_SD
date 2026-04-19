using BlindMatchPAS.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BlindMatchPAS.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<ResearchArea>        ResearchAreas       { get; set; }
    public DbSet<Project>             Projects            { get; set; }
    public DbSet<SupervisorExpertise> SupervisorExpertise { get; set; }
    public DbSet<SupervisorInterest>  SupervisorInterests { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Project → Student (restrict delete)
        builder.Entity<Project>()
            .HasOne(p => p.Student)
            .WithMany(u => u.Projects)
            .HasForeignKey(p => p.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        // Project → Supervisor (nullable, no cascade)
        builder.Entity<Project>()
            .HasOne(p => p.Supervisor)
            .WithMany()
            .HasForeignKey(p => p.SupervisorId)
            .OnDelete(DeleteBehavior.SetNull);

        // Project → ResearchArea
        builder.Entity<Project>()
            .HasOne(p => p.ResearchArea)
            .WithMany(r => r.Projects)
            .HasForeignKey(p => p.ResearchAreaId)
            .OnDelete(DeleteBehavior.Restrict);

        // SupervisorExpertise → Supervisor
        builder.Entity<SupervisorExpertise>()
            .HasOne(e => e.Supervisor)
            .WithMany(u => u.Expertise)
            .HasForeignKey(e => e.SupervisorId)
            .OnDelete(DeleteBehavior.Cascade);

        // SupervisorInterest: unique constraint per supervisor+project
        builder.Entity<SupervisorInterest>()
            .HasIndex(i => new { i.SupervisorId, i.ProjectId })
            .IsUnique();

        // Seed initial research areas
        builder.Entity<ResearchArea>().HasData(
            new ResearchArea { Id = 1, Name = "Artificial Intelligence",  Description = "ML, Deep Learning, NLP", IsActive = true },
            new ResearchArea { Id = 2, Name = "Web Development",          Description = "Frontend, Backend, APIs",  IsActive = true },
            new ResearchArea { Id = 3, Name = "Cybersecurity",            Description = "Network security, pen testing, cryptography", IsActive = true },
            new ResearchArea { Id = 4, Name = "Cloud Computing",          Description = "AWS, Azure, GCP, DevOps",  IsActive = true },
            new ResearchArea { Id = 5, Name = "Mobile Development",       Description = "iOS, Android, cross-platform", IsActive = true },
            new ResearchArea { Id = 6, Name = "Data Science",             Description = "Analytics, visualisation, big data", IsActive = true }
        );
    }
}
