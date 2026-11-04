using System.ComponentModel.DataAnnotations;

namespace EasyCreditApi.Application.DTO.Credit;

public class ReqPreviewInstallmentDto
{
    [Required(ErrorMessage = "Jumlah pinjaman wajib diisi.")]
    [Range(1000000, 50000000, ErrorMessage = "Jumlah pinjaman harus antara Rp 1.000.000 dan Rp 50.000.000.")]
    public decimal LoanAmount { get; set; }

    [Required(ErrorMessage = "Tenor wajib diisi.")]
    public int TenorMonths { get; set; }
}

