using Microsoft.EntityFrameworkCore;
using backend_dotnet.Data;
using backend_dotnet.DTO;
using backend_dotnet.Models;

namespace backend_dotnet.Services;

public class RouteValidationService : IRouteValidationService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<RouteValidationService> _logger;

    public RouteValidationService(ApplicationDbContext context, ILogger<RouteValidationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<CheckpointDetectionResultDto> ProcessDetectionAsync(string plateText, string cameraId)
    {
        var car = await _context.Cars
            .Include(c => c.VehicleRoutes.Where(vr => vr.IsActive))
            .FirstOrDefaultAsync(c => c.LicensePlate == plateText);

        if (car == null)
        {
            _logger.LogWarning("Unregistered vehicle detected: {PlateText} at camera {CameraId}", plateText, cameraId);
            return new CheckpointDetectionResultDto
            {
                IsValid = false,
                PlateText = plateText,
                CameraId = cameraId,
                Message = $"Unregistered vehicle: {plateText}"
            };
        }

        var activeVehicleRoute = car.VehicleRoutes.FirstOrDefault(vr => vr.IsActive);

        Checkpoint? checkpoint = null;
        Camera? camera = await _context.Cameras.FirstOrDefaultAsync(c => c.CameraIdentifier == cameraId);

        if (camera == null)
        {
            return new CheckpointDetectionResultDto
            {
                IsValid = false,
                PlateText = plateText,
                CameraId = cameraId,
                Message = $"Unknown camera ID: {cameraId}"
            };
        }

        if (activeVehicleRoute != null)
        {
            checkpoint = await _context.Checkpoints
                .Include(c => c.Camera)
                .Include(c => c.BusRoute)
                .FirstOrDefaultAsync(c => c.BusRouteId == activeVehicleRoute.BusRouteId && c.CameraId == camera.Id);
        }

        if (activeVehicleRoute == null || checkpoint == null)
        {
            _logger.LogWarning("Vehicle {PlateText} detected at camera {CameraId} but not on an active assigned route path",
                plateText, cameraId);

            var infringement = new Infringement
            {
                CarId = car.Id,
                ActualCheckpointId = checkpoint?.Id ?? 0,
                ExpectedCheckpointId = null,
                PlateText = plateText,
                DetectedAt = DateTime.UtcNow,
                Description = $"Vehicle detected at '{camera.Name}' but is off assigned route or not assigned"
            };
            
            if (checkpoint == null) {
                var anyCheckpoint = await _context.Checkpoints.FirstOrDefaultAsync(c => c.CameraId == camera.Id);
                if (anyCheckpoint != null)
                {
                    infringement.ActualCheckpointId = anyCheckpoint.Id;
                    _context.Infringements.Add(infringement);
                }
            } else {
                _context.Infringements.Add(infringement);
            }
            
            await _context.SaveChangesAsync();

            return new CheckpointDetectionResultDto
            {
                IsValid = false,
                PlateText = plateText,
                CameraId = cameraId,
                CheckpointName = camera.Name,
                Message = infringement.Description,
                InfringementId = infringement.Id
            };
        }

        var routeCheckpoints = await _context.Checkpoints
            .Include(c => c.Camera)
            .Where(c => c.BusRouteId == checkpoint.BusRouteId)
            .OrderBy(c => c.SequenceOrder)
            .ToListAsync();

        var lastLog = await _context.CheckpointLogs
            .Where(cl => cl.CarId == car.Id && cl.IsValid &&
                         cl.Checkpoint.BusRouteId == checkpoint.BusRouteId)
            .OrderByDescending(cl => cl.DetectedAt)
            .Include(cl => cl.Checkpoint)
            .FirstOrDefaultAsync();

        Checkpoint? expectedCheckpoint;
        if (lastLog == null)
        {
            expectedCheckpoint = routeCheckpoints.FirstOrDefault();
        }
        else
        {
            var lastIndex = routeCheckpoints.FindIndex(c => c.Id == lastLog.CheckpointId);
            if (lastIndex >= 0 && lastIndex + 1 < routeCheckpoints.Count)
            {
                expectedCheckpoint = routeCheckpoints[lastIndex + 1];
            }
            else
            {
                expectedCheckpoint = routeCheckpoints.FirstOrDefault();
            }
        }

        bool isValid = expectedCheckpoint != null && expectedCheckpoint.Id == checkpoint.Id;

        var checkpointLog = new CheckpointLog
        {
            CarId = car.Id,
            CheckpointId = checkpoint.Id,
            PlateText = plateText,
            DetectedAt = DateTime.UtcNow,
            IsValid = isValid
        };
        _context.CheckpointLogs.Add(checkpointLog);

        if (!isValid)
        {
            var infringement = new Infringement
            {
                CarId = car.Id,
                ExpectedCheckpointId = expectedCheckpoint?.Id,
                ActualCheckpointId = checkpoint.Id,
                PlateText = plateText,
                DetectedAt = DateTime.UtcNow,
                Description = $"Expected checkpoint '{expectedCheckpoint?.Camera.Name ?? "N/A"}' (seq {expectedCheckpoint?.SequenceOrder}), " +
                              $"but arrived at '{checkpoint.Camera.Name}' (seq {checkpoint.SequenceOrder})"
            };
            _context.Infringements.Add(infringement);
            await _context.SaveChangesAsync();

            _logger.LogWarning("Infringement: {Description}", infringement.Description);

            return new CheckpointDetectionResultDto
            {
                IsValid = false,
                PlateText = plateText,
                CameraId = cameraId,
                CheckpointName = checkpoint.Camera.Name,
                ExpectedCheckpointName = expectedCheckpoint?.Camera.Name,
                Message = infringement.Description,
                InfringementId = infringement.Id
            };
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Valid checkpoint: Vehicle {PlateText} at '{CheckpointName}' (seq {Seq})",
            plateText, checkpoint.Camera.Name, checkpoint.SequenceOrder);

        return new CheckpointDetectionResultDto
        {
            IsValid = true,
            PlateText = plateText,
            CameraId = cameraId,
            CheckpointName = checkpoint.Camera.Name,
            Message = $"Valid: Vehicle passed checkpoint '{checkpoint.Camera.Name}' in correct order"
        };
    }
}
