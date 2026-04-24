namespace backend_dotnet.Models;

public class CheckpointLog
{
    public int Id { get; set; }
    public string PlateText { get; set; } = string.Empty;
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
    public bool IsValid { get; set; }

    public int CarId { get; set; }
    public Car Car { get; set; } = null!;

    public int CheckpointId { get; set; }
    public Checkpoint Checkpoint { get; set; } = null!;
}
