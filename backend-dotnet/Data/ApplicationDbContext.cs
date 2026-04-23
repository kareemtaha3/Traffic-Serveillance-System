using Microsoft.EntityFrameworkCore;
using backend_dotnet.Models;

namespace backend_dotnet.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // هنا بنعرف الجداول اللي هتتكرت في Postgres
        public DbSet<Car> Cars { get; set; }
        public DbSet<Violation> Violations { get; set; }
        public DbSet<LicenseFee> LicenseFees { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // ممكن تضيف هنا أي إعدادات خاصة بالعلاقات بين الجداول
        }
    }
}