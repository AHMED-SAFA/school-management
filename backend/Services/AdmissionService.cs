using Microsoft.EntityFrameworkCore;
using backend.Data;
using backend.Models;

namespace backend.Services;

public class AdmissionService
{
    private readonly AppDbContext _context;
    private readonly int _holdDurationSeconds;
    private static readonly SemaphoreSlim _inMemoryLock = new(1, 1);

    public AdmissionService(AppDbContext context, IConfiguration configuration)
    {
        _context = context;
        var holdDuration = configuration["HOLD_DURATION_SECONDS"]
            ?? Environment.GetEnvironmentVariable("HOLD_DURATION_SECONDS");
        _holdDurationSeconds = string.IsNullOrEmpty(holdDuration) ? 600 : int.Parse(holdDuration);
    }

    public async Task<CreateClassResponse> CreateClassAsync(CreateClassRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Class name is required.");
        if (request.TotalSeats <= 0)
            throw new ArgumentException("Total seats must be greater than zero.");

        var schoolClass = new SchoolClass
        {
            Id = Guid.NewGuid().ToString(),
            Name = request.Name.Trim(),
            TotalSeats = request.TotalSeats
        };

        _context.Classes.Add(schoolClass);
        await _context.SaveChangesAsync();

        return new CreateClassResponse
        {
            Id = schoolClass.Id,
            Name = schoolClass.Name,
            TotalSeats = schoolClass.TotalSeats
        };
    }

    public async Task<List<ClassResponse>> GetAllClassesAsync()
    {
        var now = DateTime.UtcNow;
        await ExpireHoldsAsync(now);

        var classes = await _context.Classes.ToListAsync();
        var result = new List<ClassResponse>();

        foreach (var c in classes)
        {
            var seatInfo = await GetSeatInfoAsync(c.Id);
            result.Add(new ClassResponse
            {
                Id = c.Id,
                Name = c.Name,
                TotalSeats = c.TotalSeats,
                HeldSeats = seatInfo.held,
                ConfirmedSeats = seatInfo.confirmed,
                AvailableSeats = c.TotalSeats - seatInfo.held - seatInfo.confirmed
            });
        }

        return result;
    }

    public async Task<ClassResponse?> GetClassAsync(string classId)
    {
        var now = DateTime.UtcNow;
        await ExpireHoldsAsync(now);

        var schoolClass = await _context.Classes.FindAsync(classId);
        if (schoolClass == null) return null;

        var seatInfo = await GetSeatInfoAsync(classId);

        return new ClassResponse
        {
            Id = schoolClass.Id,
            Name = schoolClass.Name,
            TotalSeats = schoolClass.TotalSeats,
            HeldSeats = seatInfo.held,
            ConfirmedSeats = seatInfo.confirmed,
            AvailableSeats = schoolClass.TotalSeats - seatInfo.held - seatInfo.confirmed
        };
    }

    public async Task<HoldResponse> CreateHoldAsync(string classId, string studentId)
    {
        if (string.IsNullOrWhiteSpace(studentId))
            throw new ArgumentException("Student ID (X-Student-Id header) is required.");

        var now = DateTime.UtcNow;

        if (_context.Database.IsRelational())
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable);

