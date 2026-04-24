using backend_dotnet.DTO;

namespace backend_dotnet.Services;

public interface IRouteValidationService
{
    Task<CheckpointDetectionResultDto> ProcessDetectionAsync(string plateText, string cameraId);
}
