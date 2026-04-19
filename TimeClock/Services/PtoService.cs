using Microsoft.EntityFrameworkCore;
using TimeClock.Data;
using TimeClock.Models;

namespace TimeClock.Services;

public class PtoService
{
    private readonly ApplicationDbContext _db;

    public PtoService(ApplicationDbContext db) => _db = db;

    public async Task<PtoRequest> RequestPtoAsync(string userId, DateTime start, DateTime end, decimal hours, string? reason)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId)
            ?? throw new ArgumentException("User not found");

        if (hours > user.PtoBalanceHours)
            throw new InvalidOperationException($"Insufficient PTO balance. Available: {user.PtoBalanceHours}, requested: {hours}.");

        var req = new PtoRequest
        {
            UserId = userId,
            StartDate = start,
            EndDate = end,
            HoursRequested = hours,
            Reason = reason,
            Status = PtoRequestStatus.Pending
        };

        _db.PtoRequests.Add(req);
        await _db.SaveChangesAsync();
        return req;
    }

    public async Task ApproveAsync(int requestId, string approverId, string? notes = null)
    {
        var req = await _db.PtoRequests
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == requestId)
            ?? throw new ArgumentException("Request not found");

        if (req.Status != PtoRequestStatus.Pending)
            throw new InvalidOperationException($"Request already {req.Status}.");

        if (req.User == null)
            throw new InvalidOperationException("Request has no user.");

        if (req.HoursRequested > req.User.PtoBalanceHours)
            throw new InvalidOperationException("User PTO balance changed concurrently — insufficient hours available.");

        req.Status = PtoRequestStatus.Approved;
        req.DecidedByUserId = approverId;
        req.DecidedAt = DateTime.UtcNow;
        req.DecisionNotes = notes;

        req.User.PtoBalanceHours -= req.HoursRequested;

        _db.PtoTransactions.Add(new PtoTransaction
        {
            UserId = req.UserId,
            Hours = -req.HoursRequested,
            Description = $"PTO used: {req.StartDate:yyyy-MM-dd} to {req.EndDate:yyyy-MM-dd}",
            RelatedPtoRequestId = req.Id
        });

        await _db.SaveChangesAsync();
    }

    public async Task DenyAsync(int requestId, string approverId, string? notes = null)
    {
        var req = await _db.PtoRequests.FirstOrDefaultAsync(r => r.Id == requestId)
            ?? throw new ArgumentException("Request not found");

        if (req.Status != PtoRequestStatus.Pending)
            throw new InvalidOperationException($"Request already {req.Status}.");

        req.Status = PtoRequestStatus.Denied;
        req.DecidedByUserId = approverId;
        req.DecidedAt = DateTime.UtcNow;
        req.DecisionNotes = notes;
        await _db.SaveChangesAsync();
    }

    public async Task AccruePtoAsync(string userId, decimal hours, string description)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId)
            ?? throw new ArgumentException("User not found");

        user.PtoBalanceHours += hours;
        _db.PtoTransactions.Add(new PtoTransaction
        {
            UserId = userId,
            Hours = hours,
            Description = description
        });
        await _db.SaveChangesAsync();
    }

    public async Task<List<PtoTransaction>> GetHistoryAsync(string userId)
    {
        return await _db.PtoTransactions
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.TransactionDate)
            .ToListAsync();
    }
}
