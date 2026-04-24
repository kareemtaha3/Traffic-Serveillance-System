using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using backend_dotnet.Data;
using backend_dotnet.DTO;
using backend_dotnet.Models;

namespace backend_dotnet.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VehiclesController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public VehiclesController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<VehicleDto>>> GetAll()
    {
        var vehicles = await _context.Cars
            .Include(c => c.VehicleRoutes.Where(vr => vr.IsActive))
                .ThenInclude(vr => vr.BusRoute)
            .ToListAsync();

        return Ok(vehicles.Select(c =>
        {
            var activeRoute = c.VehicleRoutes.FirstOrDefault(vr => vr.IsActive);
            return new VehicleDto
            {
                Id = c.Id,
                LicensePlate = c.LicensePlate,
                OwnerName = c.OwnerName,
                LicenseExpiration = c.LicenseExpiration,
                ActiveRouteName = activeRoute?.BusRoute?.Name,
                ActiveRouteId = activeRoute?.BusRouteId
            };
        }));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<VehicleDto>> GetById(int id)
    {
        var car = await _context.Cars
            .Include(c => c.VehicleRoutes.Where(vr => vr.IsActive))
                .ThenInclude(vr => vr.BusRoute)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (car == null) return NotFound();

        var activeRoute = car.VehicleRoutes.FirstOrDefault(vr => vr.IsActive);
        return Ok(new VehicleDto
        {
            Id = car.Id,
            LicensePlate = car.LicensePlate,
            OwnerName = car.OwnerName,
            LicenseExpiration = car.LicenseExpiration,
            ActiveRouteName = activeRoute?.BusRoute?.Name,
            ActiveRouteId = activeRoute?.BusRouteId
        });
    }

    [HttpPost]
    public async Task<ActionResult<VehicleDto>> Create([FromBody] CreateVehicleDto dto)
    {
        if (string.IsNullOrEmpty(dto.LicensePlate))
            return BadRequest("License plate is required");

        var existing = await _context.Cars.AnyAsync(c => c.LicensePlate == dto.LicensePlate);
        if (existing)
            return BadRequest("A vehicle with this license plate already exists");

        var car = new Car
        {
            LicensePlate = dto.LicensePlate,
            OwnerName = dto.OwnerName,
            LicenseExpiration = DateTime.SpecifyKind(dto.LicenseExpiration, DateTimeKind.Utc)
        };

        _context.Cars.Add(car);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = car.Id }, new VehicleDto
        {
            Id = car.Id,
            LicensePlate = car.LicensePlate,
            OwnerName = car.OwnerName,
            LicenseExpiration = car.LicenseExpiration
        });
    }

    [HttpPost("{id}/assign-route")]
    public async Task<IActionResult> AssignRoute(int id, [FromBody] AssignRouteDto dto)
    {
        var car = await _context.Cars
            .Include(c => c.VehicleRoutes)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (car == null) return NotFound("Vehicle not found");

        var route = await _context.BusRoutes.FindAsync(dto.RouteId);
        if (route == null) return NotFound("Route not found");

        foreach (var vr in car.VehicleRoutes.Where(vr => vr.IsActive))
        {
            vr.IsActive = false;
        }

        car.VehicleRoutes.Add(new VehicleRoute
        {
            CarId = car.Id,
            BusRouteId = dto.RouteId,
            IsActive = true
        });

        await _context.SaveChangesAsync();

        return Ok(new { Message = $"Vehicle assigned to route '{route.Name}'" });
    }

    [HttpGet("{id}/checkpoint-logs")]
    public async Task<ActionResult<IEnumerable<CheckpointLogDto>>> GetCheckpointLogs(int id)
    {
        var logs = await _context.CheckpointLogs
            .Where(cl => cl.CarId == id)
            .Include(cl => cl.Checkpoint)
                .ThenInclude(cp => cp.BusRoute)
            .Include(cl => cl.Checkpoint)
                .ThenInclude(cp => cp.Camera)
            .OrderByDescending(cl => cl.DetectedAt)
            .Select(cl => new CheckpointLogDto
            {
                Id = cl.Id,
                PlateText = cl.PlateText,
                CheckpointName = cl.Checkpoint.Camera.Name,
                CameraId = cl.Checkpoint.Camera.CameraIdentifier,
                RouteName = cl.Checkpoint.BusRoute.Name,
                IsValid = cl.IsValid,
                DetectedAt = cl.DetectedAt
            })
            .ToListAsync();

        return Ok(logs);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var car = await _context.Cars.FindAsync(id);
        if (car == null) return NotFound();

        _context.Cars.Remove(car);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