            try
            {
                await ExpireHoldsAsync(now);

                SchoolClass? schoolClass;
                if (_context.Database.IsNpgsql())
                {
                    schoolClass = await _context.Classes
                        .FromSqlInterpolated($"SELECT * FROM classes WHERE id = {classId} FOR UPDATE")
                        .FirstOrDefaultAsync();
                }
                else
                {
                    schoolClass = await _context.Classes.FindAsync(classId);
                }

                if (schoolClass == null)
                    throw new KeyNotFoundException("Class not found.");

                var existingActive = await _context.Holds
                    .AnyAsync(h => h.ClassId == classId
                        && h.StudentId == studentId
                        && (h.Status == HoldStatus.HELD || h.Status == HoldStatus.CONFIRMED));

                if (existingActive)
                    throw new InvalidOperationException("Student already has an active application or confirmed admission for this class.");

                var heldCount = await _context.Holds
                    .CountAsync(h => h.ClassId == classId && h.Status == HoldStatus.HELD);
                var confirmedCount = await _context.Holds
                    .CountAsync(h => h.ClassId == classId && h.Status == HoldStatus.CONFIRMED);

                if (heldCount + confirmedCount >= schoolClass.TotalSeats)
                    throw new SeatUnavailableException("No seats available.");

                var hold = new Hold
                {
                    HoldId = Guid.NewGuid().ToString(),
                    ClassId = classId,
                    StudentId = studentId,
                    Status = HoldStatus.HELD,
                    ExpiresAt = now.AddSeconds(_holdDurationSeconds),
                    CreatedAt = now
                };

                _context.Holds.Add(hold);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return new HoldResponse
                {
                    HoldId = hold.HoldId,
                    ClassId = hold.ClassId,
                    StudentId = hold.StudentId,
                    Status = hold.Status.ToString(),
                    ExpiresAt = hold.ExpiresAt
                };
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        else
        {
            // In-Memory concurrency lock
            await _inMemoryLock.WaitAsync();
            try
            {
                await ExpireHoldsAsync(now);

                var schoolClass = await _context.Classes.FindAsync(classId);
                if (schoolClass == null)
                    throw new KeyNotFoundException("Class not found.");

                var existingActive = await _context.Holds
                    .AnyAsync(h => h.ClassId == classId
                        && h.StudentId == studentId
                        && (h.Status == HoldStatus.HELD || h.Status == HoldStatus.CONFIRMED));

                if (existingActive)
                    throw new InvalidOperationException("Student already has an active application or confirmed admission for this class.");

                var heldCount = await _context.Holds
                    .CountAsync(h => h.ClassId == classId && h.Status == HoldStatus.HELD);
                var confirmedCount = await _context.Holds
                    .CountAsync(h => h.ClassId == classId && h.Status == HoldStatus.CONFIRMED);

                if (heldCount + confirmedCount >= schoolClass.TotalSeats)
                    throw new SeatUnavailableException("No seats available.");

                var hold = new Hold
                {
                    HoldId = Guid.NewGuid().ToString(),
                    ClassId = classId,
                    StudentId = studentId,
                    Status = HoldStatus.HELD,
                    ExpiresAt = now.AddSeconds(_holdDurationSeconds),
                    CreatedAt = now
                };

                _context.Holds.Add(hold);
                await _context.SaveChangesAsync();

                return new HoldResponse
                {
                    HoldId = hold.HoldId,
                    ClassId = hold.ClassId,
                    StudentId = hold.StudentId,
                    Status = hold.Status.ToString(),
                    ExpiresAt = hold.ExpiresAt
                };
            }
            finally
            {
                _inMemoryLock.Release();
            }
        }
    }

    public async Task<HoldResponse?> GetHoldAsync(string holdId)
    {
        var now = DateTime.UtcNow;
        await ExpireHoldsAsync(now);

        var hold = await _context.Holds.FindAsync(holdId);
        if (hold == null) return null;

        if (hold.Status == HoldStatus.HELD && hold.ExpiresAt <= now)
        {
            hold.Status = HoldStatus.EXPIRED;
            await _context.SaveChangesAsync();
        }

        return new HoldResponse
        {
            HoldId = hold.HoldId,
            ClassId = hold.ClassId,
            StudentId = hold.StudentId,
            Status = hold.Status.ToString(),
            ExpiresAt = hold.ExpiresAt
        };
    }

    public async Task<ConfirmResponse> ConfirmHoldAsync(string holdId, string studentId)
    {
        if (string.IsNullOrWhiteSpace(studentId))
            throw new ArgumentException("Student ID (X-Student-Id header) is required.");

        var now = DateTime.UtcNow;

        if (_context.Database.IsRelational())
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable);

