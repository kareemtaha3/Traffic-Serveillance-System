using backend_dotnet.Data;
using backend_dotnet.DTO;
using backend_dotnet.Models;
using Microsoft.EntityFrameworkCore;

namespace backend_dotnet.Services;

public interface ITrafficService
{
    Task<(bool IsSuccess, string Message, int NewFeesCount)> DetectViolationAsync(DetectionRequest request);
    Task<IEnumerable<FeeResponse>> GetMyFeesAsync(string plate);
}

public class TrafficService : ITrafficService
{
    private readonly ApplicationDbContext _context;

    public TrafficService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<(bool IsSuccess, string Message, int NewFeesCount)> DetectViolationAsync(DetectionRequest request)
    {
        if (string.IsNullOrEmpty(request.LicensePlate))
            return (false, "رقم اللوحة مطلوب", 0);

        var car = await _context.Cars.FirstOrDefaultAsync(c => c.LicensePlate == request.LicensePlate);
        if (car == null) return (false, "هذه العربة غير مسجلة في المنظومة", 0);

        var newViolations = new List<LicenseFee>();

        if (request.Speed > 120)
        {
            newViolations.Add(new LicenseFee {
                CarId = car.Id,
                Amount = 1000, 
                IssueDate = DateTime.UtcNow,
                IsPaid = false
            });
        }

        if (car.LicenseExpiration < DateTime.UtcNow)
        {
            newViolations.Add(new LicenseFee {
                CarId = car.Id,
                Amount = 500,
                IssueDate = DateTime.UtcNow,
                IsPaid = false
            });
        }

        if (newViolations.Any())
        {
            _context.LicenseFees.AddRange(newViolations);
            await _context.SaveChangesAsync();
            return (true, "تم تسجيل المخالفات بنجاح", newViolations.Count);
        }

        return (true, "لا توجد مخالفات، القيادة آمنة", 0);
    }

    public async Task<IEnumerable<FeeResponse>> GetMyFeesAsync(string plate)
    {
        return await _context.LicenseFees
            .Where(f => f.Car.LicensePlate == plate)
            .Select(f => new FeeResponse {
                Plate = plate,
                Amount = f.Amount,
                Date = f.IssueDate,
                IsPaid = f.IsPaid,
                ViolationReason = f.Amount == 1000 ? "تجاوز السرعة" : "رخصة منتهية"
            }).ToListAsync();
    }
}
