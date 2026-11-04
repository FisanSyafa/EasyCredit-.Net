using EasyCreditApi.Application.DTO.Customer;
using EasyCreditApi.Application.Services;
using EasyCreditApi.Domain.Entities;
using EasyCreditApi.Infrastructure;
using EasyCreditApi.Middleware;
using Moq;
using Xunit;

namespace EasyCreditApi.Tests;

public class CustomerServiceTests
{
    private readonly Mock<ICustomerRepository> _customerRepoMock;
    private readonly CustomerService _sut;

    public CustomerServiceTests()
    {
        _customerRepoMock = new Mock<ICustomerRepository>();
        _sut = new CustomerService(_customerRepoMock.Object);
    }

    [Fact]
    public async Task CreateAsync_WhenNikAlreadyExists_ThrowsConflictException()
    {
        // Arrange
        var dto = new ReqCreateCustomerDto
        {
            Name = "Budi Santoso",
            Nik = "3171012345670001",
            Phone = "08123456789",
            Email = "budi@test.com",
            Address = "Jakarta"
        };
        _customerRepoMock.Setup(r => r.NikExistsAsync(dto.Nik)).ReturnsAsync(true);

        // Act & Assert
        await Assert.ThrowsAsync<ExceptionMiddleware.ConflictException>(() => _sut.CreateAsync(dto));
        _customerRepoMock.Verify(r => r.CreateAsync(It.IsAny<Customer>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenNikIsNew_CreatesAndReturnsCustomer()
    {
        // Arrange
        var dto = new ReqCreateCustomerDto
        {
            Name = "Budi Santoso",
            Nik = "3171012345670001",
            Phone = "08123456789",
            Email = "budi@test.com",
            Address = "Jakarta"
        };
        _customerRepoMock.Setup(r => r.NikExistsAsync(dto.Nik)).ReturnsAsync(false);
        _customerRepoMock.Setup(r => r.CreateAsync(It.IsAny<Customer>())).ReturnsAsync(1);

        // Act
        var result = await _sut.CreateAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(dto.Name, result.Name);
        Assert.Equal(dto.Nik, result.Nik);
        _customerRepoMock.Verify(r => r.CreateAsync(It.IsAny<Customer>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_CallsRepositorySoftDelete()
    {
        // Arrange
        var customerId = 10;
        _customerRepoMock.Setup(r => r.DeleteAsync(customerId)).ReturnsAsync(true);

        // Act
        var result = await _sut.DeleteAsync(customerId);

        // Assert
        Assert.True(result);
        _customerRepoMock.Verify(r => r.DeleteAsync(customerId), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WhenCustomerNotFound_ReturnsNull()
    {
        // Arrange
        _customerRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Customer?)null);

        // Act
        var result = await _sut.GetByIdAsync(99);

        // Assert
        Assert.Null(result);
    }
}
