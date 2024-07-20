using Microsoft.EntityFrameworkCore;
using TimeTrackerAPI.Models;

namespace TimeTrackerAPI.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Specify schema for the entities
            modelBuilder.Entity<User>().ToTable("Users", schema: "timetracker");

            // Additional configurations for other entities, relationships, etc.

            base.OnModelCreating(modelBuilder);
        }
    }
}
