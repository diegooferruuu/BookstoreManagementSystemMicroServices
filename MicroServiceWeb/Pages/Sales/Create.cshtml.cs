using MicroServiceWeb.External.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LibraryWeb.Pages.Sales
{
    public class CreateModel : PageModel
    {
        private readonly IClientsApiClient _clients;
        private readonly IProductsApiClient _products;

        public CreateModel(IClientsApiClient clients, IProductsApiClient products)
        {
            _clients = clients;
            _products = products;
        }

        // === Propiedades de binding ===
        [BindProperty]
        public string? ClientSearch { get; set; }

        [BindProperty]
        public Guid? SelectedClientId { get; set; }

        [BindProperty]
        public string? ProductSearch { get; set; }

        [BindProperty]
        public List<SaleItemVm> Items { get; set; } = new();

        [BindProperty]
        public ClientCreateDto NewClient { get; set; } = new();

        // === Propiedades de solo lectura ===
        public ClientDto? SelectedClient { get; set; }
        public List<ClientDto> ClientSuggestions { get; set; } = new();
        public List<ProductDto> ProductSuggestions { get; set; } = new();
        public decimal Total => Items.Sum(i => i.Total);
        public DateTime Today { get; } = DateTime.Today;

        public class SaleItemVm
        {
            public Guid ProductId { get; set; }
            public string Description { get; set; } = string.Empty;
            public int Quantity { get; set; } = 1;
            public decimal UnitPrice { get; set; }
            public decimal Total => Quantity * UnitPrice;
        }

        // === Helpers ===
        private static string Normalize(string? s) => (s ?? "").Trim().Replace(" ", "").ToUpperInvariant();

        private async Task LoadSelectedClientAsync(CancellationToken ct)
        {
            if (SelectedClientId is Guid id && id != Guid.Empty)
                SelectedClient = await _clients.GetByIdAsync(id, ct);
        }

        private async Task<List<ClientDto>> GetAllClientsAsync(CancellationToken ct)
        {
            var all = new List<ClientDto>();
            int page = 1;
            while (page <= 20)
            {
                var paged = await _clients.GetPagedAsync(page, 100, ct);
                if (paged.Items == null || paged.Items.Count == 0) break;
                all.AddRange(paged.Items);
                if (paged.Items.Count < 100 || page >= paged.TotalPages) break;
                page++;
            }
            return all;
        }

        private async Task<List<ProductDto>> GetAllProductsAsync(CancellationToken ct)
        {
            var all = new List<ProductDto>();
            int page = 1;
            while (page <= 20)
            {
                var paged = await _products.GetPagedAsync(page, 100, ct);
                if (paged.Items == null || paged.Items.Count == 0) break;
                all.AddRange(paged.Items);
                if (paged.Items.Count < 100 || page >= paged.TotalPages) break;
                page++;
            }
            return all;
        }

        // === GET ===
        public async Task OnGetAsync(CancellationToken ct)
        {
            await LoadSelectedClientAsync(ct);
        }

        // === CLIENTE: Buscar ===
        public async Task<IActionResult> OnPostSearchClientAsync(CancellationToken ct)
        {
            await LoadSelectedClientAsync(ct);

            var search = Normalize(ClientSearch);
            if (string.IsNullOrEmpty(search))
            {
                ClientSuggestions = new();
                return Page();
            }

            var all = await GetAllClientsAsync(ct);
            ClientSuggestions = all
                .Where(c => Normalize(c.Ci).Contains(search))
                .OrderBy(c => c.LastName)
                .Take(20)
                .ToList();

            // Si hay match exacto único, seleccionar automáticamente
            var exact = ClientSuggestions.FirstOrDefault(c => Normalize(c.Ci) == search);
            if (exact != null && ClientSuggestions.Count == 1)
            {
                SelectedClientId = exact.Id;
                SelectedClient = exact;
                ClientSuggestions = new();
            }

            return Page();
        }

        // === CLIENTE: Seleccionar ===
        public async Task<IActionResult> OnPostPickClientAsync(Guid id, CancellationToken ct)
        {
            SelectedClientId = id;
            SelectedClient = await _clients.GetByIdAsync(id, ct);
            ClientSearch = SelectedClient?.Ci;
            ClientSuggestions = new();
            return Page();
        }

        // === CLIENTE: Quitar selección ===
        public Task<IActionResult> OnPostClearClientAsync()
        {
            SelectedClientId = null;
            SelectedClient = null;
            ClientSearch = null;
            return Task.FromResult<IActionResult>(Page());
        }

        // === CLIENTE: Crear nuevo ===
        public async Task<IActionResult> OnPostCreateClientAsync(CancellationToken ct)
        {
            await LoadSelectedClientAsync(ct);

            NewClient.FirstName = NewClient.FirstName?.Trim() ?? "";
            NewClient.LastName = NewClient.LastName?.Trim() ?? "";
            NewClient.Ci = NewClient.Ci?.Trim() ?? "";
            NewClient.Email = NewClient.Email?.Trim();
            NewClient.Phone = NewClient.Phone?.Trim();
            NewClient.Address = NewClient.Address?.Trim();

            if (!TryValidateModel(NewClient, nameof(NewClient)))
            {
                TempData["OpenClientModal"] = true;
                return Page();
            }

            var result = await _clients.CreateAsync(NewClient, ct);
            if (!result.Success)
            {
                foreach (var kv in result.Errors)
                {
                    var key = kv.Key switch
                    {
                        "firstName" => "NewClient.FirstName",
                        "lastName" => "NewClient.LastName",
                        "ci" => "NewClient.Ci",
                        "email" => "NewClient.Email",
                        "phone" => "NewClient.Phone",
                        "address" => "NewClient.Address",
                        _ => $"NewClient.{kv.Key}"
                    };
                    foreach (var msg in kv.Value)
                        ModelState.AddModelError(key, msg);
                }
                TempData["OpenClientModal"] = true;
                return Page();
            }

            // Buscar el cliente recién creado
            var created = result.Client;
            if (created == null)
            {
                var all = await GetAllClientsAsync(ct);
                created = all.FirstOrDefault(c => Normalize(c.Ci) == Normalize(NewClient.Ci));
            }

            if (created != null)
            {
                SelectedClientId = created.Id;
                SelectedClient = created;
                ClientSearch = created.Ci;
            }

            NewClient = new ClientCreateDto();
            return Page();
        }

        // === CLIENTE: Sugerencias JSON ===
        public async Task<IActionResult> OnGetClientSuggestionsAsync(string? term, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
                return new JsonResult(Array.Empty<object>());

            var search = Normalize(term);
            var all = await GetAllClientsAsync(ct);
            var matches = all
                .Where(c => Normalize(c.Ci).Contains(search))
                .Take(10)
                .Select(c => new { id = c.Id, ci = c.Ci, name = $"{c.LastName} {c.FirstName}" })
                .ToList();

            return new JsonResult(matches);
        }

        // === PRODUCTO: Buscar ===
        public async Task<IActionResult> OnPostSearchProductAsync(CancellationToken ct)
        {
            await LoadSelectedClientAsync(ct);

            var search = (ProductSearch ?? "").Trim();
            if (string.IsNullOrEmpty(search))
            {
                ProductSuggestions = new();
                return Page();
            }

            var all = await GetAllProductsAsync(ct);
            ProductSuggestions = all
                .Where(p => !string.IsNullOrWhiteSpace(p.Name) && p.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
                .OrderBy(p => p.Name)
                .Take(20)
                .ToList();

            return Page();
        }

        // === PRODUCTO: Sugerencias JSON ===
        public async Task<IActionResult> OnGetProductSuggestionsAsync(string? term, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
                return new JsonResult(Array.Empty<object>());

            var search = term.Trim();
            var all = await GetAllProductsAsync(ct);
            var matches = all
                .Where(p => !string.IsNullOrWhiteSpace(p.Name) && p.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
                .Take(10)
                .Select(p => new { id = p.Id, name = p.Name, price = p.Price })
                .ToList();

            return new JsonResult(matches);
        }

        // === PRODUCTO: Agregar a la venta ===
        public async Task<IActionResult> OnPostAddItemAsync(Guid id, CancellationToken ct)
        {
            await LoadSelectedClientAsync(ct);

            // Primero verificar si ya existe en Items para evitar llamada innecesaria
            var existing = Items.FirstOrDefault(i => i.ProductId == id);
            if (existing != null)
            {
                existing.Quantity++;
                ProductSearch = null;
                ProductSuggestions = new();
                return Page();
            }

            // Solo llamar al microservicio si no existe
            var product = await _products.GetByIdAsync(id, ct);
            if (product == null)
            {
                ModelState.AddModelError("", "Producto no encontrado.");
                return Page();
            }

            Items.Add(new SaleItemVm
            {
                ProductId = id,
                Description = product.Name,
                UnitPrice = product.Price,
                Quantity = 1
            });

            ProductSearch = null;
            ProductSuggestions = new();
            return Page();
        }

        // === ITEMS: Incrementar ===
        public async Task<IActionResult> OnPostIncAsync(Guid id, CancellationToken ct)
        {
            await LoadSelectedClientAsync(ct);
            var item = Items.FirstOrDefault(i => i.ProductId == id);
            if (item != null) item.Quantity++;
            return Page();
        }

        // === ITEMS: Decrementar ===
        public async Task<IActionResult> OnPostDecAsync(Guid id, CancellationToken ct)
        {
            await LoadSelectedClientAsync(ct);
            var item = Items.FirstOrDefault(i => i.ProductId == id);
            if (item != null)
            {
                if (item.Quantity <= 1)
                    Items.Remove(item);
                else
                    item.Quantity--;
            }
            return Page();
        }

        // === ITEMS: Eliminar ===
        public async Task<IActionResult> OnPostRemoveAsync(Guid id, CancellationToken ct)
        {
            await LoadSelectedClientAsync(ct);
            Items.RemoveAll(i => i.ProductId == id);
            return Page();
        }

        // === GUARDAR VENTA ===
        public async Task<IActionResult> OnPostSaveAsync(CancellationToken ct)
        {
            await LoadSelectedClientAsync(ct);

            if (SelectedClientId == null || SelectedClient == null)
            {
                ModelState.AddModelError("", "Debe seleccionar un cliente.");
                return Page();
            }

            if (Items.Count == 0)
            {
                ModelState.AddModelError("", "Debe agregar al menos un producto.");
                return Page();
            }

            // TODO: Llamar al microservicio de ventas
            TempData["Success"] = "Venta registrada correctamente.";
            return RedirectToPage("/Sales/Index");
        }
    }
}
