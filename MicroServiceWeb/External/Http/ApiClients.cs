using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.Http.Headers;

namespace MicroServiceWeb.External.Http
{
    public class ProductsApiClient : IProductsApiClient
    {
        private readonly HttpClient _http;
        public ProductsApiClient(IHttpClientFactory f)=>_http=f.CreateClient("ProductsService");
        public async Task<IReadOnlyList<ProductDto>> GetAllAsync(CancellationToken ct)
            => await _http.GetFromJsonAsync<IReadOnlyList<ProductDto>>("api/products", ct) ?? Array.Empty<ProductDto>();
        public async Task<PagedResult<ProductDto>> GetPagedAsync(int page, int pageSize, CancellationToken ct)
        {
            // Llama al endpoint paginado: api/products/paged?page={page}&pageSize={pageSize}
            var url = $"api/products/paged?page={page}&pageSize={pageSize}";
            var resp = await _http.GetAsync(url, ct);
            if (!resp.IsSuccessStatusCode)
            {
                return new PagedResult<ProductDto>(new List<ProductDto>(), page, pageSize, 0, 0);
            }
            try
            {
                var json = await resp.Content.ReadAsStringAsync(ct);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                // Se espera estructura: { items: [...], page: n, pageSize: n, totalItems: n, totalPages: n }
                var items = new List<ProductDto>();
                if (root.TryGetProperty("items", out var itemsProp) && itemsProp.ValueKind == JsonValueKind.Array)
                {
                    foreach (var el in itemsProp.EnumerateArray())
                    {
                        try { var dto = System.Text.Json.JsonSerializer.Deserialize<ProductDto>(el.GetRawText(), options); if (dto != null) items.Add(dto); } catch { }
                    }
                }
                int totalItems = root.TryGetProperty("totalItems", out var ti) && ti.TryGetInt32(out var tiVal) ? tiVal : items.Count;
                int totalPages = root.TryGetProperty("totalPages", out var tp) && tp.TryGetInt32(out var tpVal) ? tpVal : (int)Math.Ceiling((double)totalItems / pageSize);
                int currentPage = root.TryGetProperty("page", out var pg) && pg.TryGetInt32(out var pgVal) ? pgVal : page;
                int currentPageSize = root.TryGetProperty("pageSize", out var ps) && ps.TryGetInt32(out var psVal) ? psVal : pageSize;
                return new PagedResult<ProductDto>(items, currentPage, currentPageSize, totalItems, totalPages);
            }
            catch
            {
                return new PagedResult<ProductDto>(new List<ProductDto>(), page, pageSize, 0, 0);
            }
        }
        public async Task<ProductDto?> GetByIdAsync(Guid id, CancellationToken ct)
            => await _http.GetFromJsonAsync<ProductDto>($"api/products/{id}", ct);
        public async Task<ProductApiResult> CreateAsync(ProductCreateDto dto, CancellationToken ct)
        { var resp = await _http.PostAsJsonAsync("api/products", dto, ct); return await ParseProductResult(resp, ct); }
        public async Task<ProductApiResult> UpdateAsync(Guid id, ProductUpdateDto dto, CancellationToken ct)
        { var resp = await _http.PutAsJsonAsync($"api/products/{id}", dto, ct); return await ParseProductResult(resp, ct); }
        public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
        { var resp = await _http.DeleteAsync($"api/products/{id}", ct); return resp.IsSuccessStatusCode; }
        public async Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken ct)
            => await _http.GetFromJsonAsync<IReadOnlyList<CategoryDto>>("api/categories", ct) ?? Array.Empty<CategoryDto>();
        private static async Task<ProductApiResult> ParseProductResult(HttpResponseMessage resp, CancellationToken ct)
        {
            var result = new ProductApiResult { Success = resp.IsSuccessStatusCode };
            if (resp.Content.Headers.ContentType?.MediaType == "application/json")
            {
                var json = await resp.Content.ReadAsStringAsync(ct);
                try
                {
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;
                    if (resp.IsSuccessStatusCode && root.ValueKind == JsonValueKind.Object)
                    { result.Product = System.Text.Json.JsonSerializer.Deserialize<ProductDto>(root.GetRawText()); }
                    else if (root.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var prop in root.EnumerateObject())
                        {
                            if (prop.Value.ValueKind == JsonValueKind.Array)
                            { var list = new List<string>(); foreach (var item in prop.Value.EnumerateArray()) list.Add(item.GetString() ?? "Error"); result.Errors[prop.Name] = list; }
                            else if (prop.Value.ValueKind == JsonValueKind.String)
                            { result.Errors[prop.Name] = new List<string> { prop.Value.GetString() ?? "Error" }; }
                        }
                    }
                }
                catch { }
            }
            return result;
        }
    }

    public class SalesApiClient : ISalesApiClient { private readonly HttpClient _http; public SalesApiClient(IHttpClientFactory f)=>_http=f.CreateClient("SalesService"); }

    public class UsersApiClient : IUsersApiClient
    {
        private readonly HttpClient _http;
        public UsersApiClient(IHttpClientFactory factory) => _http = factory.CreateClient("UsersService");
        public async Task<UserDto?> GetByIdAsync(Guid id, CancellationToken ct) => await _http.GetFromJsonAsync<UserDto>($"api/User/{id}", ct);
        public async Task<IReadOnlyList<UserDto>> GetAllAsync(CancellationToken ct) => await _http.GetFromJsonAsync<IReadOnlyList<UserDto>>("api/User", ct) ?? Array.Empty<UserDto>();
        public async Task<IReadOnlyList<UserFullDto>> GetAllRawAsync(CancellationToken ct) => await _http.GetFromJsonAsync<IReadOnlyList<UserFullDto>>("api/User", ct) ?? Array.Empty<UserFullDto>();
        public async Task<IReadOnlyList<string>> GetRolesAsync(Guid id, CancellationToken ct) => await _http.GetFromJsonAsync<IReadOnlyList<string>>($"api/User/{id}/roles", ct) ?? Array.Empty<string>();
        public async Task<UserFullDto?> SearchAsync(string userOrEmail, CancellationToken ct) => await _http.GetFromJsonAsync<UserFullDto>($"api/User/search/{Uri.EscapeDataString(userOrEmail)}", ct);
        public async Task<AuthLoginResult> LoginAsync(AuthLoginRequest request, CancellationToken ct) { var resp = await _http.PostAsJsonAsync("api/Auth/login", request, ct); return await ParseAuthResponse(resp, ct); }
        public async Task<UserApiResult> CreateAsync(UserCreateRequest dto, CancellationToken ct) { var resp = await _http.PostAsJsonAsync("api/User", dto, ct); return await ParseUserResult(resp, ct); }
        public async Task<UserApiResult> RegisterAsync(UserCreateRequest dto, CancellationToken ct) { var resp = await _http.PostAsJsonAsync("api/Auth/register", dto, ct); return await ParseUserResult(resp, ct); }
        public async Task<UserApiResult> UpdateAsync(Guid id, UserUpdateRequest dto, CancellationToken ct) { var resp = await _http.PutAsJsonAsync($"api/User/{id}", dto, ct); return await ParseUserResult(resp, ct); }
        public async Task<bool> DeleteAsync(Guid id, CancellationToken ct) { var resp = await _http.DeleteAsync($"api/User/{id}", ct); return resp.IsSuccessStatusCode; }
        public async Task<ApiSimpleResult> ChangePasswordAsync(ChangePasswordRequest dto, CancellationToken ct)
        { return await ChangePasswordAsync(dto, null, ct); }
        public async Task<ApiSimpleResult> ChangePasswordAsync(ChangePasswordRequest dto, string? bearerToken, CancellationToken ct)
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, "api/Auth/change-password") { Content = JsonContent.Create(dto) };
            if (!string.IsNullOrWhiteSpace(bearerToken)) req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
            var resp = await _http.SendAsync(req, ct);
            if (resp.IsSuccessStatusCode) return new ApiSimpleResult { Success = true };
            var text = await resp.Content.ReadAsStringAsync(ct);
            try
            {
                using var doc = JsonDocument.Parse(text);
                var root = doc.RootElement;
                var msg = root.TryGetProperty("message", out var m) ? m.GetString() : "Error al cambiar contraseña.";
                return new ApiSimpleResult { Success = false, Error = msg };
            }
            catch { return new ApiSimpleResult { Success = false, Error = "Error al cambiar contraseña." }; }
        }

        private static async Task<AuthLoginResult> ParseAuthResponse(HttpResponseMessage resp, CancellationToken ct)
        {
            var result = new AuthLoginResult();
            var status = resp.StatusCode;
            if (resp.Content.Headers.ContentType?.MediaType == "application/json")
            {
                var json = await resp.Content.ReadAsStringAsync(ct);
                try
                {
                    using var doc = JsonDocument.Parse(json); var root = doc.RootElement;
                    if (resp.IsSuccessStatusCode && root.ValueKind == JsonValueKind.Object)
                    {
                        result.Success = true;
                        // Soportar 'accessToken' (backend actual) y 'token' (fallback)
                        if (root.TryGetProperty("accessToken", out var accessToken)) result.Token = accessToken.GetString();
                        else if (root.TryGetProperty("token", out var token)) result.Token = token.GetString();
                        if (root.TryGetProperty("expiresAt", out var exp) && exp.ValueKind == JsonValueKind.String && DateTimeOffset.TryParse(exp.GetString(), out var dtoExp)) result.ExpiresAt = dtoExp;
                        if (root.TryGetProperty("userName", out var uname)) result.UserName = uname.GetString() ?? string.Empty;
                        else if (root.TryGetProperty("user", out var u) && u.ValueKind == JsonValueKind.String) result.UserName = u.GetString() ?? string.Empty;
                        if (root.TryGetProperty("email", out var em)) result.Email = em.GetString();
                        if (root.TryGetProperty("firstName", out var fn) && fn.ValueKind == JsonValueKind.String) result.FirstName = fn.GetString();
                        if (root.TryGetProperty("middleName", out var mn) && mn.ValueKind == JsonValueKind.String) result.MiddleName = mn.GetString();
                        if (root.TryGetProperty("lastName", out var ln) && ln.ValueKind == JsonValueKind.String) result.LastName = ln.GetString();
                        if (root.TryGetProperty("roles", out var roles) && roles.ValueKind == JsonValueKind.Array)
                            foreach (var r in roles.EnumerateArray()) if (r.ValueKind == JsonValueKind.String) result.Roles.Add(r.GetString()!);
                        if (root.TryGetProperty("mustChangePassword", out var mcp)) result.MustChangePassword = mcp.GetBoolean();
                    }
                    else
                    {
                        string? msg = null;
                        foreach (var name in new[] { "message", "Message", "error", "title" })
                        {
                            if (root.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.String)
                            { msg = prop.GetString(); break; }
                        }
                        if (string.IsNullOrWhiteSpace(msg))
                        {
                            msg = status switch
                            {
                                System.Net.HttpStatusCode.Unauthorized => "Credenciales inválidas o usuario inactivo.",
                                System.Net.HttpStatusCode.Forbidden => "Acceso denegado.",
                                _ => "Error al iniciar sesión."
                            };
                        }
                        result.Error = msg;
                        result.Success = false;
                    }
                }
                catch
                {
                    result.Success = false; result.Error = "Error al procesar la respuesta del servidor.";
                }
            }
            else
            {
                result.Success = resp.IsSuccessStatusCode;
                if (!result.Success)
                {
                    result.Error = status switch
                    {
                        System.Net.HttpStatusCode.Unauthorized => "Credenciales inválidas o usuario inactivo.",
                        System.Net.HttpStatusCode.Forbidden => "Acceso denegado.",
                        _ => "Error en la autenticación."
                    };
                }
            }
            return result;
        }
        private static async Task<UserApiResult> ParseUserResult(HttpResponseMessage resp, CancellationToken ct)
        {
            var result = new UserApiResult { Success = resp.IsSuccessStatusCode };
            if (resp.Content.Headers.ContentType?.MediaType == "application/json")
            {
                var json = await resp.Content.ReadAsStringAsync(ct);
                try
                {
                    using var doc = JsonDocument.Parse(json); var root = doc.RootElement;
                    if (resp.IsSuccessStatusCode && root.ValueKind == JsonValueKind.Object)
                    {
                        var id = root.TryGetProperty("id", out var idProp) && Guid.TryParse(idProp.GetString(), out var gid) ? gid : Guid.Empty;
                        var username = root.TryGetProperty("username", out var unProp) ? unProp.GetString() : null;
                        var email = root.TryGetProperty("email", out var emProp) ? emProp.GetString() : null;
                        var roles = new List<string>();
                        if (root.TryGetProperty("roles", out var rlProp) && rlProp.ValueKind == JsonValueKind.Array)
                            foreach (var r in rlProp.EnumerateArray()) if (r.ValueKind == JsonValueKind.String) roles.Add(r.GetString()!);
                        result.User = new UserFullDto(id, username ?? string.Empty, email, null, null, null, false, roles, string.Empty);
                    }
                    else if (root.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var prop in root.EnumerateObject())
                        {
                            if (prop.Value.ValueKind == JsonValueKind.Array)
                            {
                                var list = new List<string>(); foreach (var item in prop.Value.EnumerateArray()) list.Add(item.GetString() ?? "Error");
                                result.Errors[prop.Name] = list;
                            }
                        }
                    }
                }
                catch { }
            }
            return result;
        }
    }

    public class ClientsApiClient : IClientsApiClient
    {
        private readonly HttpClient _http; public ClientsApiClient(IHttpClientFactory f)=>_http=f.CreateClient("ClientsService");
        public async Task<ClientDto?> GetByIdAsync(Guid id, CancellationToken ct) => await _http.GetFromJsonAsync<ClientDto>($"api/Client/{id}", ct);
        public async Task<IReadOnlyList<ClientDto>> GetAllAsync(CancellationToken ct) => await _http.GetFromJsonAsync<IReadOnlyList<ClientDto>>("api/Client", ct) ?? Array.Empty<ClientDto>();
        public async Task<ClientApiResult> CreateAsync(ClientCreateDto dto, CancellationToken ct) { var resp = await _http.PostAsJsonAsync("api/Client", dto, ct); return await BuildResult(resp, ct); }
        public async Task<ClientApiResult> UpdateAsync(Guid id, ClientUpdateDto dto, CancellationToken ct) { var resp = await _http.PutAsJsonAsync($"api/Client/{id}", dto, ct); return await BuildResult(resp, ct); }
        public async Task<bool> DeleteAsync(Guid id, CancellationToken ct) { var resp = await _http.DeleteAsync($"api/Client/{id}", ct); return resp.IsSuccessStatusCode; }
        private static async Task<ClientApiResult> BuildResult(HttpResponseMessage resp, CancellationToken ct)
        {
            var result = new ClientApiResult { Success = resp.IsSuccessStatusCode };
            if (resp.Content.Headers.ContentType?.MediaType == "application/json")
            {
                try
                {
                    using var stream = await resp.Content.ReadAsStreamAsync(ct);
                    using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
                    if (resp.IsSuccessStatusCode)
                    {
                        if (doc.RootElement.ValueKind == JsonValueKind.Object && doc.RootElement.TryGetProperty("id", out _))
                            result.Client = System.Text.Json.JsonSerializer.Deserialize<ClientDto>(doc.RootElement.GetRawText());
                    }
                    else if (doc.RootElement.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var prop in doc.RootElement.EnumerateObject())
                        {
                            if (prop.Value.ValueKind == JsonValueKind.Array)
                            {
                                var list = new List<string>(); foreach (var item in prop.Value.EnumerateArray()) list.Add(item.GetString() ?? "Error");
                                result.Errors[prop.Name] = list;
                            }
                        }
                    }
                }
                catch { }
            }
            return result;
        }
    }
    public class DistributorsApiClient : IDistributorsApiClient
    {
        private readonly HttpClient _http;
        public DistributorsApiClient(IHttpClientFactory factory) => _http = factory.CreateClient("DistributorsService");
        public async Task<DistributorDto?> GetByIdAsync(Guid id, CancellationToken ct) => await _http.GetFromJsonAsync<DistributorDto>($"api/Distributors/{id}", ct);
        public async Task<IReadOnlyList<DistributorDto>> GetAllAsync(CancellationToken ct) => await _http.GetFromJsonAsync<IReadOnlyList<DistributorDto>>("api/Distributors", ct) ?? Array.Empty<DistributorDto>();
        public async Task<DistributorApiResult> CreateAsync(DistributorCreateDto dto, CancellationToken ct)
        { var resp = await _http.PostAsJsonAsync("api/Distributors", dto, ct); return await ParseResult(resp, ct); }
        public async Task<DistributorApiResult> UpdateAsync(Guid id, DistributorUpdateDto dto, CancellationToken ct)
        { var resp = await _http.PutAsJsonAsync($"api/Distributors/{id}", dto, ct); return await ParseResult(resp, ct); }
        public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
        { var resp = await _http.DeleteAsync($"api/Distributors/{id}", ct); return resp.IsSuccessStatusCode; }
        private static async Task<DistributorApiResult> ParseResult(HttpResponseMessage resp, CancellationToken ct)
        {
            var result = new DistributorApiResult { Success = resp.IsSuccessStatusCode };
            if (resp.Content.Headers.ContentType?.MediaType == "application/json")
            {
                var json = await resp.Content.ReadAsStringAsync(ct);
                try
                {
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;
                    if (resp.IsSuccessStatusCode && root.ValueKind == JsonValueKind.Object)
                    { result.Distributor = System.Text.Json.JsonSerializer.Deserialize<DistributorDto>(root.GetRawText()); }
                    else if (root.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var prop in root.EnumerateObject())
                        {
                            if (prop.Value.ValueKind == JsonValueKind.Array)
                            { var list = new List<string>(); foreach (var item in prop.Value.EnumerateArray()) list.Add(item.GetString() ?? "Error"); result.Errors[prop.Name] = list; }
                            else if (prop.Value.ValueKind == JsonValueKind.String)
                            { result.Errors[prop.Name] = new List<string> { prop.Value.GetString() ?? "Error" }; }
                        }
                    }
                }
                catch { }
            }
            return result;
        }
    }
}
