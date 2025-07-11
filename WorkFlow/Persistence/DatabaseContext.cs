using Microsoft.EntityFrameworkCore;
using WorkFlow.Models;

namespace WorkFlow.Persistence
{
    public class DatabaseContext : DbContext
    {
        public DbSet<Person> People { get; set; }
        public DbSet<WorkHistory> WorkHistories { get; set; }
        public DbSet<Employer> Employers { get; set; }
        public DbSet<Announcement> Announcements { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseInMemoryDatabase(databaseName: "Workflow");
        }
    }
}
