namespace backend_dotnet.Models;

public class VehicleRoute
{
    public int Id { get; set; }
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    public int CarId { get; set; }
    public Car Car { get; set; } = null!;

    public int BusRouteId { get; set; }
    public BusRoute BusRoute { get; set; } = null!;
}
