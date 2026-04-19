using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TimeClock.Models;

namespace TimeClock.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<TimeEntry> TimeEntries => Set<TimeEntry>();
    public DbSet<PayPeriod> PayPeriods => Set<PayPeriod>();
    public DbSet<ScheduleEntry> ScheduleEntries => Set<ScheduleEntry>();
    public DbSet<PtoRequest> PtoRequests => Set<PtoRequest>();
    public DbSet<PtoTransaction> PtoTransactions => Set<PtoTransaction>();
    public DbSet<PayCode> PayCodes => Set<PayCode>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(e =>
        {
            e.Property(u => u.FirstName).HasMaxLength(100);
            e.Property(u => u.LastName).HasMaxLength(100);
            e.Property(u => u.PtoBalanceHours).HasPrecision(10, 2);
            e.Property(u => u.PtoAccrualRatePerPeriod).HasPrecision(10, 2);
            e.HasOne(u => u.PayPeriod)
                .WithMany(p => p.Users)
                .HasForeignKey(u => u.PayPeriodId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<TimeEntry>(e =>
        {
            e.Property(t => t.TotalHours).HasPrecision(10, 2);
            e.Property(t => t.PayCode).HasMaxLength(10);
            e.HasOne(t => t.User)
                .WithMany(u => u.TimeEntries)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(t => new { t.UserId, t.ClockInUtc });
        });

        builder.Entity<ScheduleEntry>(e =>
        {
            e.HasOne(s => s.User)
                .WithMany(u => u.ScheduleEntries)
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(s => new { s.UserId, s.ShiftStartUtc });
        });

        builder.Entity<PtoRequest>(e =>
        {
            e.Property(p => p.HoursRequested).HasPrecision(10, 2);
            e.HasOne(p => p.User)
                .WithMany(u => u.PtoRequests)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<PtoTransaction>(e =>
        {
            e.Property(p => p.Hours).HasPrecision(10, 2);
            e.HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<PayPeriod>(e =>
        {
            e.Property(p => p.Name).HasMaxLength(100);
            e.Property(p => p.OvertimeThresholdHours).HasPrecision(10, 2);
            e.Property(p => p.OvertimeMultiplier).HasPrecision(5, 2);
        });

        builder.Entity<PayCode>(e =>
        {
            e.Property(p => p.Code).HasMaxLength(10);
            e.Property(p => p.Description).HasMaxLength(200);
            e.Property(p => p.Multiplier).HasPrecision(5, 2);
            e.HasIndex(p => p.Code).IsUnique();
        });
    }
}
