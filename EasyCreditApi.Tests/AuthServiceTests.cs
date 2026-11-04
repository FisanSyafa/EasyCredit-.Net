using System.Security.Claims;
using EasyCreditApi.Application.Interfaces;
using EasyCreditApi.Application.Services;
using EasyCreditApi.Domain.Entities;
using Moq;
using Xunit;

namespace EasyCreditApi.Tests;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly Mock<IPasswordHasher> _hasherMock;
    private readonly Mock<IJwtTokenGenerator> _jwtMock;
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _userRepoMock = new Mock<IUserRepository>();
        _hasherMock = new Mock<IPasswordHasher>();
        _jwtMock = new Mock<IJwtTokenGenerator>();

        _sut = new AuthService(
            _userRepoMock.Object,
            _hasherMock.Object,
            _jwtMock.Object);
    }

    [Fact]
    public async Task LoginAsync_WhenUserNotFound_ReturnsNull()
    {
        // Arrange
        _userRepoMock.Setup(r => r.GetByEmailAsync("notfound@test.com")).ReturnsAsync((User?)null);

        // Act
        var result = await _sut.LoginAsync("notfound@test.com", "anypassword");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task LoginAsync_WhenPasswordInvalid_ReturnsNull()
    {
        // Arrange
        var user = new User { UserId = 1, Email = "user@test.com", PasswordHash = "hashed" };
        _userRepoMock.Setup(r => r.GetByEmailAsync(user.Email)).ReturnsAsync(user);
        _hasherMock.Setup(h => h.Verify("wrongpass", "hashed")).Returns(false);

        // Act
        var result = await _sut.LoginAsync(user.Email, "wrongpass");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task LoginAsync_WhenValid_ReturnsTokenAndRefreshToken()
    {
        // Arrange
        var user = new User
        {
            UserId = 1,
            Email = "admin@test.com",
            Name = "Admin",
            Role = "ADMIN",
            PasswordHash = "correcthash"
        };
        _userRepoMock.Setup(r => r.GetByEmailAsync(user.Email)).ReturnsAsync(user);
        _hasherMock.Setup(h => h.Verify("pass123", "correcthash")).Returns(true);
        _jwtMock.Setup(j => j.GenerateToken(user.UserId, user.Email, user.Name, user.Role))
            .Returns(("jwt_token_123", DateTime.UtcNow.AddHours(1)));
        _jwtMock.Setup(j => j.GenerateRefreshToken()).Returns("refresh_token_abc");

        // Act
        var result = await _sut.LoginAsync(user.Email, "pass123");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("jwt_token_123", result.AccessToken);
        Assert.Equal("refresh_token_abc", result.RefreshToken);
        Assert.Equal("ADMIN", result.Role);
        _userRepoMock.Verify(r => r.UpdateAsync(It.Is<User>(u => u.RefreshToken == "refresh_token_abc")), Times.Once);
    }

    [Fact]
    public async Task RefreshTokenAsync_WhenTokenInvalid_ReturnsNull()
    {
        // Arrange
        _jwtMock.Setup(j => j.GetPrincipalFromExpiredToken("invalid_jwt")).Returns((ClaimsPrincipal?)null);

        // Act
        var result = await _sut.RefreshTokenAsync("invalid_jwt", "some_refresh");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task RefreshTokenAsync_WhenRefreshTokenMatches_ReturnsNewTokens()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Email, "admin@test.com")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var user = new User
        {
            UserId = 1,
            Email = "admin@test.com",
            Name = "Admin",
            Role = "ADMIN",
            RefreshToken = "existing_refresh",
            RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(2)
        };

        _jwtMock.Setup(j => j.GetPrincipalFromExpiredToken("old_jwt")).Returns(principal);
        _userRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
        _jwtMock.Setup(j => j.GenerateToken(user.UserId, user.Email, user.Name, user.Role))
            .Returns(("new_jwt_456", DateTime.UtcNow.AddHours(1)));
        _jwtMock.Setup(j => j.GenerateRefreshToken()).Returns("new_refresh_xyz");

        // Act
        var result = await _sut.RefreshTokenAsync("old_jwt", "existing_refresh");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("new_jwt_456", result.AccessToken);
        Assert.Equal("new_refresh_xyz", result.RefreshToken);
        _userRepoMock.Verify(r => r.UpdateAsync(It.Is<User>(u => u.RefreshToken == "new_refresh_xyz")), Times.Once);
    }
}
