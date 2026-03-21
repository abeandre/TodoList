using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ToDo.DataAccess
{
    public class AppDbContext : DbContext
    {
        public AppDbContext() { }
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<ToDo> ToDos { get; set; } = null!;
        public DbSet<User> Users { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                // Fallback for contexts created without DI (e.g. design-time tooling).
                // Tests should always pass a unique DB name via DbContextOptions to avoid
                // sharing state between test runs.
                optionsBuilder
                    .UseInMemoryDatabase(Guid.NewGuid().ToString())
                    .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning));
            }
        }
    }
}
