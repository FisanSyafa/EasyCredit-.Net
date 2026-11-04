using EasyCreditApi.Application.DTO;
using EasyCreditApi.Application.DTO.Credit;
using EasyCreditApi.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EasyCreditApi.Controllers;

[ApiController]
[Route("api/credit-applications")]
[Authorize]
public class CreditApplicationsController : ControllerBase
{
    private readonly ICreditApplicationService _service;

    public CreditApplicationsController(ICreditApplicationService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? status,
        [FromQuery] string? keyword,
        [FromQuery] int page = 1,
        [FromQuery] int size = 10)
    {
        page = Math.Max(page, 1);
        size = Math.Clamp(size, 1, 100);

        var (items, total) = await _service.GetPagedAsync(status, keyword, page, size);
        var data = new { page, size, total, items };

        return Ok(ApiResponse<object>.Success(data));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById([FromRoute] int id)
    {
        var application = await _service.GetByIdAsync(id);
        if (application is null)
        {
            return NotFound(ApiResponse<object>.Fail("Pengajuan kredit tidak ditemukan."));
        }

        return Ok(ApiResponse<ResCreditApplicationDetailDto>.Success(application));
    }

    [HttpPost("preview-installment")]
    public async Task<IActionResult> PreviewInstallment([FromBody] ReqPreviewInstallmentDto dto)
    {
        var schedule = await _service.PreviewInstallmentAsync(dto.LoanAmount, dto.TenorMonths);
        return Ok(ApiResponse<IReadOnlyList<ResInstallmentDto>>.Success(schedule, "Simulasi cicilan berhasil dihitung."));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ReqCreateCreditApplicationDto dto)
    {
        var applicationId = await _service.CreateAsync(dto);
        return CreatedAtAction(
            nameof(GetById),
            new { id = applicationId },
            ApiResponse<object>.Success(new { applicationId }, "Pengajuan kredit berhasil dibuat."));
    }

    [HttpPut("{id:int}/approve")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Approve([FromRoute] int id, [FromBody] ReqApproveApplicationDto dto)
    {
        var message = await _service.ApproveAsync(id, dto.Approver);
        return Ok(ApiResponse<object>.Success(
            new { applicationId = id, status = "APPROVED" },
            message));
    }

    [HttpPut("{id:int}/reject")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Reject([FromRoute] int id, [FromBody] ReqApproveApplicationDto dto)
    {
        var message = await _service.RejectAsync(id, dto.Approver);
        return Ok(ApiResponse<object>.Success(
            new { applicationId = id, status = "REJECTED" },
            message));
    }
}
