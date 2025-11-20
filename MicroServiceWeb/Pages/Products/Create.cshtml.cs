using ServiceProducts.Domain.Models;
using ServiceProducts.Domain.Interfaces;
using ServiceCommon.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LibraryWeb.Pages.Products
{
    public class CreateModel : PageModel
    {
        private readonly IProductService _service;
        private readonly ICategoryRepository _categoryRepository;

        [BindProperty]
        public Product Product { get; set; } = new();
        
        public List<SelectListItem> Categories { get; set; } = new();

        public CreateModel(IProductService service, ICategoryRepository categoryRepository)
        {
            _service = service;
            _categoryRepository = categoryRepository;
        }

        public void OnGet()
        {
            LoadCategories();
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
                _service.Create(Product);
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
                Text = c.Name
            }).ToList();
            Categories.Insert(0, new SelectListItem { Value = "", Text = "Selecciona una categoria..." });
        }
    }
}