            try
            {
                Hold? hold;
                if (_context.Database.IsNpgsql())
                {
                    hold = await _context.Holds
                        .FromSqlInterpolated($"SELECT * FROM holds WHERE id = {holdId} FOR UPDATE")
                        .FirstOrDefaultAsync();
                }
                else
                {
                    hold = await _context.Holds.FindAsync(holdId);
                }

                if (hold == null)
                    throw new KeyNotFoundException("Hold not found.");

                if (hold.StudentId != studentId)
                    throw new UnauthorizedAccessException("You can only confirm your own hold.");

                if (hold.ExpiresAt <= now && hold.Status == HoldStatus.HELD)
                {
                    hold.Status = HoldStatus.EXPIRED;
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    throw new InvalidOperationException("Hold has expired.");
                }

                if (hold.Status == HoldStatus.CONFIRMED)
                {
                    await transaction.CommitAsync();
                    return new ConfirmResponse
                    {
                        HoldId = hold.HoldId,
                        Status = "CONFIRMED"
                    };
                }

                if (hold.Status != HoldStatus.HELD)
                    throw new InvalidOperationException($"Cannot confirm a hold with status {hold.Status}.");

                hold.Status = HoldStatus.CONFIRMED;
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return new ConfirmResponse
                {
                    HoldId = hold.HoldId,
                    Status = "CONFIRMED"
                };
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        else
        {
            await _inMemoryLock.WaitAsync();
            try
            {
                var hold = await _context.Holds.FindAsync(holdId);

                if (hold == null)
                    throw new KeyNotFoundException("Hold not found.");

                if (hold.StudentId != studentId)
                    throw new UnauthorizedAccessException("You can only confirm your own hold.");

                if (hold.ExpiresAt <= now && hold.Status == HoldStatus.HELD)
                {
                    hold.Status = HoldStatus.EXPIRED;
                    await _context.SaveChangesAsync();
                    throw new InvalidOperationException("Hold has expired.");
                }

                if (hold.Status == HoldStatus.CONFIRMED)
                {
                    return new ConfirmResponse
                    {
                        HoldId = hold.HoldId,
                        Status = "CONFIRMED"
                    };
                }

                if (hold.Status != HoldStatus.HELD)
                    throw new InvalidOperationException($"Cannot confirm a hold with status {hold.Status}.");

                hold.Status = HoldStatus.CONFIRMED;
                await _context.SaveChangesAsync();

                return new ConfirmResponse
                {
                    HoldId = hold.HoldId,
                    Status = "CONFIRMED"
                };
            }
            finally
            {
                _inMemoryLock.Release();
            }
        }
    }

    public async Task CancelHoldAsync(string holdId, string studentId)
    {
        if (string.IsNullOrWhiteSpace(studentId))
            throw new ArgumentException("Student ID (X-Student-Id header) is required.");

        var hold = await _context.Holds.FindAsync(holdId);
        if (hold == null)
            throw new KeyNotFoundException("Hold not found.");

        if (hold.StudentId != studentId)
            throw new UnauthorizedAccessException("You can only cancel your own hold.");

        if (hold.Status == HoldStatus.CONFIRMED)
            throw new InvalidOperationException("Cannot cancel a confirmed admission.");

        if (hold.Status == HoldStatus.CANCELLED)
            return;

        hold.Status = HoldStatus.CANCELLED;
        await _context.SaveChangesAsync();
    }

    public async Task<List<MyHoldResponse>> GetMyHoldsAsync(string studentId)
    {
        if (string.IsNullOrWhiteSpace(studentId))
            throw new ArgumentException("Student ID (X-Student-Id header) is required.");

        var now = DateTime.UtcNow;
        await ExpireHoldsAsync(now);

        var holds = await _context.Holds
            .Include(h => h.SchoolClass)
            .Where(h => h.StudentId == studentId)
            .OrderByDescending(h => h.CreatedAt)
            .ToListAsync();

        foreach (var h in holds)
        {
            if (h.Status == HoldStatus.HELD && h.ExpiresAt <= now)
            {
                h.Status = HoldStatus.EXPIRED;
            }
        }
        await _context.SaveChangesAsync();

        return holds.Select(h => new MyHoldResponse
        {
            HoldId = h.HoldId,
            ClassId = h.ClassId,
            ClassName = h.SchoolClass?.Name ?? "",
            Status = h.Status.ToString(),
            ExpiresAt = h.ExpiresAt
        }).ToList();
    }

    private async Task ExpireHoldsAsync(DateTime now)
    {
        if (_context.Database.IsRelational())
        {
            await _context.Holds
                .Where(h => h.Status == HoldStatus.HELD && h.ExpiresAt <= now)
                .ExecuteUpdateAsync(s => s.SetProperty(h => h.Status, HoldStatus.EXPIRED));
        }
        else
        {
            var expired = await _context.Holds
                .Where(h => h.Status == HoldStatus.HELD && h.ExpiresAt <= now)
                .ToListAsync();
            foreach (var h in expired)
            {
                h.Status = HoldStatus.EXPIRED;
            }
            if (expired.Any())
            {
                await _context.SaveChangesAsync();
            }
        }
    }

    private async Task<(int held, int confirmed)> GetSeatInfoAsync(string classId)
    {
        var held = await _context.Holds
            .CountAsync(h => h.ClassId == classId && h.Status == HoldStatus.HELD);
        var confirmed = await _context.Holds
            .CountAsync(h => h.ClassId == classId && h.Status == HoldStatus.CONFIRMED);
        return (held, confirmed);
    }
}

public class SeatUnavailableException : Exception
{
    public SeatUnavailableException(string message) : base(message) { }
}
