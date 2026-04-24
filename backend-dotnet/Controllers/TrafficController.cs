using Microsoft.AspNetCore.Mvc;
using backend_dotnet.Services;
using backend_dotnet.DTO;

namespace backend_dotnet.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TrafficController : ControllerBase
{
    private readonly ITrafficService _trafficService;

    public TrafficController(ITrafficService trafficService)
    {
        _trafficService = trafficService;
    }

    [HttpPost("detect")]
    public async Task<IActionResult> Detect([FromBody] DetectionRequest request)
    {
        var result = await _trafficService.DetectViolationAsync(request);
        
        if (!result.IsSuccess)
            return string.IsNullOrEmpty(request.LicensePlate) ? BadRequest(result.Message) : NotFound(result.Message);

        if (result.NewFeesCount > 0)
            return Ok(new { Message = result.Message, Count = result.NewFeesCount });

        return Ok(new { Message = result.Message });
    }

    [HttpGet("my-fees/{plate}")]
    public async Task<ActionResult<IEnumerable<FeeResponse>>> GetFees(string plate)
    {
        var fees = await _trafficService.GetMyFeesAsync(plate);

        if (!fees.Any()) return NotFound("لا توجد رسوم مستحقة");

        return Ok(fees);
    }
}