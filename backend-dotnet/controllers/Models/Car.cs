namespace backend_dotnet.Models;

public class Car
{
    public int Id { get; set; }
    public string LicensePlate { get; set; } = string.Empty; 
    public string OwnerName { get; set; } = string.Empty; 
    public DateTime LicenseExpiration { get; set; } 
    
    public List<LicenseFee> Fees { get; set; } = new();
}