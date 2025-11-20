using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using MicroServiceWeb.External.Http;

namespace LibraryWeb.Pages.Users
{
    public class ChangePasswordModel : PageModel
    {
        // TODO: cuando exista endpoint remoto de cambio de contraseña usar IUsersApiClient
        public ChangePasswordModel() { }
        [BindProperty, Required(ErrorMessage = "La contraseña actual es obligatoria."), Display(Name = "Contraseña actual")] public string CurrentPassword { get; set; } = string.Empty;
        [BindProperty, Required(ErrorMessage = "La nueva contraseña es obligatoria."), MinLength(8, ErrorMessage = "Debe tener al menos 8 caracteres."), Display(Name = "Nueva contraseña")] public string NewPassword { get; set; } = string.Empty;
        [BindProperty, Required(ErrorMessage = "Debe confirmar la nueva contraseña."), Compare(nameof(NewPassword), ErrorMessage = "Las contraseñas no coinciden."), Display(Name = "Confirmar nueva contraseña")] public string ConfirmPassword { get; set; } = string.Empty;
        public IActionResult OnGet()
        {
            if (TempData.ContainsKey("PendingUser")) { TempData.Keep("PendingUser"); TempData.Keep("FirstLogin"); }
            if (!TempData.ContainsKey("PendingUser")) return RedirectToPage("/Auth/Login");
            return Page();
        }
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();
            if (!TempData.ContainsKey("PendingUser")) return RedirectToPage("/Auth/Login");
            var userName = TempData["PendingUser"]?.ToString() ?? string.Empty;
            // Validar nueva contraseña fuerte
            var regex = new Regex(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[\p{P}\p{S}]).{8,64}$");
            if (!regex.IsMatch(NewPassword)) { ModelState.AddModelError(nameof(NewPassword), "La nueva contraseña debe incluir mayúsculas, minúsculas, números y un carácter especial."); TempData.Keep("PendingUser"); return Page(); }
            // Aquí debería llamarse endpoint remoto de cambio de contraseña con userName, CurrentPassword y NewPassword.
            // Simulación temporal: si CurrentPassword == "admin123" marcar éxito.
            if (CurrentPassword != "admin123") { ModelState.AddModelError(nameof(CurrentPassword), "La contraseña actual no es correcta."); TempData.Keep("PendingUser"); return Page(); }
            // Limpiar estado y obligar re-login
            TempData.Remove("PendingUser"); TempData.Remove("FirstLogin"); await HttpContext.SignOutAsync();
            return RedirectToPage("/Auth/Login");
        }
    }
}
