namespace backend_dotnet.Models;

public class Camera
{
    public int Id { get; set; }
    
    public string CameraIdentifier { get; set; } = string.Empty;
    
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<Checkpoint> RouteCheckpoints { get; set; } = new();
}
