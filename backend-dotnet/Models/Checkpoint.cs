namespace backend_dotnet.Models;

public class Checkpoint
{
    public int Id { get; set; }
    public int SequenceOrder { get; set; }

    public int BusRouteId { get; set; }
    public BusRoute BusRoute { get; set; } = null!;

    public int CameraId { get; set; }
    public Camera Camera { get; set; } = null!;

    public List<CheckpointLog> CheckpointLogs { get; set; } = new();
}
