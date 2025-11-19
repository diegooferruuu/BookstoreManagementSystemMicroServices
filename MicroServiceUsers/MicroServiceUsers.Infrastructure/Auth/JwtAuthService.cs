using MicroServiceUsers.Domain.Interfaces;
using MicroServiceUsers.Domain.Models;
using MicroServiceUsers.Domain.Results;
using MicroServiceUsers.Domain.Validations;
using Microsoft.AspNetCore.Identity;

namespace MicroServiceUsers.Infrastructure.Auth
{
    public class JwtAuthService : IJwtAuthService
    {
        private readonly IUserRepository _users;
        private readonly ITokenGenerator _tokens;
        private readonly JwtOptions _options;
        private readonly PasswordHasher<object> _hasher = new();

        public JwtAuthService(IUserRepository users, ITokenGenerator tokens, JwtOptions options)
        {
            _users = users;
            _tokens = tokens;
            _options = options;
        }

        public async Task<Result<object>> SignInAsync(string userOrEmail, string password, CancellationToken ct = default)
        {
            var input = (userOrEmail ?? string.Empty).Trim().ToLowerInvariant();
            
            // Intentar buscar por username o email
            var user = _users.GetByUsername(input) ?? _users.GetByEmail(input);

            if (user is null || !user.IsActive)
                return Result<object>.Fail(new ValidationError("Credentials", "Credenciales inválidas."));

            var verify = _hasher.VerifyHashedPassword(null!, user.PasswordHash, password ?? string.Empty);
            if (verify == PasswordVerificationResult.Failed)
                return Result<object>.Fail(new ValidationError("Credentials", "Credenciales inválidas."));

            if (user.MustChangePassword)
            {
                return Result<object>.Fail(new ValidationError("MustChangePassword", "Debe cambiar su contraseña antes de continuar."));
            }

            // Por ahora, asignamos roles básicos (esto se puede expandir con una tabla de roles)
            var roles = new List<string> { "User" };
            var now = DateTimeOffset.UtcNow;
            var jwt = _tokens.CreateToken(user, roles, now, _options);

            var tokenData = new
            {
                AccessToken = jwt,
                ExpiresAt = now.AddMinutes(_options.ExpiresMinutes),
                UserName = user.Username,
                Roles = roles.ToArray(),
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                MiddleName = user.MiddleName
            };

            return Result<object>.Ok(tokenData);
        }
    }
}
