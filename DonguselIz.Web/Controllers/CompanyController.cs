using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DonguselIz.Web.Data;
using DonguselIz.Web.Infrastructure.Filters;
using DonguselIz.Web.Models.Domain;
using DonguselIz.Web.Models.Enums;
using DonguselIz.Web.Models.ViewModels;
using DonguselIz.Web.Services;

namespace DonguselIz.Web.Controllers
{
    // Üretici ve geri dönüşümcü firmaların kendi profilini ve ekibini gördüğü alan
    [AuthorizeRole(UserRole.WasteGenerator, UserRole.RecyclerBuyer)]
    public class CompanyController : Controller
    {
        private const int MinPasswordLength = 8;

        private readonly IAuthService _authService;
        private readonly AppDbContext _db;

        public CompanyController(IAuthService authService, AppDbContext db)
        {
            _authService = authService;
            _db = db;
        }

        private CompanyEmployee Me => (CompanyEmployee)HttpContext.Items["AuthenticatedEmployee"]!;

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var company = await _authService.GetCompanyByIdAsync(Me.CompanyId);
            if (company == null) return NotFound();

            var batches = await _db.Batches.Where(b => b.CompanyId == company.Id).ToListAsync();
            var model = new CompanyProfileViewModel
            {
                Company = company,
                CurrentEmployeeId = Me.Id,
                IsManager = Me.IsCompanyManager,
                TotalBatches = batches.Count,
                TotalWeightKg = batches.Sum(b => b.WeightKg),
                BatchCountByPerson = batches
                    .GroupBy(b => b.CreatedByName)
                    .ToDictionary(g => g.Key, g => g.Count())
            };
            return View(model);
        }

        // Çıktı türleri, kapasite ve adres firmanın yöneticisi tarafından güncellenir
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(string outputMaterialTypes, double monthlyOutputCapacityTons, string? facilityAddress)
        {
            if (!Me.IsCompanyManager) return Forbidden();

            if (string.IsNullOrWhiteSpace(outputMaterialTypes) || monthlyOutputCapacityTons < 0)
            {
                TempData["ErrorMessage"] = "Çıktı türleri boş olamaz ve kapasite negatif olamaz.";
                return RedirectToAction(nameof(Index));
            }

            var company = await _authService.GetCompanyByIdAsync(Me.CompanyId);
            if (company == null) return NotFound();

            company.OutputMaterialTypes = outputMaterialTypes.Trim();
            company.MonthlyOutputCapacityTons = monthlyOutputCapacityTons;
            company.FacilityAddress = facilityAddress?.Trim() ?? string.Empty;
            await _authService.UpdateCompanyAsync(company);

            TempData["SuccessMessage"] = "Firma çıktı bilgileri güncellendi.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddStaff(string fullName, string email, string title, string? phoneNumber, string password)
        {
            if (!Me.IsCompanyManager) return Forbidden();

            if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                TempData["ErrorMessage"] = "Ad soyad, e-posta ve şifre zorunludur.";
                return RedirectToAction(nameof(Index));
            }

            if (password.Length < MinPasswordLength)
            {
                TempData["ErrorMessage"] = $"Şifre en az {MinPasswordLength} karakter olmalı.";
                return RedirectToAction(nameof(Index));
            }

            var employee = new CompanyEmployee
            {
                CompanyId = Me.CompanyId,
                FullName = fullName.Trim(),
                Email = email.Trim().ToLowerInvariant(),
                Title = string.IsNullOrWhiteSpace(title) ? "Vardiya ve Atık Sorumlusu" : title.Trim(),
                PhoneNumber = phoneNumber?.Trim() ?? string.Empty,
                // Personel firmanın kendi sistem rolünü taşır, yetki yükseltilemez
                Role = Me.Role,
                IsActive = true
            };

            var added = await _authService.AddEmployeeToCompanyAsync(employee, password);
            if (added)
                TempData["SuccessMessage"] = $"{employee.FullName} ekibe eklendi.";
            else
                TempData["ErrorMessage"] = "Bu e-posta adresiyle kayıtlı başka bir kullanıcı var.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditStaff(string employeeId, string fullName, string title, string? phoneNumber,
            bool isActive, bool isManager, string? newPassword)
        {
            if (!Me.IsCompanyManager) return Forbidden();

            var company = await _authService.GetCompanyByIdAsync(Me.CompanyId);
            var target = company?.Employees.FirstOrDefault(e => e.Id == employeeId);
            if (target == null) return NotFound();

            if (string.IsNullOrWhiteSpace(fullName))
            {
                TempData["ErrorMessage"] = "Ad soyad boş olamaz.";
                return RedirectToAction(nameof(Index));
            }

            if (!string.IsNullOrEmpty(newPassword) && newPassword.Length < MinPasswordLength)
            {
                TempData["ErrorMessage"] = $"Yeni şifre en az {MinPasswordLength} karakter olmalı.";
                return RedirectToAction(nameof(Index));
            }

            // Yönetici kendi yetkisini ve aktifliğini düşüremez, firma yönetimsiz kalmasın
            var isSelf = target.Id == Me.Id;
            var update = new CompanyEmployee
            {
                Id = target.Id,
                FullName = fullName.Trim(),
                Title = string.IsNullOrWhiteSpace(title) ? target.Title : title.Trim(),
                PhoneNumber = phoneNumber?.Trim() ?? string.Empty,
                Role = target.Role,
                IsActive = isSelf ? true : isActive,
                IsCompanyManager = isSelf ? true : isManager
            };

            await _authService.UpdateEmployeeAsync(update, newPassword);
            TempData["SuccessMessage"] = $"{update.FullName} bilgileri güncellendi.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteStaff(string employeeId)
        {
            if (!Me.IsCompanyManager) return Forbidden();

            if (employeeId == Me.Id)
            {
                TempData["ErrorMessage"] = "Kendi hesabınızı silemezsiniz.";
                return RedirectToAction(nameof(Index));
            }

            var company = await _authService.GetCompanyByIdAsync(Me.CompanyId);
            if (company?.Employees.All(e => e.Id != employeeId) != false) return NotFound();

            var deleted = await _authService.DeleteEmployeeAsync(employeeId);
            if (deleted)
                TempData["SuccessMessage"] = "Personel silindi.";
            else
                TempData["ErrorMessage"] = "Personel silinemedi.";

            return RedirectToAction(nameof(Index));
        }

        private IActionResult Forbidden() => View("~/Views/Shared/AccessDenied.cshtml");
    }
}
