using Microsoft.EntityFrameworkCore;

namespace QiDian.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<NavigationMenu> NavigationMenus => Set<NavigationMenu>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<NavigationMenu>(b =>
            {
                b.HasKey(e => e.Id);
                b.Property(e => e.Title).IsRequired().HasMaxLength(100);
                b.Property(e => e.ViewKey).IsRequired().HasMaxLength(100);
                b.Property(e => e.Icon).HasMaxLength(10);
            });

        }
    }
}