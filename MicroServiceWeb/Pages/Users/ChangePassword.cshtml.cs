using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using MicroServiceWeb.External.Http;

namespace LibraryWeb.Pages.Users
{
    public class ChangePasswordModel : PageModel
    {
        private readonly IUsersApiClient _usersApi;
        public ChangePasswordModel(IUsersApiClient usersApi) { _usersApi = usersApi; }
        [BindProperty, Required(ErrorMessage = "La contraseña actual es obligatoria."), Display(Name = "Contraseña actual")] public string CurrentPassword { get; set; } = string.Empty;
        [BindProperty, Required(ErrorMessage = "La nueva contraseña es obligatoria."), MinLength(8, ErrorMessage = "Debe tener al menos 8 caracteres."), Display(Name = "Nueva contraseña")] public string NewPassword { get; set; } = string.Empty;
        [BindProperty, Required(ErrorMessage = "Debe confirmar la nueva contraseña."), Compare(nameof(NewPassword), ErrorMessage = "Las contraseñas no coinciden."), Display(Name = "Confirmar nueva contraseña")] public string ConfirmPassword { get; set; } = string.Empty;
        public string? SuccessMessage { get; set; }
        public IActionResult OnGet()
        {
            if (TempData.ContainsKey("PendingUser")) { TempData.Keep("PendingUser"); TempData.Keep("FirstLogin"); TempData.Keep("PendingToken"); }
            if (!TempData.ContainsKey("PendingUser")) return RedirectToPage("/Auth/Login");
            return Page();
        }
        public async Task<IActionResult> OnPostAsync()
        {
            if (!TempData.ContainsKey("PendingUser")) return RedirectToPage("/Auth/Login");
            if (!ModelState.IsValid) { TempData.Keep("PendingUser"); TempData.Keep("FirstLogin"); TempData.Keep("PendingToken"); return Page(); }

            var regex = new Regex(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[\p{P}\p{S}]).{8,64}$");
            if (!regex.IsMatch(NewPassword)) { ModelState.AddModelError(nameof(NewPassword), "La nueva contraseña debe incluir mayúsculas, minúsculas, números y un carácter especial."); TempData.Keep("PendingUser"); TempData.Keep("FirstLogin"); TempData.Keep("PendingToken"); return Page(); }

            var bearer = TempData["PendingToken"]?.ToString();
            var result = await _usersApi.ChangePasswordAsync(new ChangePasswordRequest
            {
                CurrentPassword = CurrentPassword,
                NewPassword = NewPassword,
                ConfirmPassword = ConfirmPassword
            }, bearer, HttpContext.RequestAborted);

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Error ?? "No se pudo cambiar la contraseña.");
                TempData.Keep("PendingUser"); TempData.Keep("FirstLogin"); TempData.Keep("PendingToken");
                return Page();
            }

            SuccessMessage = "La contraseña se cambió correctamente. Debe iniciar sesión nuevamente.";
            // Limpiar estado y forzar re-login con mensaje
            TempData.Clear();
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            TempData["PasswordChanged"] = SuccessMessage;
            return RedirectToPage("/Auth/Login");
        }
    }
}
