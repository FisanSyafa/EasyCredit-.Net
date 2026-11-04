using System.ComponentModel.DataAnnotations;

namespace EasyCreditFrontend.Models.ViewModels;

// ViewModel untuk form Create/Edit.
// Data Annotation di sini dipakai dua kali:
// 1) server-side oleh ModelState.IsValid,
// 2) client-side oleh jQuery Validation Unobtrusive (atribut data-val-*).
public class CustomerFormViewModel
{
    public int CustomerId { get; set; }

    [Required(ErrorMessage = "Nama wajib diisi.")]
    [StringLength(100, ErrorMessage = "Nama maksimal 100 karakter.")]
    [Display(Name = "Nama lengkap")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "NIK wajib diisi.")]
    [RegularExpression(@"^\d{16,20}$", ErrorMessage = "NIK harus 16-20 digit angka.")]
    [Display(Name = "NIK")]
    public string Nik { get; set; } = string.Empty;

    [StringLength(20, ErrorMessage = "Nomor telepon maksimal 20 karakter.")]
    [Phone(ErrorMessage = "Format nomor telepon tidak valid.")]
    [Display(Name = "No. telepon")]
    public string? Phone { get; set; }

    [EmailAddress(ErrorMessage = "Format email tidak valid.")]
    [StringLength(100, ErrorMessage = "Email maksimal 100 karakter.")]
    [Display(Name = "Email")]
    public string? Email { get; set; }

    [StringLength(100, ErrorMessage = "Alamat maksimal 100 karakter.")]
    [Display(Name = "Alamat")]
    public string? Address { get; set; }
}