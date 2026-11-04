using System.ComponentModel.DataAnnotations;
using EasyCreditFrontend.Models.Dtos;

namespace EasyCreditFrontend.Models.ViewModels;

public class CreateApplicationViewModel
{
    [Required(ErrorMessage = "Pilih customer.")]
    [Display(Name = "Customer")]
    public int CustomerId { get; set; }

    [Required(ErrorMessage = "Jumlah pinjaman wajib diisi.")]
    [Range(1000000, 50000000, ErrorMessage = "Pinjaman harus antara Rp 1.000.000 dan Rp 50.000.000.")]
    [Display(Name = "Jumlah Pinjaman (Rp)")]
    public decimal LoanAmount { get; set; } = 5000000;

    [Required(ErrorMessage = "Pilih tenor pinjaman.")]
    [Display(Name = "Tenor (Bulan)")]
    public int TenorMonths { get; set; } = 12;

    public List<CustomerDto> AvailableCustomers { get; set; } = new();
}

