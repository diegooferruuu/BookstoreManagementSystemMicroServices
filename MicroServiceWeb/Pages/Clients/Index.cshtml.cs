using ServiceClients.Domain.Models;
using ServiceClients.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Linq;
using MicroServiceWeb.External.Http;
using System.Threading;

namespace LibraryWeb.Pages.Clients
{
    public class IndexModel : PageModel
    {
        private readonly IClientsApiClient _api;

        public List<ClientDto> Clients { get; set; } = new();

        public IndexModel(IClientsApiClient api)
        {
            _api = api;
        }

        public async Task OnGetAsync(CancellationToken ct)
        {
            Clients = (await _api.GetAllAsync(ct)).ToList();
        }

        public IActionResult OnPostEdit(Guid id)
        {
            TempData["EditClientId"] = id;
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
