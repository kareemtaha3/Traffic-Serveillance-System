using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using backend_dotnet.Data;
using backend_dotnet.DTO;

namespace backend_dotnet.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InfringementsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public InfringementsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<InfringementDto>>> GetAll(
        [FromQuery] string? plate = null,
        [FromQuery] int? routeId = null)
    {
        var query = _context.Infringements
            .Include(i => i.Car)
            .Include(i => i.ExpectedCheckpoint)
                .ThenInclude(c => c!.BusRoute)
            .Include(i => i.ExpectedCheckpoint)
                .ThenInclude(c => c!.Camera)
            .Include(i => i.ActualCheckpoint)
                .ThenInclude(c => c.BusRoute)
            .Include(i => i.ActualCheckpoint)
                .ThenInclude(c => c.Camera)
            .AsQueryable();

        if (!string.IsNullOrEmpty(plate))
            query = query.Where(i => i.PlateText.Contains(plate));

        if (routeId.HasValue)
            query = query.Where(i => i.ActualCheckpoint.BusRouteId == routeId.Value);

        var infringements = await query
            .OrderByDescending(i => i.DetectedAt)
            .Select(i => new InfringementDto
            {
                Id = i.Id,
                PlateText = i.PlateText,
                CarOwner = i.Car.OwnerName,
                ExpectedCheckpoint = i.ExpectedCheckpoint != null ? i.ExpectedCheckpoint.Camera.Name : "N/A",
                ActualCheckpoint = i.ActualCheckpoint.Camera.Name,
                RouteName = i.ActualCheckpoint.BusRoute.Name,
                Description = i.Description,
                DetectedAt = i.DetectedAt
            })
            .ToListAsync();

        return Ok(infringements);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<InfringementDto>> GetById(int id)
    {
        var infringement = await _context.Infringements
            .Include(i => i.Car)
            .Include(i => i.ExpectedCheckpoint)
                .ThenInclude(c => c!.Camera)
            .Include(i => i.ActualCheckpoint)
                .ThenInclude(c => c.BusRoute)
            .Include(i => i.ActualCheckpoint)
                .ThenInclude(c => c.Camera)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (infringement == null) return NotFound();

        return Ok(new InfringementDto
        {
            Id = infringement.Id,
            PlateText = infringement.PlateText,
            CarOwner = infringement.Car.OwnerName,
            ExpectedCheckpoint = infringement.ExpectedCheckpoint?.Camera.Name ?? "N/A",
            ActualCheckpoint = infringement.ActualCheckpoint.Camera.Name,
            RouteName = infringement.ActualCheckpoint.BusRoute.Name,
            Description = infringement.Description,
            DetectedAt = infringement.DetectedAt
        });
    }
}
