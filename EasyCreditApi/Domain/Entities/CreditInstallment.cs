namespace EasyCreditApi.Domain.Entities;

public class CreditInstallment
{
    public int InstallmentId { get; set; }
    public int ApplicationId { get; set; }
    public int InstallmentNo { get; set; }
    public DateTime DueDate { get; set; }
    public decimal InstallmentAmount { get; set; }
    public string Status { get; set; } = "UNPAID";
}

