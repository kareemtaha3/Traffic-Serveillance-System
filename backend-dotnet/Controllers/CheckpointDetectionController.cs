using Microsoft.AspNetCore.Mvc;
using backend_dotnet.DTO;
using backend_dotnet.Services;

namespace backend_dotnet.Controllers;

[ApiController]
[Route("api/checkpoint-detection")]
public class CheckpointDetectionController : ControllerBase
{
    private readonly IAiServiceClient _aiService;
    private readonly IRouteValidationService _routeValidation;
    private readonly ILogger<CheckpointDetectionController> _logger;

    public CheckpointDetectionController(
        IAiServiceClient aiService,
        IRouteValidationService routeValidation,
        ILogger<CheckpointDetectionController> logger)
    {
        _aiService = aiService;
        _routeValidation = routeValidation;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> DetectCheckpoint(IFormFile image, [FromForm] string cameraId)
    {
        if (image == null || image.Length == 0)
            return BadRequest("Image is required");

        if (string.IsNullOrEmpty(cameraId))
            return BadRequest("Camera ID is required");

        _logger.LogInformation("Checkpoint detection: camera={CameraId}, imageSize={Size}",
            cameraId, image.Length);

        using var memoryStream = new MemoryStream();
        await image.CopyToAsync(memoryStream);
        var imageBytes = memoryStream.ToArray();

        AiRecognitionResponse aiResponse;
        try
        {
            aiResponse = await _aiService.RecognizePlateAsync(imageBytes, image.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to communicate with AI service");
            return StatusCode(503, new { Message = "AI service unavailable", Error = ex.Message });
        }

        if (aiResponse.Plates == null || !aiResponse.Plates.Any())
        {
            _logger.LogWarning("No plates detected in image from camera {CameraId}", cameraId);
            return Ok(new CheckpointDetectionResultDto
            {
                IsValid = false,
                CameraId = cameraId,
                Message = "No license plate detected in the image"
            });
        }

        var bestPlate = aiResponse.Plates.OrderByDescending(p => p.Score).First();

        _logger.LogInformation("AI detected plate: {PlateText} (score: {Score})",
            bestPlate.PlateText, bestPlate.Score);

        var result = await _routeValidation.ProcessDetectionAsync(bestPlate.PlateText, cameraId);

        return Ok(result);
    }
}
public class DetectCheckpointRequest
{
    public IFormFile? Image { get; set; }
    public string CameraId { get; set; } = string.Empty;
}
