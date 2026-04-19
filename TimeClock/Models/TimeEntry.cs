namespace TimeClock.Models;

public enum TimeEntryType
{
    Regular = 0,
    Overtime = 1,
    Pto = 2,
    Holiday = 3,
    Sick = 4
}

public class TimeEntry
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    public DateTime ClockInUtc { get; set; }
    public DateTime? ClockOutUtc { get; set; }

    public string? ClockInIpAddress { get; set; }
    public string? ClockOutIpAddress { get; set; }

    public TimeEntryType EntryType { get; set; } = TimeEntryType.Regular;

    // Pay code — customizable. Examples: "C" = Regular Hours, "D" = Overtime, "E" = PTO
    public string? PayCode { get; set; }

    public string? Notes { get; set; }

    // True if this entry was manually adjusted by a manager (for audit)
    public bool IsManuallyAdjusted { get; set; }
    public string? AdjustedByUserId { get; set; }

    // Pre-computed hours for fast access in reports
    public decimal? TotalHours { get; set; }

    public bool IsOpen => ClockOutUtc == null;

    public decimal CalculateHours()
    {
        if (ClockOutUtc == null) return 0;
        return (decimal)(ClockOutUtc.Value - ClockInUtc).TotalHours;
    }
}
