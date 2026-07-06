using Microsoft.EntityFrameworkCore;
using MyWorkTracker.ApiService.Models;

namespace MyWorkTracker.ApiService.Data;

public class TrackerDbContext : DbContext
{
    public TrackerDbContext(DbContextOptions<TrackerDbContext> options) : base(options) { }

    public DbSet<TrackedRepo> Repos => Set<TrackedRepo>();
    public DbSet<PullRequest> PullRequests => Set<PullRequest>();
    public DbSet<WorkItem> WorkItems => Set<WorkItem>();
    public DbSet<Epic> Epics => Set<Epic>();
    public DbSet<TrackedAgent> Agents => Set<TrackedAgent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WorkItem>()
            .HasOne<Epic>()
            .WithMany(e => e.WorkItems)
            .HasForeignKey(w => w.EpicId);

        modelBuilder.Entity<PullRequest>()
            .HasOne<TrackedRepo>()
            .WithMany()
            .HasForeignKey(p => p.RepoId);

        modelBuilder.Entity<WorkItem>()
            .HasOne<TrackedRepo>()
            .WithMany()
            .HasForeignKey(w => w.RepoId);
    }
}
