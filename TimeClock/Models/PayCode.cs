namespace TimeClock.Models;

public class PayCode
{
    public int Id { get; set; }

    // Short code used on paychecks (e.g. "C", "D", "E")
    public string Code { get; set; } = string.Empty;

    // Human-readable description (e.g. Regular Hours, Overtime, PTO)
    public string Description { get; set; } = string.Empty;

    // Rate multiplier (1.0 for regular, 1.5 for overtime)
    public decimal Multiplier { get; set; } = 1.0m;

    public bool IsActive { get; set; } = true;
}
