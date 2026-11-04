using EasyCreditApi.Application.DTO.Credit;
using EasyCreditApi.Application.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace EasyCreditApi.Infrastructure.Pdf;

public class CreditApplicationPdfService : ICreditApplicationPdfService
{
    public CreditApplicationPdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] GenerateInstallmentSchedulePdf(ResCreditApplicationDetailDto application)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                page.Header().Element(header =>
                {
                    header.Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("EASYCREDIT").FontSize(20).Bold().FontColor(Colors.Blue.Medium);
                            col.Item().Text("Simulasi & Skema Jadwal Angsuran Pinjaman").FontSize(11).FontColor(Colors.Grey.Medium);
                        });

                        row.ConstantItem(180).AlignRight().Column(col =>
                        {
                            col.Item().Text($"No. Pengajuan: #{application.ApplicationId}").Bold().FontColor(Colors.Grey.Darken2);
                            col.Item().Text($"Tanggal: {application.ApplicationDate:dd/MM/yyyy}").FontColor(Colors.Grey.Medium);
                            col.Item().Text($"Status: {application.Status}").Bold().FontColor(application.Status == "APPROVED" ? Colors.Green.Medium : Colors.Orange.Medium);
                        });
                    });
                });

                page.Content().PaddingVertical(1, Unit.Centimetre).Column(col =>
                {
                    // Section 1: Informasi Nasabah & Pinjaman
                    col.Item().Text("Informasi Nasabah & Pinjaman").FontSize(13).Bold().FontColor(Colors.Blue.Darken2);
                    col.Item().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);

                    col.Item().PaddingTop(8).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text($"Nama Nasabah: {application.Customer?.Name ?? "-"}").Bold();
                            c.Item().Text($"NIK: {application.Customer?.Nik ?? "-"}");
                            c.Item().Text($"Telepon: {application.Customer?.Phone ?? "-"}");
                            c.Item().Text($"Email: {application.Customer?.Email ?? "-"}");
                        });

                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text($"Jumlah Pinjaman: Rp {application.LoanAmount:N0}").Bold();
                            c.Item().Text($"Tenor: {application.TenorMonths} Bulan");
                            c.Item().Text($"Suku Bunga: {application.InterestRate * 100}% per tahun");
                            c.Item().Text($"Disetujui Oleh: {application.ApprovedBy ?? "-"}");
                        });
                    });

                    col.Item().PaddingTop(20).Text("Jadwal Angsuran Bulanan").FontSize(13).Bold().FontColor(Colors.Blue.Darken2);
                    col.Item().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);

                    // Tabel Angsuran
                    col.Item().PaddingTop(10).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(40);
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("No").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Tanggal Jatuh Tempo").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text("Jumlah Angsuran").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignCenter().Text("Status").Bold();
                        });

                        foreach (var inst in application.Installments)
                        {
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(inst.InstallmentNo.ToString());
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(inst.DueDate.ToString("dd MMM yyyy"));
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"Rp {inst.InstallmentAmount:N0}");
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignCenter().Text(inst.Status);
                        }
                    });

                    var totalInstallment = application.Installments.Sum(i => i.InstallmentAmount);
                    col.Item().PaddingTop(10).AlignRight().Text($"Total Pembayaran: Rp {totalInstallment:N0}").FontSize(11).Bold();
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Dokumen ini dihasilkan secara otomatis oleh EasyCredit System. Halaman ");
                    x.CurrentPageNumber();
                    x.Span(" dari ");
                    x.TotalPages();
                });
            });
        });

        return document.GeneratePdf();
    }
}
