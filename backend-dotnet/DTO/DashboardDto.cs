namespace backend_dotnet.DTO;

public class DashboardStatsDto
{
    public int TotalVehicles { get; set; }
    public int TotalRoutes { get; set; }
    public int TotalInfringements { get; set; }
    public int TotalCheckpointLogs { get; set; }
    public int ActiveVehicles { get; set; }
    public List<CheckpointLogDto> RecentLogs { get; set; } = new();
}

public class CheckpointLogDto
{
    public int Id { get; set; }
    public string PlateText { get; set; } = string.Empty;
    public string CheckpointName { get; set; } = string.Empty;
    public string CameraId { get; set; } = string.Empty;
    public string RouteName { get; set; } = string.Empty;
    public bool IsValid { get; set; }
    public DateTime DetectedAt { get; set; }
}
