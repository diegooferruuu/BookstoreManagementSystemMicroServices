using System.Globalization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Http;
using System.Linq;
using ServiceUsers.Domain.Interfaces;
using ServiceUsers.Application.Facade;
using ServiceProducts.Domain.Interfaces;
using ServiceProducts.Domain.Interfaces.Reports;
using ServiceClients.Domain.Interfaces;
using ServiceDistributors.Domain.Interfaces;
using ServiceSales.Domain.Interfaces;
using MicroServiceWeb.External.Http;
using System.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<AuthHeaderHandler>();

// === Registro de HttpClients para microservicios ===
builder.Services.AddHttpClient("ProductsService", c => c.BaseAddress = new Uri(builder.Configuration["Services:Products"] ?? "https://placeholder-products"));
builder.Services.AddHttpClient("SalesService", c => c.BaseAddress = new Uri(builder.Configuration["Services:Sales"] ?? "https://placeholder-sales"));
builder.Services.AddHttpClient("UsersService", c => c.BaseAddress = new Uri(builder.Configuration["Services:Users"] ?? "https://placeholder-users"))
                .AddHttpMessageHandler<AuthHeaderHandler>();
builder.Services.AddHttpClient("ClientsService", c => c.BaseAddress = new Uri(builder.Configuration["Services:Clients"] ?? "https://placeholder-clients"));
builder.Services.AddHttpClient("DistributorsService", c => c.BaseAddress = new Uri(builder.Configuration["Services:Distributors"] ?? "https://localhost:62293/"));

builder.Services.AddTransient<IProductsApiClient, ProductsApiClient>();
builder.Services.AddTransient<ISalesApiClient, SalesApiClient>();
builder.Services.AddTransient<IUsersApiClient, UsersApiClient>();
builder.Services.AddTransient<IClientsApiClient, ClientsApiClient>();
builder.Services.AddTransient<IDistributorsApiClient, DistributorsApiClient>();

builder.Services.AddSingleton<IUserService, StubUserService>();
builder.Services.AddSingleton<IJwtAuthService, StubJwtAuthService>();
builder.Services.AddSingleton<IUserFacade, StubUserFacade>();

builder.Services.AddSingleton<IProductRepository, StubProductRepository>();
builder.Services.AddSingleton<IProductService, StubProductService>();
builder.Services.AddSingleton<ICategoryRepository, StubCategoryRepository>();
builder.Services.AddSingleton<IProductReportService, StubProductReportService>();

builder.Services.AddSingleton<IClientService, StubClientService>();
builder.Services.AddSingleton<IDistributorService, StubDistributorService>();
builder.Services.AddSingleton<ISalesReportService, StubSalesReportService>();

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AllowAnonymousToPage("/Auth/Login");
    options.Conventions.AllowAnonymousToPage("/Auth/Logout");
    options.Conventions.AllowAnonymousToPage("/Users/ChangePassword");
    options.Conventions.AuthorizeFolder("/");
    options.Conventions.AuthorizePage("/Products/Index", "AdminOrEmployee");
    options.Conventions.AuthorizePage("/Products/Create", "AdminOnly");
    options.Conventions.AuthorizePage("/Products/Edit", "AdminOnly");
    options.Conventions.AuthorizePage("/Products/Delete", "AdminOnly");
    options.Conventions.AuthorizePage("/Distributors/Index", "AdminOrEmployee");
    options.Conventions.AuthorizePage("/Distributors/Create", "AdminOnly");
    options.Conventions.AuthorizePage("/Distributors/Edit", "AdminOnly");
    options.Conventions.AuthorizePage("/Distributors/Delete", "AdminOnly");
    options.Conventions.AuthorizeFolder("/Clients", "AdminOrEmployee");
    options.Conventions.AuthorizeFolder("/Users", "AdminOnly");
    options.Conventions.AuthorizePage("/Users/ChangePassword", "AdminOrEmployee");
    options.Conventions.AuthorizePage("/Index", "AdminOrEmployee");
    options.Conventions.AuthorizePage("/Error", "AdminOrEmployee");
})
.AddMvcOptions(options =>
{
    options.ModelBindingMessageProvider.SetValueMustNotBeNullAccessor(_ => "Debe seleccionar una categoría.");
    options.ModelBindingMessageProvider.SetAttemptedValueIsInvalidAccessor((x, y) => "El valor ingresado no es válido.");
    options.ModelBindingMessageProvider.SetMissingBindRequiredValueAccessor(x => $"Falta el valor para {x}.");
});

var cultureInfo = new CultureInfo("es-ES");
CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;
builder.Services.Configure<RequestLocalizationOptions>(options => { options.DefaultRequestCulture = new RequestCulture(cultureInfo); });

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.AccessDeniedPath = "/Auth/Login";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
    options.AddPolicy("EmployeeOnly", p => p.RequireRole("Employee"));
    options.AddPolicy("AdminOrEmployee", p => p.RequireRole("Admin", "Employee"));
    options.AddPolicy("RequireEmployeeOrAdmin", p => p.RequireRole("Admin", "Employee"));
});

