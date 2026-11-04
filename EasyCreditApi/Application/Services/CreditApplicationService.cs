using EasyCreditApi.Application.DTO.Credit;
using EasyCreditApi.Application.DTO.Customer;
using EasyCreditApi.Application.Interfaces;
using EasyCreditApi.Domain.Entities;
using EasyCreditApi.Infrastructure;
using EasyCreditApi.Infrastructure.Oracle;
using EasyCreditApi.Middleware;

namespace EasyCreditApi.Application.Services;

public sealed class CreditApplicationService : ICreditApplicationService
{
    private static readonly int[] AllowedTenors = { 6, 12, 24, 36 };

    private readonly IOracleCreditApplicationRepository _oracleAppRepo;
    private readonly IOracleCreditInstallmentRepository _oracleInstallmentRepo;
    private readonly ICustomerRepository _customerRepository;

    public CreditApplicationService(
        IOracleCreditApplicationRepository oracleAppRepo,
        IOracleCreditInstallmentRepository oracleInstallmentRepo,
        ICustomerRepository customerRepository)
    {
        _oracleAppRepo = oracleAppRepo;
        _oracleInstallmentRepo = oracleInstallmentRepo;
        _customerRepository = customerRepository;
    }

    public async Task<(IReadOnlyList<ResCreditApplicationListItemDto> Items, int Total)> GetPagedAsync(
        string? status, string? keyword, int page, int size)
    {
        var (items, total) = await _oracleAppRepo.GetPagedAsync(status, page, size);

        var customerIds = items.Select(a => a.CustomerId).Distinct().ToList();
        var customers = await _customerRepository.GetByIdsAsync(customerIds);
        var customerMap = customers.ToDictionary(c => c.CustomerId, c => c.Name);

        var result = items.Select(app => new ResCreditApplicationListItemDto
        {
            ApplicationId = app.ApplicationId,
            CustomerId = app.CustomerId,
            CustomerName = customerMap.TryGetValue(app.CustomerId, out var name)
                ? name
                : $"Customer #{app.CustomerId}",
            LoanAmount = app.LoanAmount,
            TenorMonths = app.TenorMonths,
            InterestRate = app.InterestRate,
            CreditScore = app.CreditScore,
            Status = app.Status,
            ApplicationDate = app.ApplicationDate,
            ApprovedBy = app.ApprovedBy,
            ApprovedAt = app.ApprovedAt
        }).ToList();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var term = keyword.Trim();
            result = result.Where(r =>
                r.CustomerName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                r.ApplicationId.ToString().Contains(term, StringComparison.Ordinal)).ToList();
        }

        return (result, total);
    }

    public async Task<ResCreditApplicationDetailDto?> GetByIdAsync(int applicationId)
    {
        var app = await _oracleAppRepo.GetByIdAsync(applicationId);
        if (app is null) return null;

        var installments = await _oracleInstallmentRepo.GetByApplicationIdAsync(applicationId);
        var customer = await _customerRepository.GetByIdAsync(app.CustomerId);

        return new ResCreditApplicationDetailDto
        {
            ApplicationId = app.ApplicationId,
            CustomerId = app.CustomerId,
            Customer = customer is null ? null : new ResCustomerDto
            {
                CustomerId = customer.CustomerId,
                Name = customer.Name,
                Nik = customer.Nik,
                Phone = customer.Phone,
                Email = customer.Email,
                Address = customer.Address
            },
            LoanAmount = app.LoanAmount,
            TenorMonths = app.TenorMonths,
            InterestRate = app.InterestRate,
            CreditScore = app.CreditScore,
            Status = app.Status,
            ApplicationDate = app.ApplicationDate,
            ApprovedBy = app.ApprovedBy,
            ApprovedAt = app.ApprovedAt,
            Installments = installments.Select(i => new ResInstallmentDto
            {
                InstallmentNo = i.InstallmentNo,
                DueDate = i.DueDate,
                InstallmentAmount = i.InstallmentAmount,
                Status = i.Status
            }).ToList()
        };
    }

    public async Task<IReadOnlyList<ResInstallmentDto>> PreviewInstallmentAsync(decimal loanAmount, int tenorMonths)
    {
        ValidateLoanParameters(loanAmount, tenorMonths);

        var (items, status, message) = await _oracleAppRepo.CalculateInstallmentSpAsync(loanAmount, tenorMonths);

        if (status != "SUCCESS")
        {
            throw new ExceptionMiddleware.ValidationException(message);
        }

        return items.Select(i => new ResInstallmentDto
        {
            InstallmentNo = i.InstallmentNo,
            DueDate = i.DueDate,
            InstallmentAmount = i.InstallmentAmount,
            Status = "UNPAID"
        }).ToList();
    }

    public async Task<int> CreateAsync(ReqCreateCreditApplicationDto dto)
    {
        ValidateLoanParameters(dto.LoanAmount, dto.TenorMonths);

        // Validasi relasi ke PostgreSQL di service layer (tidak ada FK fisik lintas DB)
        var customerExists = await _customerRepository.ExistsAsync(dto.CustomerId);
        if (!customerExists)
        {
            throw new ExceptionMiddleware.ValidationException("Customer tidak ditemukan.");
        }

        // Hitung skema cicilan melalui SP
        var (installments, status, message) = await _oracleAppRepo.CalculateInstallmentSpAsync(dto.LoanAmount, dto.TenorMonths);
        if (status != "SUCCESS" || installments.Count == 0)
        {
            throw new ExceptionMiddleware.ValidationException(message);
        }

        var application = new CreditApplication
        {
            CustomerId = dto.CustomerId,
            LoanAmount = dto.LoanAmount,
            TenorMonths = dto.TenorMonths,
            InterestRate = 0.12m,
            CreditScore = 700,
            Status = "PENDING",
            ApplicationDate = DateTime.UtcNow
        };

        // Simpan header + semua installments dalam satu transaksi Oracle
        return await _oracleAppRepo.CreateWithInstallmentsAsync(application, installments);
    }

    public async Task<string> ApproveAsync(int applicationId, string approver)
    {
        var (status, message) = await _oracleAppRepo.ApproveOrRejectAsync(applicationId, "APPROVE", approver.Trim());

        return status switch
        {
            "SUCCESS" => message,
            "NOT_FOUND" => throw new ExceptionMiddleware.NotFoundException(message),
            "INVALID_STATUS" => throw new ExceptionMiddleware.ValidationException(message),
            _ => throw new InvalidOperationException(message)
        };
    }

    public async Task<string> RejectAsync(int applicationId, string approver)
    {
        var (status, message) = await _oracleAppRepo.ApproveOrRejectAsync(applicationId, "REJECT", approver.Trim());

        return status switch
        {
            "SUCCESS" => message,
            "NOT_FOUND" => throw new ExceptionMiddleware.NotFoundException(message),
            "INVALID_STATUS" => throw new ExceptionMiddleware.ValidationException(message),
            _ => throw new InvalidOperationException(message)
        };
    }

    private static void ValidateLoanParameters(decimal loanAmount, int tenorMonths)
    {
        if (loanAmount < 1_000_000m || loanAmount > 50_000_000m)
        {
            throw new ExceptionMiddleware.ValidationException("Jumlah pinjaman harus antara Rp 1.000.000 dan Rp 50.000.000.");
        }

        if (!AllowedTenors.Contains(tenorMonths))
        {
            throw new ExceptionMiddleware.ValidationException("Tenor harus 6, 12, 24, atau 36 bulan.");
        }
    }
}

