using MicroServiceWeb.External.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Linq;
using System.Threading;

namespace LibraryWeb.Pages.Distributors
{
    public class IndexModel : PageModel
    {
        private readonly IDistributorsApiClient _api;
        public List<DistributorDto> Distributors { get; set; } = new();
        public IndexModel(IDistributorsApiClient api) { _api = api; }
        public async Task OnGetAsync(CancellationToken ct) => Distributors = (await _api.GetAllAsync(ct)).ToList();
        public IActionResult OnPostEdit(Guid id) { TempData["EditDistributorId"] = id; return RedirectToPage("Edit"); }
        public async Task<IActionResult> OnPostDeleteAsync(Guid id, CancellationToken ct)
        {
            var ok = await _api.DeleteAsync(id, ct);
            if (!ok) TempData["ErrorMessage"] = "No se pudo eliminar"; return RedirectToPage();
        }
    }
}
