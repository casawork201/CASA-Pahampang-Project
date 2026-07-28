using CASAPahampang.Models;
using Microsoft.EntityFrameworkCore;

namespace CASAPahampang.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : base(options)
    {
        
    }
    public DbSet<Team> Teams { get; set; }
    public DbSet<Match> Matches { get; set; }
    public DbSet<ChatMessage> ChatMessages { get; set; }
    public DbSet<AvatarOption> AvatarOptions { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }
}
