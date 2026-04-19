namespace TimeClock.Models;

public enum PtoRequestStatus
{
    Pending = 0,
    Approved = 1,
    Denied = 2,
    Cancelled = 3
}

public class PtoRequest
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal HoursRequested { get; set; }

    public PtoRequestStatus Status { get; set; } = PtoRequestStatus.Pending;

    public string? Reason { get; set; }
    public string? DecisionNotes { get; set; }
    public string? DecidedByUserId { get; set; }
    public DateTime? DecidedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

// Audit log of PTO balance changes (accruals and deductions)
public class PtoTransaction
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    // Positive = accrual, negative = usage
    public decimal Hours { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
    public int? RelatedPtoRequestId { get; set; }
}
