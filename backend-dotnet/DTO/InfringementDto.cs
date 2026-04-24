namespace backend_dotnet.DTO;

public class InfringementDto
{
    public int Id { get; set; }
    public string PlateText { get; set; } = string.Empty;
    public string CarOwner { get; set; } = string.Empty;
    public string ExpectedCheckpoint { get; set; } = string.Empty;
    public string ActualCheckpoint { get; set; } = string.Empty;
    public string RouteName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime DetectedAt { get; set; }
}
