namespace EasyCreditApi.Infrastructure.Oracle;

using EasyCreditApi.Domain.Entities;

public interface IOracleCreditApplicationRepository
{
    Task<(IReadOnlyList<CreditApplication> Items, int Total)> GetPagedAsync(string? status, int page, int size);
    Task<CreditApplication?> GetByIdAsync(int applicationId);
    Task<int> CreateWithInstallmentsAsync(CreditApplication application, IEnumerable<CreditInstallment> installments);
    Task<(string Status, string Message)> ApproveOrRejectAsync(int applicationId, string action, string approver);
    Task<(IReadOnlyList<CreditInstallment> Installments, string Status, string Message)> CalculateInstallmentSpAsync(decimal loanAmount, int tenorMonths);
}

