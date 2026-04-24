using backend_dotnet.DTO;

namespace backend_dotnet.Services;

public interface IAiServiceClient
{
    Task<AiRecognitionResponse> RecognizePlateAsync(byte[] imageBytes, string fileName);
}
