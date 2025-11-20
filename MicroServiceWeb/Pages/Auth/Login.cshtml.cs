using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MicroServiceWeb.External.Http;
using System.ComponentModel.DataAnnotations;

namespace LibraryWeb.Pages.Auth
{
    public class LoginModel : PageModel
    {
        private readonly IUsersApiClient _usersApi;
        public LoginModel(IUsersApiClient usersApi) { _usersApi = usersApi; }
        [BindProperty, Required(ErrorMessage="El usuario o correo es obligatorio."), Display(Name="Usuario o Correo")] public string UserOrEmail { get; set; } = string.Empty;
        [BindProperty, Required(ErrorMessage="La contraseña es obligatoria."), Display(Name="Contraseña")] public string Password { get; set; } = string.Empty;
        [BindProperty(SupportsGet = true)] public string? ReturnUrl { get; set; }
        public string? ErrorMessage { get; set; }
        public void OnGet() { Password = string.Empty; }
        public async Task<IActionResult> OnPostAsync()
        {
            UserOrEmail = (UserOrEmail ?? string.Empty).Trim(); Password = (Password ?? string.Empty).Trim();
            if (!ModelState.IsValid) return Page();
            if (!string.IsNullOrEmpty(ReturnUrl) && !Url.IsLocalUrl(ReturnUrl)) ReturnUrl = Url.Page("/Index");
            var loginResult = await _usersApi.LoginAsync(new AuthLoginRequest { UserOrEmail = UserOrEmail, Password = Password }, HttpContext.RequestAborted);
            if (!loginResult.Success)
            {
                var msg = loginResult.Error;
                if (string.IsNullOrWhiteSpace(msg) || msg!.Trim().StartsWith("{")) msg = "Credenciales inválidas.";
                ErrorMessage = msg;
                return Page();
            }
            if (loginResult.MustChangePassword)
            {
                TempData["PendingUser"] = loginResult.UserName;
                TempData["PendingEmail"] = loginResult.Email;
                TempData["PendingToken"] = loginResult.Token;
                TempData["FirstLogin"] = true;
                return RedirectToPage("/Users/ChangePassword");
            }
            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, loginResult.UserName),
                new(ClaimTypes.Email, loginResult.Email ?? string.Empty)
            };
            foreach (var r in loginResult.Roles ?? new List<string>()) claims.Add(new Claim(ClaimTypes.Role, r));
            if (!string.IsNullOrEmpty(loginResult.Token)) claims.Add(new Claim("access_token", loginResult.Token));
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity), new AuthenticationProperties { IsPersistent = true, ExpiresUtc = (loginResult.ExpiresAt ?? DateTimeOffset.UtcNow.AddHours(8)).UtcDateTime });
            return Redirect(ReturnUrl ?? Url.Page("/Index")!);
        }
    }
}
