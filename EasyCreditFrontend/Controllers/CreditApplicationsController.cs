using EasyCreditFrontend.Models;
using EasyCreditFrontend.Models.Dtos;
using EasyCreditFrontend.Models.ViewModels;
using EasyCreditFrontend.Services;
using Microsoft.AspNetCore.Mvc;

namespace EasyCreditFrontend.Controllers;

public class CreditApplicationsController : BaseController
{
    private const int DefaultPageSize = 10;
    private readonly ApiClient _apiClient;

    public CreditApplicationsController(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? status,
        string? keyword,
        int page = 1,
        int size = DefaultPageSize)
    {
        page = Math.Max(page, 1);
        size = Math.Clamp(size, 1, 100);

        var url = $"/api/credit-applications?status={Uri.EscapeDataString(status ?? string.Empty)}" +
                  $"&keyword={Uri.EscapeDataString(keyword ?? string.Empty)}" +
                  $"&page={page}&size={size}";

        var response = await _apiClient.GetAsync<PagedData<ApplicationListItemDto>>(url);
        var data = response?.Data;

        var items = (data?.Items ?? new List<ApplicationListItemDto>())
            .Select(app => new ApplicationRowViewModel
            {
                ApplicationId = app.ApplicationId,
                CustomerId = app.CustomerId,
                CustomerName = app.CustomerName,
                Amount = app.LoanAmount,
                TenorMonths = app.TenorMonths,
                Status = app.Status,
                ApplicationDate = app.ApplicationDate,
                ApprovedBy = app.ApprovedBy,
                ApprovedAt = app.ApprovedAt
            }).ToList();

        var total = data?.Total ?? items.Count;
        var totalPages = total == 0 ? 1 : (int)Math.Ceiling(total / (double)size);

        if (response is { IsSuccess: false })
        {
            TempData["Error"] = response.Message;
        }

        var model = new ApplicationIndexViewModel
        {
            Items = items,
            Status = status,
            Keyword = keyword,
            Page = page,
            Size = size,
            Total = total,
            TotalPages = totalPages
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Detail(int id)
    {
        var response = await _apiClient.GetAsync<ApplicationDetailDto>($"/api/credit-applications/{id}");
        if (response?.Data is null)
        {
            TempData["Error"] = response?.Message ?? "Pengajuan kredit tidak ditemukan.";
            return RedirectToAction(nameof(Index));
        }

        var model = new ApplicationDetailViewModel
        {
            Application = response.Data
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> DownloadPdf(int id)
    {
        var (bytes, contentType, success) = await _apiClient.GetRawBytesAsync($"/api/credit-applications/{id}/pdf");
        if (!success || bytes is null)
        {
            TempData["Error"] = "Gagal mengunduh berkas PDF skema cicilan.";
            return RedirectToAction(nameof(Detail), new { id });
        }

        return File(bytes, contentType ?? "application/pdf", $"skema-cicilan-#{id}.pdf");
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new CreateApplicationViewModel
        {
            AvailableCustomers = await LoadCustomersAsync()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateApplicationViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.AvailableCustomers = await LoadCustomersAsync();
            return View(model);
        }

        var response = await _apiClient.PostAsync<object>("/api/credit-applications", new
        {
            customerId = model.CustomerId,
            loanAmount = model.LoanAmount,
            tenorMonths = model.TenorMonths
        });

        if (response is null || !response.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, response?.Message ?? "Gagal membuat pengajuan kredit.");
            model.AvailableCustomers = await LoadCustomersAsync();
            return View(model);
        }

        TempData["Success"] = response.Message ?? "Pengajuan kredit berhasil dibuat dan menunggu approval.";
        return RedirectToAction(nameof(Index));
    }

    // Endpoint AJAX preview cicilan
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PreviewInstallment(decimal loanAmount, int tenorMonths)
    {
        var response = await _apiClient.PostAsync<List<InstallmentDto>>(
            "/api/credit-applications/preview-installment",
            new { loanAmount, tenorMonths });

        if (response is null || !response.IsSuccess)
        {
            return Json(new { status = false, message = response?.Message ?? "Gagal menghitung simulasi cicilan." });
        }

        return Json(new { status = true, data = response.Data, message = response.Message });
    }

    // Endpoint AJAX approve pengajuan (ADMIN only)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(ApproveApplicationViewModel model)
    {
        if (!IsAdmin)
        {
            return Json(new { status = false, message = "Akses ditolak: Hanya ADMIN yang dapat menyetujui pengajuan." });
        }

        if (!ModelState.IsValid)
        {
            var message = string.Join(" ", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage));

            return Json(new { status = false, message });
        }

        var response = await _apiClient.PutAsync<object>(
            $"/api/credit-applications/{model.ApplicationId}/approve",
            new { approver = model.Approver });

        if (response is null || !response.IsSuccess)
        {
            return Json(new
            {
                status = false,
                message = response?.Message ?? "Approve gagal."
            });
        }

        return Json(new
        {
            status = true,
            message = response.Message,
            applicationId = model.ApplicationId,
            newStatus = "APPROVED",
            approver = model.Approver
        });
    }

    // Endpoint AJAX reject pengajuan (ADMIN only)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(RejectApplicationViewModel model)
    {
        if (!IsAdmin)
        {
            return Json(new { status = false, message = "Akses ditolak: Hanya ADMIN yang dapat menolak pengajuan." });
        }

        if (!ModelState.IsValid)
        {
            var message = string.Join(" ", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage));

            return Json(new { status = false, message });
        }

        var response = await _apiClient.PutAsync<object>(
            $"/api/credit-applications/{model.ApplicationId}/reject",
            new { approver = model.Approver });

        if (response is null || !response.IsSuccess)
        {
            return Json(new
            {
                status = false,
                message = response?.Message ?? "Reject gagal."
            });
        }

        return Json(new
        {
            status = true,
            message = response.Message,
            applicationId = model.ApplicationId,
            newStatus = "REJECTED",
            approver = model.Approver
        });
    }

    private async Task<List<CustomerDto>> LoadCustomersAsync()
    {
        var response = await _apiClient.GetAsync<PagedData<CustomerDto>>("/api/customers?page=1&size=100");
        return response?.Data?.Items ?? new List<CustomerDto>();
    }
}