builder.Services.AddControllersWithViews().AddDataAnnotationsLocalization();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();
app.Run();

// ===================== Implementaciones Stub =====================

public class StubUserService : IUserService
{
    private readonly List<ServiceUsers.Domain.Models.User> _users = new();
    public StubUserService()
    {
        _users.Add(new ServiceUsers.Domain.Models.User { Username = "admin", Email = "admin@test.local", Roles = new List<string>{"Admin"}, PasswordHash = "admin" });
        _users.Add(new ServiceUsers.Domain.Models.User { Username = "empleado", Email = "empleado@test.local", Roles = new List<string>{"Employee"}, PasswordHash = "empleado" });
    }
    public IEnumerable<ServiceUsers.Domain.Models.User> GetAll() => _users;
    public ServiceUsers.Domain.Models.User? Read(Guid id) => _users.FirstOrDefault(u => u.Id == id);
    public void Update(ServiceUsers.Domain.Models.User user) { }
    public void Delete(Guid id) { _users.RemoveAll(u => u.Id == id); }
    public List<string> GetUserRoles(Guid id) => _users.FirstOrDefault(u => u.Id == id)?.Roles ?? new List<string>();
    public void UpdateUserRoles(Guid id, List<string> roles)
    {
        var u = _users.FirstOrDefault(x => x.Id == id);
        if (u != null) u.Roles = roles;
    }
}

public class StubJwtAuthService : IJwtAuthService
{
    private readonly IUserService _userService;
    public StubJwtAuthService(IUserService userService) { _userService = userService; }
    public Task<ServiceUsers.Application.DTOs.SignInResult> SignInAsync(ServiceUsers.Application.DTOs.AuthRequestDto request, CancellationToken ct)
    {
        var usr = _userService.GetAll().FirstOrDefault(u => u.Username.Equals(request.UserOrEmail, StringComparison.OrdinalIgnoreCase));
        var result = new ServiceUsers.Application.DTOs.SignInResult();
        if (usr == null || usr.PasswordHash != request.Password)
        {
            result.Errors.Add(new ServiceUsers.Application.DTOs.AuthError { Field = "Credentials", Message = "Credenciales inválidas" });
            return Task.FromResult(result);
        }
        result.Value = new ServiceUsers.Application.DTOs.AuthResponseDto
        {
            UserName = usr.Username,
            Email = usr.Email,
            FirstName = usr.FirstName,
            MiddleName = usr.MiddleName,
            LastName = usr.LastName,
            Roles = usr.Roles,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(8)
        };
        if (usr.MustChangePassword)
            result.Errors.Add(new ServiceUsers.Application.DTOs.AuthError { Field = "MustChangePassword", Message = "Debe cambiar contraseña" });
        return Task.FromResult(result);
    }
}

public class StubUserFacade : IUserFacade
{
    private readonly IUserService _userService;
    public StubUserFacade(IUserService userService) { _userService = userService; }
    public Task CreateUserAsync(ServiceUsers.Application.DTOs.UserCreateDto dto, CancellationToken ct)
    {
        // No-op stub
        return Task.CompletedTask;
    }
    public Task<ServiceUsers.Application.DTOs.AuthResponseDto?> LoginAsync(ServiceUsers.Application.DTOs.AuthRequestDto dto, CancellationToken ct)
    {
        // Reusar stub auth service directamente no implementado aquí.
        return Task.FromResult<ServiceUsers.Application.DTOs.AuthResponseDto?>(null);
    }
}

// Productos
public interface IProductService
{
    List<ServiceProducts.Domain.Models.Product> GetAll();
    void Create(ServiceProducts.Domain.Models.Product p);
    void Delete(Guid id);
    ServiceProducts.Domain.Models.Product? Read(Guid id);
    void Update(ServiceProducts.Domain.Models.Product p);
}

public class StubProductRepository : IProductRepository
{
    private readonly List<ServiceProducts.Domain.Models.Product> _data = new();
    public IEnumerable<ServiceProducts.Domain.Models.Product> GetAll() => _data;
    public List<ServiceProducts.Domain.Models.Product> Internal => _data;
}

public class StubCategoryRepository : ICategoryRepository
{
    private readonly List<Category> _categories = new(){ new Category{ Id = Guid.NewGuid(), Name = "General" } };
    public IEnumerable<Category> GetAll() => _categories;
}

