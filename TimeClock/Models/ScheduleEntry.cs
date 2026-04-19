namespace TimeClock.Models;

public class ScheduleEntry
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    public DateTime ShiftStartUtc { get; set; }
    public DateTime ShiftEndUtc { get; set; }

    // Clock-in window: how many minutes before shift start the employee can punch in
    public int ClockInWindowMinutesBefore { get; set; } = 15;
    public int ClockInWindowMinutesAfter { get; set; } = 15;

    // Whether to enforce the clock-in window for this specific shift
    public bool EnforceClockInWindow { get; set; } = true;

    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedByUserId { get; set; }
}
