using Microsoft.AspNetCore.Mvc;
using DonguselIz.Web.Models.ViewModels;
using DonguselIz.Web.Models.Domain;
using DonguselIz.Web.Models.Enums;
using DonguselIz.Web.Services;
using DonguselIz.Web.Infrastructure.Filters;

namespace DonguselIz.Web.Controllers
{
    [AuthorizeRole(UserRole.OsbAuditor)]
    public class AdminController : Controller
    {
        private readonly IAuthService _authService;

        public AdminController(IAuthService authService)
        {
            _authService = authService;
        }

        // 1. Tesisler ve Ön Talepler Ana Ekranı
        [HttpGet]
        public async Task<IActionResult> Companies()
        {
            var model = new CompanyManagementViewModel
            {
                Companies = await _authService.GetAllCompaniesWithEmployeesAsync(),
                PendingApplications = await _authService.GetPendingApplicationsAsync()
            };
            return View(model);
        }

        // 2. Yeni Tesis Ekleme
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCompany(CompanyManagementViewModel model)
        {
            var newCompany = model.NewCompany;
            var success = await _authService.CreateCompanyWithAdminAsync(
                newCompany, 
                model.InitialAdminFullName, 
                model.InitialAdminEmail, 
                model.InitialPassword);

            if (!success)
            {
                TempData["ErrorMessage"] = "Firma kaydedilemedi. VKN veya Yetkili E-Posta adresi sistemde zaten kayıtlı olabilir.";
                return RedirectToAction(nameof(Companies));
            }

            TempData["SuccessMessage"] = $"{newCompany.Name} tesisi ve yetkili personeli başarıyla sisteme tanımlandı.";
            return RedirectToAction(nameof(Companies));
        }

        // 3. Tesis Detay ve Düzenleme Ekranı
        [HttpGet]
        public async Task<IActionResult> EditCompany(string id)
        {
            var company = await _authService.GetCompanyByIdAsync(id);
            if (company == null) return NotFound();

            var model = new CompanyEditViewModel
            {
                Id = company.Id,
                Name = company.Name,
                TaxNumber = company.TaxNumber,
                OsbRegion = company.OsbRegion,
                PrimaryRole = company.PrimaryRole,
                OutputMaterialTypes = company.OutputMaterialTypes,
                MonthlyOutputCapacityTons = company.MonthlyOutputCapacityTons,
                FacilityAddress = company.FacilityAddress,
                IsActive = company.IsActive,
                Employees = company.Employees
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCompany(CompanyEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var comp = await _authService.GetCompanyByIdAsync(model.Id);
                if (comp != null) model.Employees = comp.Employees;
                return View(model);
            }

            var company = new Company
            {
                Id = model.Id,
                Name = model.Name,
                TaxNumber = model.TaxNumber,
                OsbRegion = model.OsbRegion,
                PrimaryRole = model.PrimaryRole,
                OutputMaterialTypes = model.OutputMaterialTypes,
                MonthlyOutputCapacityTons = model.MonthlyOutputCapacityTons,
                FacilityAddress = model.FacilityAddress,
                IsActive = model.IsActive
            };

            var success = await _authService.UpdateCompanyAsync(company);
            if (success)
            {
                TempData["SuccessMessage"] = $"{model.Name} tesis bilgileri ve çıktı kapasitesi başarıyla güncellendi.";
            }

            return RedirectToAction(nameof(EditCompany), new { id = model.Id });
        }

        // 4. Şirkete Yeni Personel Ekleme
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddEmployee(EmployeeCreateOrEditViewModel model)
        {
            if (string.IsNullOrEmpty(model.FullName) || string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.Password))
            {
                TempData["ErrorMessage"] = "Personel ad soyad, e-posta ve şifre zorunludur.";
                return RedirectToAction(nameof(EditCompany), new { id = model.CompanyId });
            }

            var targetCompany = await _authService.GetCompanyByIdAsync(model.CompanyId);
            if (targetCompany == null) return NotFound();

            var employee = new CompanyEmployee
            {
                CompanyId = model.CompanyId,
                FullName = model.FullName,
                Email = model.Email.ToLower(),
                Title = model.Title,
                PhoneNumber = model.PhoneNumber,
                // Personel her zaman firmanın sistem rolünü taşır
                Role = targetCompany.PrimaryRole,
                IsActive = true
            };

            var success = await _authService.AddEmployeeToCompanyAsync(employee, model.Password);
            if (!success)
            {
                TempData["ErrorMessage"] = "Bu kurumsal e-posta ile sistemde kayıtlı başka bir personel bulunmaktadır.";
            }
            else
            {
                TempData["SuccessMessage"] = $"{model.FullName} personeli tesise başarıyla tanımlandı.";
            }

            return RedirectToAction(nameof(EditCompany), new { id = model.CompanyId });
        }

        // 5. Personel Güncelleme (Ad, Ünvan, Telefon, Rol, Şifre)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateEmployee(EmployeeCreateOrEditViewModel model)
        {
            if (string.IsNullOrEmpty(model.EmployeeId) || string.IsNullOrEmpty(model.FullName))
            {
                TempData["ErrorMessage"] = "Personel bilgileri eksik.";
                return RedirectToAction(nameof(EditCompany), new { id = model.CompanyId });
            }

            var company = await _authService.GetCompanyByIdAsync(model.CompanyId);
            if (company == null) return NotFound();

            var employee = new CompanyEmployee
            {
                Id = model.EmployeeId,
                CompanyId = model.CompanyId,
                FullName = model.FullName,
                Title = model.Title,
                PhoneNumber = model.PhoneNumber ?? string.Empty,
                Role = company.PrimaryRole,
                IsActive = true
            };

            var success = await _authService.UpdateEmployeeAsync(employee, string.IsNullOrWhiteSpace(model.Password) ? null : model.Password);
            if (success)
            {
                TempData["SuccessMessage"] = $"{model.FullName} personel bilgileri başarıyla güncellendi.";
            }
            else
            {
                TempData["ErrorMessage"] = "Personel güncellenirken bir hata oluştu.";
            }

            return RedirectToAction(nameof(EditCompany), new { id = model.CompanyId });
        }

        // 6. Personel Silme
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEmployee(string employeeId, string companyId)
        {
            var deleted = await _authService.DeleteEmployeeAsync(employeeId);
            if (deleted)
                TempData["SuccessMessage"] = "Personel erişim kaydı silindi.";
            else
                TempData["ErrorMessage"] = "Personel silinemedi. Firmanın tek yöneticisi silinemez.";
            return RedirectToAction(nameof(EditCompany), new { id = companyId });
        }

        // 6. Ön Başvuru Onaylama
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveApplication(string applicationId)
        {
            var success = await _authService.ApproveApplicationAsync(applicationId);
            if (success)
            {
                TempData["SuccessMessage"] = "Ön başvuru başarıyla onaylandı. Tesis hesabı aktif edildi ve kullanıcıya onay e-postası iletildi.";
            }
            else
            {
                TempData["ErrorMessage"] = "Başvuru onaylanırken bir hata oluştu veya başvuru bulunamadı.";
            }
            return RedirectToAction(nameof(Companies));
        }
    }
}
