using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using backend_dotnet.Data;
using backend_dotnet.DTO;
using backend_dotnet.Models;

namespace backend_dotnet.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RoutesController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public RoutesController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<RouteDto>>> GetAll()
    {
        var routes = await _context.BusRoutes
            .Include(r => r.Checkpoints.OrderBy(c => c.SequenceOrder))
                .ThenInclude(c => c.Camera)
            .Include(r => r.VehicleRoutes)
            .ToListAsync();

        return Ok(routes.Select(r => new RouteDto
        {
            Id = r.Id,
            Name = r.Name,
            Description = r.Description,
            CreatedAt = r.CreatedAt,
            VehicleCount = r.VehicleRoutes.Count(vr => vr.IsActive),
            Checkpoints = r.Checkpoints.Select(c => new CheckpointDto
            {
                Id = c.Id,
                CameraId = c.CameraId,
                CameraIdentifier = c.Camera?.CameraIdentifier ?? string.Empty,
                Name = c.Camera?.Name ?? string.Empty,
                SequenceOrder = c.SequenceOrder,
                Latitude = c.Camera?.Latitude ?? 0,
                Longitude = c.Camera?.Longitude ?? 0
            }).ToList()
        }));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<RouteDto>> GetById(int id)
    {
        var route = await _context.BusRoutes
            .Include(r => r.Checkpoints.OrderBy(c => c.SequenceOrder))
                .ThenInclude(c => c.Camera)
            .Include(r => r.VehicleRoutes)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (route == null) return NotFound();

        return Ok(new RouteDto
        {
            Id = route.Id,
            Name = route.Name,
            Description = route.Description,
            CreatedAt = route.CreatedAt,
            VehicleCount = route.VehicleRoutes.Count(vr => vr.IsActive),
            Checkpoints = route.Checkpoints.Select(c => new CheckpointDto
            {
                Id = c.Id,
                CameraId = c.CameraId,
                CameraIdentifier = c.Camera?.CameraIdentifier ?? string.Empty,
                Name = c.Camera?.Name ?? string.Empty,
                SequenceOrder = c.SequenceOrder,
                Latitude = c.Camera?.Latitude ?? 0,
                Longitude = c.Camera?.Longitude ?? 0
            }).ToList()
        });
    }

    [HttpPost]
    public async Task<ActionResult<RouteDto>> Create([FromBody] CreateRouteDto dto)
    {
        if (string.IsNullOrEmpty(dto.Name))
            return BadRequest("Route name is required");

        if (dto.CameraIds == null || !dto.CameraIds.Any())
            return BadRequest("At least one camera checkpoint is required");

        var existingCamerasCount = await _context.Cameras.CountAsync(c => dto.CameraIds.Contains(c.Id));
        if (existingCamerasCount != dto.CameraIds.Distinct().Count())
            return BadRequest("One or more provided Camera IDs do not exist");

        var route = new BusRoute
        {
            Name = dto.Name,
            Description = dto.Description,
            Checkpoints = dto.CameraIds.Select((cameraId, index) => new Checkpoint
            {
                CameraId = cameraId,
                SequenceOrder = index + 1
            }).ToList()
        };

        _context.BusRoutes.Add(route);
        await _context.SaveChangesAsync();

        await _context.Entry(route).Collection(r => r.Checkpoints).Query().Include(c => c.Camera).LoadAsync();

        return CreatedAtAction(nameof(GetById), new { id = route.Id }, new RouteDto
        {
            Id = route.Id,
            Name = route.Name,
            Description = route.Description,
            CreatedAt = route.CreatedAt,
            Checkpoints = route.Checkpoints.Select(c => new CheckpointDto
            {
                Id = c.Id,
                CameraId = c.CameraId,
                CameraIdentifier = c.Camera?.CameraIdentifier ?? string.Empty,
                Name = c.Camera?.Name ?? string.Empty,
                SequenceOrder = c.SequenceOrder,
                Latitude = c.Camera?.Latitude ?? 0,
                Longitude = c.Camera?.Longitude ?? 0
            }).ToList()
        });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateRouteDto dto)
    {
        var route = await _context.BusRoutes
            .Include(r => r.Checkpoints)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (route == null) return NotFound();

        if (dto.CameraIds == null || !dto.CameraIds.Any())
            return BadRequest("At least one camera checkpoint is required");

        var existingCamerasCount = await _context.Cameras.CountAsync(c => dto.CameraIds.Contains(c.Id));
        if (existingCamerasCount != dto.CameraIds.Distinct().Count())
            return BadRequest("One or more provided Camera IDs do not exist");

        route.Name = dto.Name;
        route.Description = dto.Description;

        _context.Checkpoints.RemoveRange(route.Checkpoints);
        route.Checkpoints = dto.CameraIds.Select((cameraId, index) => new Checkpoint
        {
            CameraId = cameraId,
            SequenceOrder = index + 1,
            BusRouteId = route.Id
        }).ToList();

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var route = await _context.BusRoutes.FindAsync(id);
        if (route == null) return NotFound();

        _context.BusRoutes.Remove(route);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
