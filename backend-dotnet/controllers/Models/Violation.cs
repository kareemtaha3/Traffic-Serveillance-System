namespace backend_dotnet.Models;

public class Violation
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty; // مثلاً: Speed, Parking, Expiration
    public decimal FineAmount { get; set; } // مبلغ الغرامة
}