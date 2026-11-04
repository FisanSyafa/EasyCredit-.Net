namespace EasyCreditApi.Infrastructure.Oracle;

using EasyCreditApi.Domain.Entities;

public interface IOracleCreditInstallmentRepository
{
    Task<IReadOnlyList<CreditInstallment>> GetByApplicationIdAsync(int applicationId);
}

