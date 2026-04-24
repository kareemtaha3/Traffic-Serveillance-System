namespace backend_dotnet.DTO;

public class CheckpointDetectionResultDto
{
    public bool IsValid { get; set; }
    public string PlateText { get; set; } = string.Empty;
    public string CameraId { get; set; } = string.Empty;
    public string CheckpointName { get; set; } = string.Empty;
    public string? ExpectedCheckpointName { get; set; }
    public string Message { get; set; } = string.Empty;
    public int? InfringementId { get; set; }
}

public class AiRecognitionResponse
{
    public List<AiPlateResult> Plates { get; set; } = new();
}

public class AiPlateResult
{
    [System.Text.Json.Serialization.JsonPropertyName("plate_text")]
    public string PlateText { get; set; } = string.Empty;
    
    [System.Text.Json.Serialization.JsonPropertyName("score")]
    public double Score { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("bbox")]
    public List<float> Bbox { get; set; } = new();
}
