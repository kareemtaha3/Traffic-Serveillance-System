namespace backend_dotnet.DTO;

public class CreateVehicleDto
{
    public string LicensePlate { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public DateTime LicenseExpiration { get; set; }
}

public class VehicleDto
{
    public int Id { get; set; }
    public string LicensePlate { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public DateTime LicenseExpiration { get; set; }
    public string? ActiveRouteName { get; set; }
    public int? ActiveRouteId { get; set; }
}

public class AssignRouteDto
{
    public int RouteId { get; set; }
}
