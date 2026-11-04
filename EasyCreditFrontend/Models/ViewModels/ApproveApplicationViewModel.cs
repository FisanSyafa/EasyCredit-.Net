using System.ComponentModel.DataAnnotations;

namespace EasyCreditFrontend.Models.ViewModels;

// Model untuk request AJAX approve.
// Tetap divalidasi di server walau client sudah mengirim data.
public class ApproveApplicationViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Application ID tidak valid.")]
    public int ApplicationId { get; set; }

    [Required(ErrorMessage = "Nama approver wajib diisi.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Nama approver 2-100 karakter.")]
    [Display(Name = "Nama approver")]
    public string Approver { get; set; } = string.Empty;
}