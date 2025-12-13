using Microsoft.EntityFrameworkCore;
using TaseDreams.Api.Models;

namespace TaseDreams.Api.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Project> Projects { get; set; }
    public DbSet<ProjectLane> ProjectLanes { get; set; }
    public DbSet<InfrastructureService> InfrastructureServices { get; set; }
    public DbSet<ProjectInfrastructureService> ProjectInfrastructureServices { get; set; }
    public DbSet<NodePortAllocation> NodePortAllocations { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Project>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(12);
            entity.Property(e => e.BitbucketProjectKey).IsRequired();
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasIndex(e => e.BitbucketProjectKey).IsUnique();
        });

        modelBuilder.Entity<ProjectLane>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired();
            entity.Property(e => e.BackendLanguage).IsRequired();
            entity.Property(e => e.FrontendFramework).IsRequired();
        });

        modelBuilder.Entity<InfrastructureService>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired();
            entity.Property(e => e.Type).IsRequired();
        });

        modelBuilder.Entity<ProjectInfrastructureService>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Project)
                  .WithMany(p => p.InfrastructureServices)
                  .HasForeignKey(e => e.ProjectId);
            entity.HasOne(e => e.InfrastructureService)
                  .WithMany()
                  .HasForeignKey(e => e.InfrastructureServiceId);
        });

        modelBuilder.Entity<NodePortAllocation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Project)
                  .WithMany()
                  .HasForeignKey(e => e.ProjectId);
            entity.HasIndex(e => e.Port).IsUnique();
        });

        // Seed data
        SeedData(modelBuilder);
    }

    private void SeedData(ModelBuilder modelBuilder)
    {
        // Project Lanes
        modelBuilder.Entity<ProjectLane>().HasData(
            new ProjectLane { Id = 1, Name = "DotNet Angular", BackendLanguage = "dotnet", FrontendFramework = "angular", IsActive = true },
            new ProjectLane { Id = 2, Name = "Java React", BackendLanguage = "java", FrontendFramework = "react", IsActive = true }
        );

        // Infrastructure Services
        modelBuilder.Entity<InfrastructureService>().HasData(
            new InfrastructureService { Id = 1, Name = "RabbitMQ", Type = "rabbitmq", IsActive = true },
            new InfrastructureService { Id = 2, Name = "Redis", Type = "redis", IsActive = true },
            new InfrastructureService { Id = 3, Name = "MinIO", Type = "minio", IsActive = true }
        );
    }
}

