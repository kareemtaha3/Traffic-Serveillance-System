namespace backend_dotnet.Models;

public class LicenseFee
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public DateTime IssueDate { get; set; } = DateTime.UtcNow;
    public bool IsPaid { get; set; } = false;

    // ربط المخالفة بالعربية (Foreign Key)
    public int CarId { get; set; }
    public Car Car { get; set; } = null!;
}