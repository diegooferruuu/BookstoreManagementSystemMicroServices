using ServiceProducts.Domain.Models;
using ServiceProducts.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LibraryWeb.Pages.Products
{
    public class IndexModel : PageModel
    {
        private readonly IProductService _service;

        public List<Product> Products { get; set; } = new();

        public IndexModel(IProductService service)
        {
            _service = service;
        }

        public void OnGet()
        {
            Products = _service.GetAll();
        }

        public IActionResult OnPostEdit(Guid id)
        {
            TempData["EditProductId"] = id;
            return RedirectToPage("Edit");
        }

        public IActionResult OnPostDelete(Guid id)
        {
            _service.Delete(id);
            return RedirectToPage();
        }
    }
}
