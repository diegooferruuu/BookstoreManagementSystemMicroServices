using MicroServiceUsers.Domain.Results;

namespace MicroServiceUsers.Domain.Interfaces
{
    public interface IJwtAuthService
    {
        Task<Result<object>> SignInAsync(string userOrEmail, string password, CancellationToken ct = default);
    }
}
