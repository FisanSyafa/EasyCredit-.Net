namespace EasyCreditApi.Domain.Entities;

public class CreditApplication
{
    public int ApplicationId { get; set; }
    public int CustomerId { get; set; }
    public decimal LoanAmount { get; set; }
    public int TenorMonths { get; set; }
    public decimal InterestRate { get; set; } = 0.12m;
    public int CreditScore { get; set; } = 700;
    public string Status { get; set; } = "PENDING";
    public DateTime ApplicationDate { get; set; } = DateTime.UtcNow;
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
}
