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
            var allUsers = await _users.GetAllAsync(ct);
            var uniqueUsername = _unameGen.EnsureUniqueUsername(
                baseUsername,
                u => allUsers.Any(x => x.Username.Equals(u, StringComparison.OrdinalIgnoreCase)));

            var plainPassword = _pwdGen.GenerateSecurePassword();

            var temp = new User();
            var hasher = new PasswordHasher<User>();
            var hash = hasher.HashPassword(temp, plainPassword);

            var user = new User
            {
                Email = dto.Email.Trim().ToLowerInvariant(),
                Username = uniqueUsername,
                FirstName = string.Empty,
                LastName = string.Empty,
                PasswordHash = hash,
                IsActive = true,
                MustChangePassword = true
            };

            var roles = new List<string> { dto.Role };
            await _users.CreateAsync(user, plainPassword, roles, ct);

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

        public async Task<IReadOnlyList<UserReadDto>> GetAllAsync(CancellationToken ct = default)
        {
            var allUsers = await _users.GetAllAsync(ct);
            var result = new List<UserReadDto>();

            foreach (var u in allUsers)
            {
                var roles = await _users.GetRolesAsync(u.Id, ct);
                result.Add(new UserReadDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    Email = u.Email,
                    Roles = roles.ToArray()
                });
            }

            return result.AsReadOnly();
        }
    }
}
