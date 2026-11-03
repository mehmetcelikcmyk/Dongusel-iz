using Microsoft.EntityFrameworkCore;
using DonguselIz.Web.Data;
using DonguselIz.Web.Models.Domain;
using DonguselIz.Web.Models.Enums;

namespace DonguselIz.Web.Services
{
    public interface IAuthService
    {
        Task<CompanyEmployee?> AuthenticateAsync(string email, string password);
        Task<CompanyEmployee?> GetEmployeeByEmailAsync(string email);

        // Şirket Operasyonları
        Task<IEnumerable<Company>> GetAllCompaniesWithEmployeesAsync();
        Task<Company?> GetCompanyByIdAsync(string id);
        Task<bool> CreateCompanyWithAdminAsync(Company company, string adminFullName, string adminEmail, string password);
        Task<bool> UpdateCompanyAsync(Company company);
        Task<bool> DeleteCompanyAsync(string id);

        // Personel Operasyonları
        Task<bool> AddEmployeeToCompanyAsync(CompanyEmployee employee, string password);
        Task<bool> UpdateEmployeeAsync(CompanyEmployee employee, string? newPassword);
        Task<bool> DeleteEmployeeAsync(string employeeId);

        // Ön Başvuru Yönetimi
        Task SubmitApplicationAsync(CompanyApplication application);
        Task<IEnumerable<CompanyApplication>> GetPendingApplicationsAsync();
        Task<bool> ApproveApplicationAsync(string applicationId, string? initialPassword = null);
    }

    public class EfAuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly IEmailSender _emailSender;

        public EfAuthService(AppDbContext context, IEmailSender emailSender)
        {
            _context = context;
            _emailSender = emailSender;
        }

        public async Task<CompanyEmployee?> AuthenticateAsync(string email, string password)
        {
            var emp = await _context.Employees
                .Include(e => e.Company)
                .FirstOrDefaultAsync(e => e.Email.ToLower() == email.ToLower() && e.IsActive);

            if (emp == null) return null;

            return BCryptVerify(password, emp.PasswordHash) ? emp : null;
        }

        public async Task<CompanyEmployee?> GetEmployeeByEmailAsync(string email)
        {
            return await _context.Employees
                .Include(e => e.Company)
                .FirstOrDefaultAsync(e => e.Email.ToLower() == email.ToLower() && e.IsActive);
        }

