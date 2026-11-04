using EasyCreditApi.Application.Interfaces;
using EasyCreditApi.Domain.Entities;

namespace EasyCreditApi.Infrastructure;

public class InMemoryUserRepository : IUserRepository
{
    private readonly List<User> _users = new();

    public InMemoryUserRepository(IPasswordHasher hasher)
    {
        _users.Add(new User
        {
            UserId = 1,
            Name = "Admin",
            Email = "admin@mail.com",
            Role = "Admin",
            PasswordHash = hasher.Hash("Admin123!"),
            CreatedAt = DateTime.UtcNow
        });
    }

    public Task<User?> GetByEmailAsync(string email) =>
        Task.FromResult(_users.FirstOrDefault(u =>
            u.Email.Equals(email, StringComparison.OrdinalIgnoreCase)));

    public Task<User?> GetByIdAsync(int userId) =>
        Task.FromResult(_users.FirstOrDefault(u => u.UserId == userId));

    public Task AddAsync(User user)
    {
        user.UserId = _users.Count + 1;
        _users.Add(user);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(User user)
    {
        var idx = _users.FindIndex(u => u.UserId == user.UserId);
        if (idx >= 0)
        {
            _users[idx] = user;
        }
        return Task.CompletedTask;
    }
}
