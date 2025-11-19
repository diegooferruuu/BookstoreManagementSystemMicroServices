using Microsoft.AspNetCore.Identity;
using MicroServiceUsers.Application.DTOs;
using MicroServiceUsers.Domain.Interfaces;
using MicroServiceUsers.Domain.Models;

namespace MicroServiceUsers.Application.Facade
{
    public class UserFacade : IUserFacade
    {
        private readonly IUserService _users;
        private readonly IJwtAuthService _auth;
        private readonly IPasswordGenerator _pwdGen;
        private readonly IUsernameGenerator _unameGen;

        public UserFacade(
            IUserService users,
            IJwtAuthService auth,
            IPasswordGenerator pwdGen,
            IUsernameGenerator unameGen)
        {
            _users = users;
            _auth = auth;
            _pwdGen = pwdGen;
            _unameGen = unameGen;
        }

        public async Task<UserReadDto> CreateUserAsync(UserCreateDto dto, CancellationToken ct = default)
        {
            var baseUsername = _unameGen.GenerateUsernameFromEmail(dto.Email);
            var uniqueUsername = _unameGen.EnsureUniqueUsername(
                baseUsername,
                u => _users.GetAll().Any(x => x.Username.Equals(u, StringComparison.OrdinalIgnoreCase)));

            var plainPassword = _pwdGen.GenerateSecurePassword();

            var temp = new User();
            var hasher = new PasswordHasher<User>();
            var hash = hasher.HashPassword(temp, plainPassword);

            var user = new User
            {
                Email = dto.Email.Trim().ToLowerInvariant(),
                Username = uniqueUsername,
                FirstName = null,
                LastName = null,
                PasswordHash = hash,
                IsActive = true,
                MustChangePassword = true
            };

            _users.Create(user);

            // TODO: Enviar email con credenciales
            // var html = $"<p>Usuario: <b>{uniqueUsername}</b></p><p>Contraseña: <b>{plainPassword}</b></p>";
            // await _email.SendEmailAsync(dto.Email, "Credenciales de acceso", html, ct);

            return new UserReadDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Roles = new[] { dto.Role }
            };
        }

        public async Task<AuthTokenDto?> LoginAsync(AuthRequestDto req, CancellationToken ct = default)
        {
            var result = await _auth.SignInAsync(req.UserOrEmail, req.Password, ct);
            
            if (!result.IsSuccess || result.Value is null)
                return null;

            // Convertir el objeto anónimo a AuthTokenDto
            dynamic data = result.Value;
            return new AuthTokenDto
            {
                AccessToken = data.AccessToken,
                ExpiresAt = data.ExpiresAt,
                UserName = data.UserName,
                Roles = data.Roles,
                Email = data.Email,
                FirstName = data.FirstName,
                LastName = data.LastName,
                MiddleName = data.MiddleName
            };
        }

        public Task<IReadOnlyList<UserReadDto>> GetAllAsync(CancellationToken ct = default)
        {
            var list = _users.GetAll()
                .Select(u => new UserReadDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    Email = u.Email,
                    Roles = new[] { "User" } // Por ahora roles básicos
                })
                .ToList()
                .AsReadOnly();

            return Task.FromResult((IReadOnlyList<UserReadDto>)list);
        }
    }
}
