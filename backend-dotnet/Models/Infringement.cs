namespace backend_dotnet.Models;

public class Infringement
{
    public int Id { get; set; }
    public string PlateText { get; set; } = string.Empty;
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
    public string Description { get; set; } = string.Empty;

    public int CarId { get; set; }
    public Car Car { get; set; } = null!;

    public int? ExpectedCheckpointId { get; set; }
    public Checkpoint? ExpectedCheckpoint { get; set; }

    public int ActualCheckpointId { get; set; }
    public Checkpoint ActualCheckpoint { get; set; } = null!;
}
