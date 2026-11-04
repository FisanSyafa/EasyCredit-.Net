using EasyCreditApi.Application.DTO.Customer;

namespace EasyCreditApi.Application.DTO.Credit;

public class ResCreditApplicationDetailDto
{
    public int ApplicationId { get; set; }
    public int CustomerId { get; set; }
    public ResCustomerDto? Customer { get; set; }
    public decimal LoanAmount { get; set; }
    public int TenorMonths { get; set; }
    public decimal InterestRate { get; set; }
    public int CreditScore { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime ApplicationDate { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public List<ResInstallmentDto> Installments { get; set; } = new();
}

