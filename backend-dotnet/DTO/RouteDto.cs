namespace backend_dotnet.DTO;

public class CreateRouteDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<int> CameraIds { get; set; } = new();
}

public class RouteDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<CheckpointDto> Checkpoints { get; set; } = new();
    public int VehicleCount { get; set; }
}

public class CheckpointDto
{
    public int Id { get; set; }
    public int CameraId { get; set; }
    public string CameraIdentifier { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int SequenceOrder { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}
