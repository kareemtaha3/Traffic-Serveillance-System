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

        public DbSet<Car> Cars { get; set; }
        public DbSet<Violation> Violations { get; set; }
        public DbSet<LicenseFee> LicenseFees { get; set; }
        public DbSet<BusRoute> BusRoutes { get; set; }
        public DbSet<Camera> Cameras { get; set; }
        public DbSet<Checkpoint> Checkpoints { get; set; }
        public DbSet<VehicleRoute> VehicleRoutes { get; set; }
        public DbSet<CheckpointLog> CheckpointLogs { get; set; }
        public DbSet<Infringement> Infringements { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Camera>()
                .HasIndex(c => c.CameraIdentifier)
                .IsUnique();

            modelBuilder.Entity<Checkpoint>()
                .HasOne(c => c.BusRoute)
                .WithMany(r => r.Checkpoints)
                .HasForeignKey(c => c.BusRouteId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Checkpoint>()
                .HasOne(c => c.Camera)
                .WithMany(cam => cam.RouteCheckpoints)
                .HasForeignKey(c => c.CameraId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Checkpoint>()
                .HasIndex(c => new { c.BusRouteId, c.SequenceOrder })
                .IsUnique();

            modelBuilder.Entity<VehicleRoute>()
                .HasOne(vr => vr.Car)
                .WithMany(c => c.VehicleRoutes)
                .HasForeignKey(vr => vr.CarId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<VehicleRoute>()
                .HasOne(vr => vr.BusRoute)
                .WithMany(r => r.VehicleRoutes)
                .HasForeignKey(vr => vr.BusRouteId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CheckpointLog>()
                .HasOne(cl => cl.Car)
                .WithMany(c => c.CheckpointLogs)
                .HasForeignKey(cl => cl.CarId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CheckpointLog>()
                .HasOne(cl => cl.Checkpoint)
                .WithMany(cp => cp.CheckpointLogs)
                .HasForeignKey(cl => cl.CheckpointId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Infringement>()
                .HasOne(i => i.Car)
                .WithMany(c => c.Infringements)
                .HasForeignKey(i => i.CarId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Infringement>()
                .HasOne(i => i.ExpectedCheckpoint)
                .WithMany()
                .HasForeignKey(i => i.ExpectedCheckpointId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Infringement>()
                .HasOne(i => i.ActualCheckpoint)
                .WithMany()
                .HasForeignKey(i => i.ActualCheckpointId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}