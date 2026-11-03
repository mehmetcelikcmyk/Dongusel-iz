using Microsoft.AspNetCore.Mvc;
using DonguselIz.Web.Models.ViewModels;
using DonguselIz.Web.Models.Domain;
using DonguselIz.Web.Services;

namespace DonguselIz.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAuthService _authService;
        private readonly ITokenService _tokenService;

        public AccountController(IAuthService authService, ITokenService tokenService)
        {
            _authService = authService;
            _tokenService = tokenService;
        }

        [HttpGet]
        public async Task<IActionResult> Login(string? returnUrl = null)
        {
            var sessionEmail = HttpContext.Session.GetString("CurrentUserEmail");
            if (!string.IsNullOrEmpty(sessionEmail))
            {
                return RedirectToAction("Index", "Panel");
            }

            if (Request.Cookies.TryGetValue("DonguselIz_AuthToken", out var token) && !string.IsNullOrWhiteSpace(token))
            {
                var validatedEmail = _tokenService.ValidateRememberToken(token);
                if (!string.IsNullOrEmpty(validatedEmail))
                {
                    var existingEmp = await _authService.GetEmployeeByEmailAsync(validatedEmail);
                    if (existingEmp != null && existingEmp.IsActive)
                    {
                        HttpContext.Session.SetString("CurrentUserEmail", existingEmp.Email);
                        HttpContext.Session.SetString("CurrentUserRole", existingEmp.Role.ToString());
                        HttpContext.Session.SetString("CurrentCompany", existingEmp.Company?.Name ?? "");
                        HttpContext.Session.SetString("CurrentUserName", existingEmp.FullName);

                        return RedirectToAction("Index", "Panel");
                    }
                }
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var employee = await _authService.AuthenticateAsync(model.Email, model.Password);
            if (employee == null)
            {
                ModelState.AddModelError(string.Empty, "Girilen kurumsal e-posta adresi veya parola geçersiz. Sistemde tanımlı değilseniz lütfen katılım başvurusu yapınız.");
                return View(model);
            }

            HttpContext.Session.SetString("CurrentUserEmail", employee.Email);
            HttpContext.Session.SetString("CurrentUserRole", employee.Role.ToString());
            HttpContext.Session.SetString("CurrentCompany", employee.Company?.Name ?? "");
            HttpContext.Session.SetString("CurrentUserName", employee.FullName);

            if (model.RememberMe)
            {
                var secureToken = _tokenService.GenerateRememberToken(employee.Email, daysValid: 30);
                var cookieOptions = new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddDays(30),
                    HttpOnly = true,
                    Secure = false,
                    SameSite = SameSiteMode.Lax
                };
                Response.Cookies.Append("DonguselIz_AuthToken", secureToken, cookieOptions);
            }
            else
            {
                Response.Cookies.Delete("DonguselIz_AuthToken");
            }

            TempData["SuccessMessage"] = $"Giriş başarılı. Sayın {employee.FullName} ({employee.Company?.Name} - {employee.Title})";

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Panel");
        }

        [HttpGet]
        public IActionResult Apply()
        {
            return View(new CompanyApplicationViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Apply(CompanyApplicationViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var app = new CompanyApplication
            {
                CompanyName = model.CompanyName,
                TaxNumber = model.TaxNumber,
                OsbRegion = model.OsbRegion,
                ContactFullName = model.ContactFullName,
                Email = model.Email.ToLower().Trim(),
                PhoneNumber = model.PhoneNumber,
                RequestedRole = model.RequestedRole,
                PasswordHash = EfAuthService.BCryptHash(model.Password),
                WasteOrMaterialDescription = $"{model.WasteOrMaterialDescription} (Kapasite: {model.MonthlyOutputCapacityTons} Ton/Ay)"
            };

            await _authService.SubmitApplicationAsync(app);

            TempData["SuccessMessage"] = "Ön başvuru kaydınız başarıyla alındı. Bölge müdürlüğü onayının ardından belirlediğiniz şifre ile sisteme doğrudan giriş yapabilirsiniz.";
            return RedirectToAction(nameof(Login));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            Response.Cookies.Delete("DonguselIz_AuthToken");
            return RedirectToAction("Index", "Demo");
        }
    }
}
