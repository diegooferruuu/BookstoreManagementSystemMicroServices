using MicroServiceWeb.External.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Linq;
using System.Threading;

namespace LibraryWeb.Pages.Products
{
    public class IndexModel : PageModel
    {
        private readonly IProductsApiClient _api;

        public List<ProductDto> Products { get; set; } = new();

        public IndexModel(IProductsApiClient api)
        {
            _api = api;
        }

        public async Task OnGetAsync(CancellationToken ct)
        {
            Products = (await _api.GetAllAsync(ct)).ToList();
        }

        public IActionResult OnPostEdit(Guid id)
        {
            TempData["EditProductId"] = id;
            return RedirectToPage("Edit");
        }

        public async Task<IActionResult> OnPostDeleteAsync(Guid id, CancellationToken ct)
        {
            var ok = await _api.DeleteAsync(id, ct);
            if (!ok) TempData["ErrorMessage"] = "No se pudo eliminar";
            return RedirectToPage();
        }
    }
}
