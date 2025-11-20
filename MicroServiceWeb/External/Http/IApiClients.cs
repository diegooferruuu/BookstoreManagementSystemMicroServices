using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Collections.Generic;
using System;

namespace MicroServiceWeb.External.Http;

public interface IProductsApiClient { }
public interface ISalesApiClient { }
public interface IUsersApiClient
{
    Task<UserDto?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<UserDto>> GetAllAsync(CancellationToken ct);
    Task<IReadOnlyList<UserFullDto>> GetAllRawAsync(CancellationToken ct);
    Task<IReadOnlyList<string>> GetRolesAsync(Guid id, CancellationToken ct);
    Task<AuthLoginResult> LoginAsync(AuthLoginRequest request, CancellationToken ct);
    Task<UserFullDto?> SearchAsync(string userOrEmail, CancellationToken ct);
    Task<UserApiResult> CreateAsync(UserCreateRequest dto, CancellationToken ct); // mantiene /api/User
    Task<UserApiResult> RegisterAsync(UserCreateRequest dto, CancellationToken ct); // nuevo /api/Auth/register
    Task<UserApiResult> UpdateAsync(Guid id, UserUpdateRequest dto, CancellationToken ct);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct);
}
public interface IClientsApiClient
{
    Task<ClientDto?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<ClientDto>> GetAllAsync(CancellationToken ct);
    Task<ClientApiResult> CreateAsync(ClientCreateDto dto, CancellationToken ct);
    Task<ClientApiResult> UpdateAsync(Guid id, ClientUpdateDto dto, CancellationToken ct);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct);
}
public interface IDistributorsApiClient
{
    Task<DistributorDto?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<DistributorDto>> GetAllAsync(CancellationToken ct);
    Task<DistributorApiResult> CreateAsync(DistributorCreateDto dto, CancellationToken ct);
    Task<DistributorApiResult> UpdateAsync(Guid id, DistributorUpdateDto dto, CancellationToken ct);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct);
}

public record UserDto(Guid Id, string Username, string? Email);
public record UserFullDto(Guid Id, string Username, string? Email, string? FirstName, string? MiddleName, string? LastName, bool MustChangePassword, List<string> Roles, string PasswordHash);
public record ClientDto(Guid Id, string FirstName, string LastName, string? Email, string? Phone, string? Address);
public record DistributorDto(Guid Id, string Name, string? ContactEmail, string? Phone, string? Address);

public class AuthLoginRequest { [Required] public string UserOrEmail { get; set; } = string.Empty; [Required] public string Password { get; set; } = string.Empty; }
public class AuthLoginResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string? Token { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? FirstName { get; set; }
    public string? MiddleName { get; set; }
    public string? LastName { get; set; }
    public bool MustChangePassword { get; set; }
    public List<string> Roles { get; set; } = new();
}

public class UserCreateRequest
{
    [Required(ErrorMessage="El correo es obligatorio."), EmailAddress(ErrorMessage="Correo inválido.")] public string Email { get; set; } = string.Empty;
    [Required(ErrorMessage="El rol es obligatorio.")] public string Role { get; set; } = string.Empty;
}
public class UserUpdateRequest { [Required, EmailAddress] public string Email { get; set; } = string.Empty; public List<string> Roles { get; set; } = new(); }
public class UserApiResult { public bool Success { get; set; } public UserFullDto? User { get; set; } public Dictionary<string, List<string>> Errors { get; set; } = new(); }

public class ClientCreateDto
{
    [Required(ErrorMessage = "El nombre es obligatorio."), MinLength(3, ErrorMessage = "Mínimo 3 caracteres."), MaxLength(100, ErrorMessage = "Máximo 100 caracteres."), Display(Name = "Nombre")] public string FirstName { get; set; } = string.Empty;
    [Required(ErrorMessage = "El apellido es obligatorio."), MinLength(3, ErrorMessage = "Mínimo 3 caracteres."), MaxLength(100, ErrorMessage = "Máximo 100 caracteres."), Display(Name = "Apellido")] public string LastName { get; set; } = string.Empty;
    [Required(ErrorMessage = "El correo electrónico es obligatorio."), EmailAddress(ErrorMessage = "Correo inválido."), MaxLength(150, ErrorMessage = "Máximo 150 caracteres."), Display(Name = "Correo electrónico")] public string? Email { get; set; }
    [Required(ErrorMessage = "El teléfono es obligatorio."), RegularExpression(@"^\d{8}$", ErrorMessage = "Debe tener 8 dígitos."), Display(Name = "Teléfono")] public string? Phone { get; set; }
    [Required(ErrorMessage = "La dirección es obligatoria."), MinLength(5, ErrorMessage = "Mínimo 5 caracteres."), MaxLength(255, ErrorMessage = "Máximo 255 caracteres."), Display(Name = "Dirección")] public string? Address { get; set; }
}
public class ClientUpdateDto : ClientCreateDto { }
public class ClientApiResult { public bool Success { get; set; } public ClientDto? Client { get; set; } public Dictionary<string, List<string>> Errors { get; set; } = new(); }

public class DistributorCreateDto
{
    [Required(ErrorMessage = "El nombre es obligatorio."), MinLength(3, ErrorMessage = "Mínimo 3 caracteres."), MaxLength(120, ErrorMessage = "Máximo 120 caracteres."), Display(Name = "Nombre")] public string Name { get; set; } = string.Empty;
    [Required(ErrorMessage = "El correo de contacto es obligatorio."), EmailAddress(ErrorMessage = "Correo inválido."), Display(Name = "Correo de contacto")] public string? ContactEmail { get; set; }
    [Required(ErrorMessage = "El teléfono es obligatorio."), RegularExpression(@"^\d{8}$", ErrorMessage = "Debe tener 8 dígitos."), Display(Name = "Teléfono")] public string? Phone { get; set; }
    [Required(ErrorMessage = "La dirección es obligatoria."), MinLength(5, ErrorMessage = "Mínimo 5 caracteres."), MaxLength(255, ErrorMessage = "Máximo 255 caracteres."), Display(Name = "Dirección")] public string? Address { get; set; }
}
public class DistributorUpdateDto : DistributorCreateDto { }
public class DistributorApiResult { public bool Success { get; set; } public DistributorDto? Distributor { get; set; } public Dictionary<string, List<string>> Errors { get; set; } = new(); }
