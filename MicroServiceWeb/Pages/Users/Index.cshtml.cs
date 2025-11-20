using MicroServiceWeb.External.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Linq;
using System.Threading;

namespace LibraryWeb.Pages.Users
{
    public class IndexModel : PageModel
    {
        private readonly IUsersApiClient _api;
        public List<UserFullDto> Users { get; set; } = new();
        public IndexModel(IUsersApiClient api) { _api = api; }
        public async Task OnGetAsync(CancellationToken ct)
        {
            var basic = await _api.GetAllRawAsync(ct);
            var list = new List<UserFullDto>();
            foreach (var u in basic)
            {
                var roles = await _api.GetRolesAsync(u.Id, ct);
                list.Add(new UserFullDto(u.Id, u.Username, u.Email, u.FirstName, u.MiddleName, u.LastName, u.MustChangePassword, roles.ToList(), u.PasswordHash));
            }
            Users = list;
        }
        public async Task<IActionResult> OnPostDeleteAsync(Guid id, CancellationToken ct)
        {
            var ok = await _api.DeleteAsync(id, ct);
            if (!ok) TempData["ErrorMessage"] = "No se pudo eliminar usuario";
            return RedirectToPage();
        }
    }
}
