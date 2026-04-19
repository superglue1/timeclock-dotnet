using Microsoft.AspNetCore.Identity;

namespace TimeClock.Models;

public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;

    // Salaried employees: overtime is not calculated hourly
    public bool IsSalaried { get; set; }

    // IP address verification for clock in/out (can be toggled per user)
    public bool EnforceIpCheck { get; set; }
    public string? AllowedIpAddresses { get; set; } // comma-separated

    // Which pay period this user belongs to (Weekly, Bi-Weekly, etc.)
    public int? PayPeriodId { get; set; }
    public PayPeriod? PayPeriod { get; set; }

    // Current PTO balance in hours
    public decimal PtoBalanceHours { get; set; }
    public decimal PtoAccrualRatePerPeriod { get; set; } // How many PTO hours accrue per pay period

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    public string FullName => $"{FirstName} {LastName}".Trim();

    // Navigation properties
    public ICollection<TimeEntry> TimeEntries { get; set; } = new List<TimeEntry>();
    public ICollection<ScheduleEntry> ScheduleEntries { get; set; } = new List<ScheduleEntry>();
    public ICollection<PtoRequest> PtoRequests { get; set; } = new List<PtoRequest>();
}
