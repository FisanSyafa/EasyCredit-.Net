namespace EasyCreditApi.Application.Interfaces;

public interface IJwtTokenGenerator
{
    (string Token, DateTime ExpiresAt) GenerateToken(
        int userId,
        string email,
        string name,
        string role);
}
