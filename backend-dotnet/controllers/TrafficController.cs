using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using backend_dotnet.Data;
using backend_dotnet.Models;
using backend_dotnet.dto;

namespace backend_dotnet.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TrafficController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public TrafficController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpPost("detect")]
    public async Task<IActionResult> Detect([FromBody] DetectionRequest request)
    {
        if (string.IsNullOrEmpty(request.LicensePlate))
            return BadRequest("رقم اللوحة مطلوب");

        var car = await _context.Cars.FirstOrDefaultAsync(c => c.LicensePlate == request.LicensePlate);
        if (car == null) return NotFound("هذه العربة غير مسجلة في المنظومة");

        var newViolations = new List<LicenseFee>();

        // 1. فحص السرعة
        if (request.Speed > 120)
        {
            newViolations.Add(new LicenseFee {
                CarId = car.Id,
                Amount = 1000, // يمكن لاحقاً سحب القيمة من جدول Violations
                IssueDate = DateTime.UtcNow,
                IsPaid = false
            });
        }

        // 2. فحص صلاحية الرخصة
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
            return Ok(new { Message = "تم تسجيل المخالفات بنجاح", Count = newViolations.Count });
        }

        return Ok(new { Message = "لا توجد مخالفات، القيادة آمنة" });
    }

    [HttpGet("my-fees/{plate}")]
    public async Task<ActionResult<IEnumerable<FeeResponse>>> GetFees(string plate)
    {
        var fees = await _context.LicenseFees
            .Where(f => f.Car.LicensePlate == plate)
            .Select(f => new FeeResponse {
                Plate = plate,
                Amount = f.Amount,
                Date = f.IssueDate,
                IsPaid = f.IsPaid,
                ViolationReason = f.Amount == 1000 ? "تجاوز السرعة" : "رخصة منتهية"
            }).ToListAsync();

        if (!fees.Any()) return NotFound("لا توجد رسوم مستحقة");

        return Ok(fees);
    }
}