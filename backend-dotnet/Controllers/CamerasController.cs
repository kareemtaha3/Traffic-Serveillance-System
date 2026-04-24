using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using backend_dotnet.Data;
using backend_dotnet.Models;

namespace backend_dotnet.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CamerasController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public CamerasController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Camera>>> GetAll()
    {
        return await _context.Cameras.OrderBy(c => c.Name).ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Camera>> GetById(int id)
    {
        var camera = await _context.Cameras.FindAsync(id);
        if (camera == null) return NotFound();
        return camera;
    }

    [HttpPost]
    public async Task<ActionResult<Camera>> Create(Camera dto)
    {
        if (await _context.Cameras.AnyAsync(c => c.CameraIdentifier == dto.CameraIdentifier))
            return BadRequest("A camera with this identifier already exists");

        _context.Cameras.Add(dto);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, Camera dto)
    {
        if (id != dto.Id) return BadRequest();

        var camera = await _context.Cameras.FindAsync(id);
        if (camera == null) return NotFound();

        if (!string.IsNullOrEmpty(dto.CameraIdentifier))
        {
            var conflict = await _context.Cameras.AnyAsync(c => c.CameraIdentifier == dto.CameraIdentifier);
            if (conflict && camera.CameraIdentifier != dto.CameraIdentifier)
                return BadRequest("Camera identifier is already in use.");
        }

        camera.Name = dto.Name;
        camera.CameraIdentifier = dto.CameraIdentifier;
        camera.Description = dto.Description;
        camera.Latitude = dto.Latitude;
        camera.Longitude = dto.Longitude;
        camera.IsActive = dto.IsActive;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var camera = await _context.Cameras.FindAsync(id);
        if (camera == null) return NotFound();

        _context.Cameras.Remove(camera);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
