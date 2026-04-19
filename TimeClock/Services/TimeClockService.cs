using Microsoft.EntityFrameworkCore;
using TimeClock.Data;
using TimeClock.Models;

namespace TimeClock.Services;

public class ClockResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public TimeEntry? Entry { get; set; }
}

public class TimeClockService
{
    private readonly ApplicationDbContext _db;

    public TimeClockService(ApplicationDbContext db) => _db = db;

    public async Task<TimeEntry?> GetOpenEntryAsync(string userId)
    {
        return await _db.TimeEntries
            .Where(t => t.UserId == userId && t.ClockOutUtc == null)
            .OrderByDescending(t => t.ClockInUtc)
            .FirstOrDefaultAsync();
    }

    public async Task<ClockResult> ClockInAsync(string userId, string? ipAddress)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null || !user.IsActive)
            return new ClockResult { Success = false, Message = "User not found or inactive." };

        var open = await GetOpenEntryAsync(userId);
        if (open != null)
            return new ClockResult { Success = false, Message = "You already have an open shift. Clock out first.", Entry = open };

        // IP whitelist check
        if (user.EnforceIpCheck && !string.IsNullOrWhiteSpace(user.AllowedIpAddresses))
        {
            var allowed = user.AllowedIpAddresses.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (string.IsNullOrEmpty(ipAddress) || !allowed.Contains(ipAddress))
                return new ClockResult { Success = false, Message = $"IP address {ipAddress ?? "unknown"} is not allowed for clock-in." };
        }

        // Schedule window check
        var now = DateTime.UtcNow;
        var windowFrom = now.AddHours(-12);
        var windowTo = now.AddHours(12);

        // EF Core with SQLite cannot translate Math.Abs(TimeSpan.Ticks) into SQL,
        // so we fetch candidates into memory first and then sort by proximity
        var candidates = await _db.ScheduleEntries
            .Where(s => s.UserId == userId && s.EnforceClockInWindow)
            .Where(s => s.ShiftStartUtc >= windowFrom && s.ShiftStartUtc <= windowTo)
            .ToListAsync();

        var upcomingShift = candidates
            .OrderBy(s => Math.Abs((s.ShiftStartUtc - now).Ticks))
            .FirstOrDefault();

        if (upcomingShift != null)
        {
            var windowStart = upcomingShift.ShiftStartUtc.AddMinutes(-upcomingShift.ClockInWindowMinutesBefore);
            var windowEnd = upcomingShift.ShiftStartUtc.AddMinutes(upcomingShift.ClockInWindowMinutesAfter);

            if (now < windowStart || now > windowEnd)
            {
                return new ClockResult
                {
                    Success = false,
                    Message = $"Outside clock-in window. Shift starts at {upcomingShift.ShiftStartUtc:yyyy-MM-dd HH:mm} UTC. Allowed window: {windowStart:HH:mm}–{windowEnd:HH:mm}."
                };
            }
        }

        var entry = new TimeEntry
        {
            UserId = userId,
            ClockInUtc = now,
            ClockInIpAddress = ipAddress,
            EntryType = TimeEntryType.Regular,
            PayCode = "C"
        };

        _db.TimeEntries.Add(entry);
        await _db.SaveChangesAsync();

        return new ClockResult { Success = true, Message = "Clocked in successfully.", Entry = entry };
    }

    public async Task<ClockResult> ClockOutAsync(string userId, string? ipAddress)
    {
        var open = await GetOpenEntryAsync(userId);
        if (open == null)
            return new ClockResult { Success = false, Message = "No open shift to clock out." };

        open.ClockOutUtc = DateTime.UtcNow;
        open.ClockOutIpAddress = ipAddress;
        open.TotalHours = open.CalculateHours();

        await _db.SaveChangesAsync();

        return new ClockResult { Success = true, Message = $"Clocked out. Hours: {open.TotalHours:F2}", Entry = open };
    }

    public async Task<List<TimeEntry>> GetEntriesForUserAsync(string userId, DateTime fromUtc, DateTime toUtc)
    {
        return await _db.TimeEntries
            .Where(t => t.UserId == userId && t.ClockInUtc >= fromUtc && t.ClockInUtc <= toUtc)
            .OrderByDescending(t => t.ClockInUtc)
            .ToListAsync();
    }
}