        public async Task<IEnumerable<Company>> GetAllCompaniesWithEmployeesAsync()
        {
            return await _context.Companies
                .Include(c => c.Employees)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<Company?> GetCompanyByIdAsync(string id)
        {
            return await _context.Companies
                .Include(c => c.Employees)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<bool> CreateCompanyWithAdminAsync(Company company, string adminFullName, string adminEmail, string password)
        {
            if (await _context.Companies.AnyAsync(c => c.TaxNumber == company.TaxNumber))
                return false;

            if (await _context.Employees.AnyAsync(e => e.Email.ToLower() == adminEmail.ToLower()))
                return false;

            company.CreatedAt = DateTime.UtcNow;
            await _context.Companies.AddAsync(company);

            var adminEmp = new CompanyEmployee
            {
                CompanyId = company.Id,
                FullName = adminFullName,
                Email = adminEmail.ToLower(),
                PasswordHash = BCryptHash(password),
                Title = "Tesis Yöneticisi / Baş Mühendis",
                Role = company.PrimaryRole,
                IsActive = true,
                IsCompanyManager = true,
                CreatedAt = DateTime.UtcNow
            };

            await _context.Employees.AddAsync(adminEmp);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateCompanyAsync(Company company)
        {
            var existing = await _context.Companies.FindAsync(company.Id);
            if (existing == null) return false;

            existing.Name = company.Name;
            existing.TaxNumber = company.TaxNumber;
            existing.OsbRegion = company.OsbRegion;
            existing.PrimaryRole = company.PrimaryRole;
            existing.OutputMaterialTypes = company.OutputMaterialTypes;
            existing.MonthlyOutputCapacityTons = company.MonthlyOutputCapacityTons;
            existing.FacilityAddress = company.FacilityAddress;
            existing.IsActive = company.IsActive;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteCompanyAsync(string id)
        {
            var comp = await _context.Companies.FindAsync(id);
            if (comp == null) return false;

            _context.Companies.Remove(comp);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> AddEmployeeToCompanyAsync(CompanyEmployee employee, string password)
        {
            if (await _context.Employees.AnyAsync(e => e.Email.ToLower() == employee.Email.ToLower()))
                return false;

            employee.PasswordHash = BCryptHash(password);
            employee.CreatedAt = DateTime.UtcNow;

            await _context.Employees.AddAsync(employee);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateEmployeeAsync(CompanyEmployee employee, string? newPassword)
        {
            var existing = await _context.Employees.FindAsync(employee.Id);
            if (existing == null) return false;

            existing.FullName = employee.FullName;
            existing.Title = employee.Title;
            existing.PhoneNumber = employee.PhoneNumber;
            existing.Role = employee.Role;
            existing.IsActive = employee.IsActive;
            existing.IsCompanyManager = employee.IsCompanyManager;

            if (!string.IsNullOrEmpty(newPassword))
            {
                existing.PasswordHash = BCryptHash(newPassword);
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteEmployeeAsync(string employeeId)
        {
            var emp = await _context.Employees.FindAsync(employeeId);
            if (emp == null) return false;

            // Firmanın tek yöneticisi silinemez, aksi halde firma kendini yönetemez
            if (emp.IsCompanyManager &&
                !await _context.Employees.AnyAsync(e => e.CompanyId == emp.CompanyId && e.IsCompanyManager && e.Id != emp.Id))
                return false;

            _context.Employees.Remove(emp);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task SubmitApplicationAsync(CompanyApplication application)
        {
            application.ApplicationDate = DateTime.UtcNow;
            await _context.Applications.AddAsync(application);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<CompanyApplication>> GetPendingApplicationsAsync()
        {
            return await _context.Applications
                .Where(a => !a.IsApproved)
                .OrderByDescending(a => a.ApplicationDate)
                .ToListAsync();
        }

        public async Task<bool> ApproveApplicationAsync(string applicationId, string? initialPassword = null)
        {
            var app = await _context.Applications.FirstOrDefaultAsync(a => a.Id == applicationId && !a.IsApproved);
            if (app == null) return false;

            app.IsApproved = true;

            var newCompany = new Company
            {
                Name = app.CompanyName,
                TaxNumber = app.TaxNumber,
                OsbRegion = app.OsbRegion,
                PrimaryRole = app.RequestedRole,
                OutputMaterialTypes = app.WasteOrMaterialDescription,
                IsActive = true
            };

            await _context.Companies.AddAsync(newCompany);

            // Başvuru anında belirlenen şifre hash'i doğrudan kullanılır veya initialPassword verildiyse hashlenir
            var passwordHash = !string.IsNullOrEmpty(app.PasswordHash) 
                ? app.PasswordHash 
                : BCryptHash(string.IsNullOrEmpty(initialPassword) ? "Sifre2026!" : initialPassword);

            var admin = new CompanyEmployee
            {
                CompanyId = newCompany.Id,
                FullName = app.ContactFullName,
                Email = app.Email.ToLower(),
                PhoneNumber = app.PhoneNumber,
                PasswordHash = passwordHash,
                Role = newCompany.PrimaryRole,
                Title = "Tesis Yöneticisi & Çevre Sorumlusu",
                IsCompanyManager = true,
                IsActive = true
            };

            await _context.Employees.AddAsync(admin);
            await _context.SaveChangesAsync();

            // E-posta bildirim gönderimi
            var mailBody = $"Sayın {app.ContactFullName},\n\n{app.CompanyName} tesisinizin Gaziantep OSB Döngüsel İz Malzeme Pasaportu ekosistemine katılım başvurusu Bölge Müdürlüğü Çevre Masası tarafından incelenerek onaylanmıştır.\n\nBaşvurunuz onaylanmıştır. Başvuru esnasında belirlediğiniz şifre ile sisteme doğrudan giriş yapabilirsiniz.\n\nGiriş Adresi: /Account/Login\n\nGaziantep Organize Sanayi Bölge Müdürlüğü";
            await _emailSender.SendEmailAsync(app.Email, "Gaziantep OSB - Tesis Başvurunuz Onaylandı", mailBody);

            return true;
        }

        public static string BCryptHash(string password)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var bytes = System.Text.Encoding.UTF8.GetBytes(password + "_DONGUSEL_IZ_SALT");
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        private static bool BCryptVerify(string password, string hash)
        {
            return BCryptHash(password) == hash;
        }
    }
}
