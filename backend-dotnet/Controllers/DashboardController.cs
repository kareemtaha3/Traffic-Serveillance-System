using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using backend_dotnet.Data;
using backend_dotnet.DTO;

namespace backend_dotnet.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public DashboardController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("stats")]
    public async Task<ActionResult<DashboardStatsDto>> GetStats()
    {
        var totalVehicles = await _context.Cars.CountAsync();
        var totalRoutes = await _context.BusRoutes.CountAsync();
        var totalInfringements = await _context.Infringements.CountAsync();
        var totalCheckpointLogs = await _context.CheckpointLogs.CountAsync();
        var activeVehicles = await _context.VehicleRoutes.Where(vr => vr.IsActive).CountAsync();

        var recentLogs = await _context.CheckpointLogs
            .Include(cl => cl.Checkpoint)
                .ThenInclude(cp => cp.BusRoute)
            .Include(cl => cl.Checkpoint)
                .ThenInclude(cp => cp.Camera)
            .OrderByDescending(cl => cl.DetectedAt)
            .Take(20)
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

        return Ok(new DashboardStatsDto
        {
            TotalVehicles = totalVehicles,
            TotalRoutes = totalRoutes,
            TotalInfringements = totalInfringements,
            TotalCheckpointLogs = totalCheckpointLogs,
            ActiveVehicles = activeVehicles,
            RecentLogs = recentLogs
        });
    }
}
