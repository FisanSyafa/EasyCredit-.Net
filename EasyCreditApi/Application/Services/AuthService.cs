using System.Security.Claims;
using EasyCreditApi.Application.DTO.Auth;
using EasyCreditApi.Application.Interfaces;
using EasyCreditApi.Domain.Entities;

namespace EasyCreditApi.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public AuthService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<ResLoginDto?> LoginAsync(string email, string password)
    {
        var user = await _userRepository.GetByEmailAsync(email);
        if (user is null || !_passwordHasher.Verify(password, user.PasswordHash))
        {
            return null;
        }

        var (token, expiresAt) = _jwtTokenGenerator.GenerateToken(user.UserId, user.Email, user.Name, user.Role);
        var refreshToken = _jwtTokenGenerator.GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
        await _userRepository.UpdateAsync(user);

        return new ResLoginDto(token, user.Role, user.Name, expiresAt, refreshToken);
    }

    public async Task<ResLoginDto?> RefreshTokenAsync(string token, string refreshToken)
    {
        var principal = _jwtTokenGenerator.GetPrincipalFromExpiredToken(token);
        if (principal is null)
        {
            return null;
        }

        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? principal.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return null;
        }

        var user = await _userRepository.GetByIdAsync(userId);
        if (user is null || user.RefreshToken != refreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
        {
            return null;
        }

        var (newToken, newExpiresAt) = _jwtTokenGenerator.GenerateToken(user.UserId, user.Email, user.Name, user.Role);
        var newRefreshToken = _jwtTokenGenerator.GenerateRefreshToken();

        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7); // Sliding expiration
        await _userRepository.UpdateAsync(user);

        return new ResLoginDto(newToken, user.Role, user.Name, newExpiresAt, newRefreshToken);
    }

    public async Task<User> RegisterAsync(string email, string password, string name)
    {
        var existing = await _userRepository.GetByEmailAsync(email);
        if (existing is not null)
            throw new InvalidOperationException("Email already registered");

        var user = new User
        {
            Email = email,
            PasswordHash = _passwordHasher.Hash(password),
            Name = name,
            Role = "STAFF",
            CreatedAt = DateTime.UtcNow
        };

        await _userRepository.AddAsync(user);
        return user;
    }
}
