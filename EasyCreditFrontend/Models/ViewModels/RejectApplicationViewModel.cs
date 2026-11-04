using System.ComponentModel.DataAnnotations;

namespace EasyCreditFrontend.Models.ViewModels;

public class RejectApplicationViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Application ID tidak valid.")]
    public int ApplicationId { get; set; }

    [Required(ErrorMessage = "Nama approver wajib diisi.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Nama approver 2-100 karakter.")]
    [Display(Name = "Nama approver")]
    public string Approver { get; set; } = string.Empty;
}

