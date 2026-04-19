using Microsoft.EntityFrameworkCore;
using TimeClock.Data;
using TimeClock.Models;

namespace TimeClock.Services;

public class PayrollWeekSummary
{
    public DateTime WeekStartUtc { get; set; }
    public DateTime WeekEndUtc { get; set; }
    public decimal RegularHours { get; set; }
    public decimal OvertimeHours { get; set; }
    public decimal PtoHours { get; set; }
    public decimal HolidayHours { get; set; }
    public decimal SickHours { get; set; }
    public decimal TotalHours => RegularHours + OvertimeHours + PtoHours + HolidayHours + SickHours;
    public List<TimeEntry> Entries { get; set; } = new();
}

public class PayrollReport
{
    public ApplicationUser User { get; set; } = null!;
    public DateTime PeriodStartUtc { get; set; }
    public DateTime PeriodEndUtc { get; set; }
    public List<PayrollWeekSummary> Weeks { get; set; } = new();

    public decimal TotalRegular => Weeks.Sum(w => w.RegularHours);
    public decimal TotalOvertime => Weeks.Sum(w => w.OvertimeHours);
    public decimal TotalPto => Weeks.Sum(w => w.PtoHours);
    public decimal TotalHolidayHours => Weeks.Sum(w => w.HolidayHours);
    public decimal TotalSickHours => Weeks.Sum(w => w.SickHours);
    public decimal GrandTotal => Weeks.Sum(w => w.TotalHours);
}

/// <summary>
/// Calculates payroll for a user over a given date range, with automatic overtime detection
/// per calendar week (US-style, Sunday-based).
/// </summary>
/// <remarks>
/// Business rules:
/// <list type="bullet">
///   <item>Salaried employees: all hours counted as Regular, no overtime.</item>
///   <item>Hourly employees: hours above the pay period's OvertimeThresholdHours (default 40)
///   become Overtime at the configured multiplier (default 1.5x).</item>
///   <item>PTO / Holiday / Sick are tracked separately and do NOT count toward the OT threshold.</item>
/// </list>
/// </remarks>
public class PayrollService
{
    private readonly ApplicationDbContext _db;

    public PayrollService(ApplicationDbContext db) => _db = db;

    /// <summary>
    /// Builds a full payroll report for the given user over [fromUtc, toUtc], grouped by week.
    /// </summary>
    /// <param name="userId">Target employee identifier.</param>
    /// <param name="fromUtc">Inclusive start of the reporting window (UTC).</param>
    /// <param name="toUtc">Inclusive end of the reporting window (UTC).</param>
    /// <returns>A <see cref="PayrollReport"/> with per-week breakdowns and aggregate totals.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="userId"/> does not exist.</exception>
    public async Task<PayrollReport> BuildReportAsync(string userId, DateTime fromUtc, DateTime toUtc)
    {
        var user = await _db.Users
            .Include(u => u.PayPeriod)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null) throw new ArgumentException("User not found", nameof(userId));

        var threshold = user.PayPeriod?.OvertimeThresholdHours ?? 40m;

        var entries = await _db.TimeEntries
            .Where(t => t.UserId == userId
                && t.ClockInUtc >= fromUtc
                && t.ClockInUtc <= toUtc
                && t.ClockOutUtc != null)
            .OrderBy(t => t.ClockInUtc)
            .ToListAsync();

        var report = new PayrollReport
        {
            User = user,
            PeriodStartUtc = fromUtc,
            PeriodEndUtc = toUtc
        };

        // Group time entries by calendar week (Sunday-based, US payroll standard)
        var weeks = entries
            .GroupBy(e => GetWeekStart(e.ClockInUtc))
            .OrderBy(g => g.Key);

        foreach (var week in weeks)
        {
            var weekSummary = new PayrollWeekSummary
            {
                WeekStartUtc = week.Key,
                WeekEndUtc = week.Key.AddDays(7),
                Entries = week.ToList()
            };

            decimal regular = 0, pto = 0, holiday = 0, sick = 0;
            foreach (var e in week)
            {
                var hours = e.TotalHours ?? e.CalculateHours();
                switch (e.EntryType)
                {
                    case TimeEntryType.Pto: pto += hours; break;
                    case TimeEntryType.Holiday: holiday += hours; break;
                    case TimeEntryType.Sick: sick += hours; break;
                    default: regular += hours; break;
                }
            }

            if (user.IsSalaried)
            {
                // Salaried employees: no overtime — all hours go to Regular
                weekSummary.RegularHours = regular;
                weekSummary.OvertimeHours = 0;
            }
            else
            {
                if (regular > threshold)
                {
                    weekSummary.RegularHours = threshold;
                    weekSummary.OvertimeHours = regular - threshold;
                }
                else
                {
                    weekSummary.RegularHours = regular;
                    weekSummary.OvertimeHours = 0;
                }
            }

            weekSummary.PtoHours = pto;
            weekSummary.HolidayHours = holiday;
            weekSummary.SickHours = sick;

            report.Weeks.Add(weekSummary);
        }

        return report;
    }

    // Week starts on Sunday at 00:00 UTC (standard US payroll convention)
    private static DateTime GetWeekStart(DateTime date)
    {
        var day = (int)date.DayOfWeek; // Sun=0, Sat=6
        return date.Date.AddDays(-day);
    }
}
