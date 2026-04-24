using System.Net.Http.Headers;
using System.Text.Json;
using backend_dotnet.DTO;

namespace backend_dotnet.Services;

public class AiServiceClient : IAiServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AiServiceClient> _logger;

    public AiServiceClient(HttpClient httpClient, ILogger<AiServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<AiRecognitionResponse> RecognizePlateAsync(byte[] imageBytes, string fileName)
    {
        using var content = new MultipartFormDataContent();
        var imageContent = new ByteArrayContent(imageBytes);
        imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        content.Add(imageContent, "file", fileName);

        _logger.LogInformation("Sending image '{FileName}' ({Size} bytes) to AI service", fileName, imageBytes.Length);

        var response = await _httpClient.PostAsync("/recognize", content);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<AiRecognitionResponse>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return result ?? new AiRecognitionResponse();
    }
}
