using EasyCreditApi.Application.DTO.Credit;

namespace EasyCreditApi.Application.Interfaces;

public interface ICreditApplicationService
{
    Task<(IReadOnlyList<ResCreditApplicationListItemDto> Items, int Total)> GetPagedAsync(
        string? status, string? keyword, int page, int size);

    Task<ResCreditApplicationDetailDto?> GetByIdAsync(int applicationId);

    Task<IReadOnlyList<ResInstallmentDto>> PreviewInstallmentAsync(decimal loanAmount, int tenorMonths);

    Task<int> CreateAsync(ReqCreateCreditApplicationDto dto);

    Task<string> ApproveAsync(int applicationId, string approver);

    Task<string> RejectAsync(int applicationId, string approver);
}

