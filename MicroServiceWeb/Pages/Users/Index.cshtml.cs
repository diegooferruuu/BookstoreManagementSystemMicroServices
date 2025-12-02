using MicroServiceWeb.External.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ServiceUsers.Domain.Models;
using System.Linq;
using System.Threading;

namespace LibraryWeb.Pages.Users
{
    public class IndexModel : PageModel
    {
        private readonly IUsersApiClient _api;
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalItems { get; set; }
        public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling((double)TotalItems / PageSize);

        public List<UserFullDto> Users { get; set; } = new();
        public IndexModel(IUsersApiClient api) { _api = api; }
        public async Task OnGetAsync(CancellationToken ct, int? page, int? pageSize)
        {
            var basic = await _api.GetAllRawAsync(ct);
            var list = new List<UserFullDto>();
            TotalItems = basic.Count;

            if (pageSize.HasValue && pageSize.Value > 0)
                PageSize = pageSize.Value;
            if (page.HasValue && page.Value > 0)
                Page = page.Value;

            if (PageSize < 1) PageSize = 10;
            if (PageSize > 100) PageSize = 100;
            if (TotalItems == 0)
            {
                Users = new List<UserFullDto>();
                Page = 1;
                return;
            }
            var maxPage = (int)Math.Ceiling((double)TotalItems / PageSize);
            if (Page > maxPage) Page = maxPage;
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
