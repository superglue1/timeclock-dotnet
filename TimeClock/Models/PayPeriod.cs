namespace TimeClock.Models;

public enum PayPeriodType
{
    Weekly = 0,
    BiWeekly = 1,
    SemiMonthly = 2,
    Monthly = 3
}

public class PayPeriod
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public PayPeriodType PeriodType { get; set; }

    // Reference date from which pay periods are calculated forward
    public DateTime AnchorDate { get; set; }

    // Overtime threshold (typically 40 hours/week in the US)
    public decimal OvertimeThresholdHours { get; set; } = 40m;

    // Multiplier applied to overtime hours (1.5x is the US standard)
    public decimal OvertimeMultiplier { get; set; } = 1.5m;

    public bool IsActive { get; set; } = true;

    public ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
}