public class StubProductService : IProductService
{
    private readonly IProductRepository _repo; // usa la interfaz
    public StubProductService(IProductRepository repo) { _repo = repo; }
    public List<ServiceProducts.Domain.Models.Product> GetAll() => _repo.GetAll().ToList();
    public ServiceProducts.Domain.Models.Product? Read(Guid id) => _repo.GetAll().FirstOrDefault(p => p.Id == id);
    public void Create(ServiceProducts.Domain.Models.Product p)
    {
        if (_repo is StubProductRepository concrete)
        {
            p.Id = Guid.NewGuid();
            concrete.Internal.Add(p);
        }
    }
    public void Update(ServiceProducts.Domain.Models.Product p)
    {
        if (_repo is StubProductRepository concrete)
        {
            var idx = concrete.Internal.FindIndex(x => x.Id == p.Id);
            if (idx >= 0) concrete.Internal[idx] = p;
        }
    }
    public void Delete(Guid id)
    {
        if (_repo is StubProductRepository concrete)
        {
            concrete.Internal.RemoveAll(p => p.Id == id);
        }
    }
}

public class StubProductReportService : IProductReportService
{
    public Task<ServiceProducts.Application.DTOs.GeneratedReportDto?> GenerateAsync(ServiceProducts.Application.DTOs.ReportFilterDto filter, string format, string generatedBy, byte[]? logo, CancellationToken ct)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes($"Reporte generado por {generatedBy} con formato {format}");
        return Task.FromResult<ServiceProducts.Application.DTOs.GeneratedReportDto?>(new ServiceProducts.Application.DTOs.GeneratedReportDto
        {
            FileName = $"reporte_productos.{(format == "excel" ? "xlsx" : format)}",
            MimeType = format == "excel" ? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" : "application/pdf",
            Content = bytes
        });
    }
}

// Categorías y validación
public class Category { public Guid Id { get; set; } = Guid.NewGuid(); public string Name { get; set; } = string.Empty; }
public interface ICategoryRepository { IEnumerable<Category> GetAll(); }
public static class ProductValidation
{
    public static IEnumerable<ServiceCommon.Application.Services.ValidationError> Validate(ServiceProducts.Domain.Models.Product p, ICategoryRepository categoryRepository)
    {
        if (string.IsNullOrWhiteSpace(p.Name)) yield return new ServiceCommon.Application.Services.ValidationError { Field = nameof(p.Name), Message = "Nombre requerido" }; ;
        if (p.Price < 0) yield return new ServiceCommon.Application.Services.ValidationError { Field = nameof(p.Price), Message = "Precio inválido" }; ;
    }
}

// Clientes
public class StubClientService : IClientService
{
    private readonly List<ServiceClients.Domain.Models.Client> _clients = new();
    public IEnumerable<ServiceClients.Domain.Models.Client> GetAll() => _clients;
    public ServiceClients.Domain.Models.Client? Read(Guid id) => _clients.FirstOrDefault(c => c.Id == id);
    public void Update(ServiceClients.Domain.Models.Client client) { }
    public void Create(ServiceClients.Domain.Models.Client client) { client.Id = Guid.NewGuid(); _clients.Add(client); }
    public void Delete(Guid id) { _clients.RemoveAll(c => c.Id == id); }
}

// Distribuidores
public class StubDistributorService : IDistributorService
{
    private readonly List<ServiceDistributors.Domain.Models.Distributor> _dists = new();
    public IEnumerable<ServiceDistributors.Domain.Models.Distributor> GetAll() => _dists;
    public ServiceDistributors.Domain.Models.Distributor? Read(Guid id) => _dists.FirstOrDefault(d => d.Id == id);
    public void Update(ServiceDistributors.Domain.Models.Distributor distributor) { }
    public void Create(ServiceDistributors.Domain.Models.Distributor distributor) { distributor.Id = Guid.NewGuid(); _dists.Add(distributor); }
    public void Delete(Guid id) { _dists.RemoveAll(d => d.Id == id); }
}

// Ventas
public class StubSalesReportService : ISalesReportService
{
    public Task<byte[]> GenerateSalesReportAsync(ServiceSales.Domain.Models.SaleReportFilter filter, string reportType, string generatedBy)
    {
        var content = System.Text.Encoding.UTF8.GetBytes($"Reporte ventas {reportType} generado por {generatedBy}");
        return Task.FromResult(content);
    }
    public Task<string> GetReportContentType(string reportType) => Task.FromResult(reportType == "excel" ? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" : "application/pdf");
    public Task<string> GetReportFileExtension(string reportType) => Task.FromResult(reportType == "excel" ? ".xlsx" : ".pdf");
}

// Handler para adjuntar token JWT si existe
public class AuthHeaderHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _accessor;
    public AuthHeaderHandler(IHttpContextAccessor accessor) { _accessor = accessor; }
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var ctx = _accessor.HttpContext;
        var token = ctx?.User?.FindFirst("access_token")?.Value;
        if (!string.IsNullOrEmpty(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await base.SendAsync(request, cancellationToken);
    }
}
