using ServiceProducts.Domain.Models;
using ServiceProducts.Domain.Interfaces;
using ServiceCommon.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;

namespace LibraryWeb.Pages.Products
{
    public class EditModel : PageModel
    {
        private readonly IProductService _service;
        private readonly ICategoryRepository _categoryRepository;

        [BindProperty]
        public Product Product { get; set; } = new();

        public List<SelectListItem> Categories { get; set; } = new();

        [TempData]
        public Guid EditProductId { get; set; }

        public EditModel(IProductService service, ICategoryRepository categoryRepository)
        {
            _service = service;
            _categoryRepository = categoryRepository;
        }

        public IActionResult OnGet()
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

            var product = _service.Read(id);
            if (product == null)
                return RedirectToPage("Index");

            Product = product;
            LoadCategories();
            return Page();
        }

        public IActionResult OnPost()
        {
            var domainErrors = ProductValidation
                .Validate(Product, _categoryRepository)
                .ToList();
            foreach (var e in domainErrors)
                ModelState.AddModelError($"Product.{e.Field}", e.Message);

            if (!ModelState.IsValid)
            {
                LoadCategories();
                return Page();
            }

            try
            {
                _service.Update(Product);
                return RedirectToPage("/Products/Index");
            }
            catch (ValidationException vex)
            {
                foreach (var e in vex.Errors)
                    ModelState.AddModelError($"Product.{e.Field}", e.Message);
                LoadCategories();
                return Page();
            }
        }

        private void LoadCategories()
        {
            var categories = _categoryRepository.GetAll();
            Categories = categories.Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.Name,
                Selected = c.Id == Product.CategoryId
            }).ToList();
            Categories.Insert(0, new SelectListItem { Value = "", Text = "Seleccionar categoría..." });
        }
    }
}
