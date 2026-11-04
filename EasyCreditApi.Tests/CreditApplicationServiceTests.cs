using EasyCreditApi.Application.DTO.Credit;
using EasyCreditApi.Application.Services;
using EasyCreditApi.Domain.Entities;
using EasyCreditApi.Infrastructure;
using EasyCreditApi.Infrastructure.Oracle;
using EasyCreditApi.Middleware;
using Moq;
using Xunit;

namespace EasyCreditApi.Tests;

public class CreditApplicationServiceTests
{
    private readonly Mock<IOracleCreditApplicationRepository> _oracleAppRepoMock;
    private readonly Mock<IOracleCreditInstallmentRepository> _oracleInstallmentRepoMock;
    private readonly Mock<ICustomerRepository> _customerRepoMock;
    private readonly CreditApplicationService _sut;

    public CreditApplicationServiceTests()
    {
        _oracleAppRepoMock = new Mock<IOracleCreditApplicationRepository>();
        _oracleInstallmentRepoMock = new Mock<IOracleCreditInstallmentRepository>();
        _customerRepoMock = new Mock<ICustomerRepository>();

        _sut = new CreditApplicationService(
            _oracleAppRepoMock.Object,
            _oracleInstallmentRepoMock.Object,
            _customerRepoMock.Object);
    }

    [Theory]
    [InlineData(500_000, 12)]      // Di bawah Rp 1.000.000
    [InlineData(60_000_000, 12)]   // Di atas Rp 50.000.000
    public async Task PreviewInstallmentAsync_WhenLoanAmountOutOfRange_ThrowsValidationException(decimal amount, int tenor)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ExceptionMiddleware.ValidationException>(() =>
            _sut.PreviewInstallmentAsync(amount, tenor));
    }

    [Theory]
    [InlineData(5_000_000, 3)]     // Tenor bukan 6, 12, 24, 36
    [InlineData(5_000_000, 18)]
    [InlineData(5_000_000, 48)]
    public async Task PreviewInstallmentAsync_WhenTenorInvalid_ThrowsValidationException(decimal amount, int tenor)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ExceptionMiddleware.ValidationException>(() =>
            _sut.PreviewInstallmentAsync(amount, tenor));
    }

    [Fact]
    public async Task ApproveAsync_WhenStatusSuccess_ReturnsMessage()
    {
        // Arrange
        var appId = 1;
        var approver = "Admin User";
        _oracleAppRepoMock
            .Setup(r => r.ApproveOrRejectAsync(appId, "APPROVE", approver))
            .ReturnsAsync(("SUCCESS", "Pengajuan disetujui"));

        // Act
        var result = await _sut.ApproveAsync(appId, approver);

        // Assert
        Assert.Equal("Pengajuan disetujui", result);
        _oracleAppRepoMock.Verify(r => r.ApproveOrRejectAsync(appId, "APPROVE", approver), Times.Once);
    }

    [Fact]
    public async Task RejectAsync_WhenStatusSuccess_ReturnsMessage()
    {
        // Arrange
        var appId = 1;
        var approver = "Admin User";
        _oracleAppRepoMock
            .Setup(r => r.ApproveOrRejectAsync(appId, "REJECT", approver))
            .ReturnsAsync(("SUCCESS", "Pengajuan ditolak"));

        // Act
        var result = await _sut.RejectAsync(appId, approver);

        // Assert
        Assert.Equal("Pengajuan ditolak", result);
        _oracleAppRepoMock.Verify(r => r.ApproveOrRejectAsync(appId, "REJECT", approver), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenCustomerDoesNotExist_ThrowsValidationException()
    {
        // Arrange
        var dto = new ReqCreateCreditApplicationDto
        {
            CustomerId = 999,
            LoanAmount = 10_000_000m,
            TenorMonths = 12
        };

        _customerRepoMock.Setup(r => r.ExistsAsync(dto.CustomerId)).ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsAsync<ExceptionMiddleware.ValidationException>(() =>
            _sut.CreateAsync(dto));
    }
}
