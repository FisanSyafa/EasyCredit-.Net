using EasyCreditFrontend.Models.Dtos;

namespace EasyCreditFrontend.Models.ViewModels;

public class ApplicationDetailViewModel
{
    public ApplicationDetailDto Application { get; set; } = new();

    public bool CanApproveOrReject =>
        string.Equals(Application.Status, "SUBMITTED", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(Application.Status, "PENDING", StringComparison.OrdinalIgnoreCase);
}

