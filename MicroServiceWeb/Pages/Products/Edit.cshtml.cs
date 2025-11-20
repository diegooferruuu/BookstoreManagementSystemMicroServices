using ServiceProducts.Domain.Models;
using ServiceProducts.Domain.Interfaces;
using ServiceCommon.Application.Services;
using MicroServiceWeb.External.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using System.Threading;

namespace LibraryWeb.Pages.Products
{
    public class EditModel : PageModel
    {
        private readonly IProductsApiClient _api;

        [BindProperty]
        public ProductUpdateDto Product { get; set; } = new();

        public List<SelectListItem> Categories { get; set; } = new();

        [TempData]
        public Guid EditProductId { get; set; }

        public EditModel(IProductsApiClient api)
        {
            _api = api;
        }

        public async Task<IActionResult> OnGetAsync(CancellationToken ct)
        {
            var obj = TempData["EditProductId"];
            if (obj == null)
                return RedirectToPage("Index");

            Guid id;
            if (obj is Guid g)
                id = g;
            else if (obj is string s && Guid.TryParse(s, out g))
                id = g;
            else
                return RedirectToPage("Index");

            var product = await _api.GetByIdAsync(id, ct);
            if (product == null)
                return RedirectToPage("Index");

            Product = new ProductUpdateDto
            {
                Name = product.Name,
                Description = product.Description,
                CategoryId = product.CategoryId,
                Price = product.Price,
                Stock = product.Stock
            };
            await LoadCategoriesAsync(product.CategoryId, ct);
            TempData["EditProductId"] = id; // preservar para POST
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                await LoadCategoriesAsync(Product.CategoryId ?? Guid.Empty, ct);
                return Page();
            }

            var idObj = TempData["EditProductId"] ?? HttpContext.Request.Form["id"].ToString();
            if (idObj is null) return RedirectToPage("Index");
            Guid id = Guid.Empty;
            if (idObj is string sid) Guid.TryParse(sid, out id);
            else if (idObj is Guid gid) id = gid;
            if (id == Guid.Empty) return RedirectToPage("Index");

            var result = await _api.UpdateAsync(id, Product, ct);
            if (!result.Success)
            {
                foreach (var kv in result.Errors)
                {
                    var key = kv.Key switch
                    {
                        "name" => "Product.Name",
                        "description" => "Product.Description",
                        "categoryId" => "Product.CategoryId",
                        "price" => "Product.Price",
                        "stock" => "Product.Stock",
                        _ => $"Product.{kv.Key}"
                    };
                    foreach (var msg in kv.Value) ModelState.AddModelError(key, msg);
                }
                if (result.Errors.Count == 0) ModelState.AddModelError(string.Empty, "No se pudo actualizar el producto.");
                await LoadCategoriesAsync(Product.CategoryId ?? Guid.Empty, ct);
                TempData["EditProductId"] = id; // mantener
                return Page();
            }
            return RedirectToPage("Index");
        }

        private async Task LoadCategoriesAsync(Guid selected, CancellationToken ct)
        {
            var categories = await _api.GetCategoriesAsync(ct);
            Categories = categories.Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.Name,
                Selected = c.Id == selected
            }).ToList();
        }
    }
}
