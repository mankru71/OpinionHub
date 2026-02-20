using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OpinionHub.Web.Models;

namespace OpinionHub.Web.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Poll> Polls => Set<Poll>();
    public DbSet<PollOption> PollOptions => Set<PollOption>();
    public DbSet<Vote> Votes => Set<Vote>();
    public DbSet<VoteSelection> VoteSelections => Set<VoteSelection>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<VoteSelection>().HasKey(vs => new { vs.VoteId, vs.PollOptionId });

        builder.Entity<Poll>()
            .HasMany(p => p.Options)
            .WithOne(o => o.Poll)
            .HasForeignKey(o => o.PollId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Vote>()
            .HasMany(v => v.Selections)
            .WithOne(s => s.Vote)
            .HasForeignKey(s => s.VoteId);

        builder.Entity<Poll>()
            .HasIndex(p => new { p.Status, p.EndDateUtc });

        builder.Entity<Vote>()
            .Property(v => v.VoterAccountId)
            .IsRequired();

        builder.Entity<Vote>()
            .HasIndex(v => new { v.PollId, v.VoterAccountId })
            .HasIndex(v => new { v.PollId, v.UserId })
            .IsUnique();
    }
}
