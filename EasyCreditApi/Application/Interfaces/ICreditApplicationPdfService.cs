using EasyCreditApi.Application.DTO.Credit;

namespace EasyCreditApi.Application.Interfaces;

public interface ICreditApplicationPdfService
{
    byte[] GenerateInstallmentSchedulePdf(ResCreditApplicationDetailDto application);
}
