using System.ComponentModel.DataAnnotations;
using DonguselIz.Web.Models.Enums;

namespace DonguselIz.Web.Models.Domain
{
    // 1. Kurumsal Firma / Tesis Varlığı
    public class Company
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(10)]
        public string TaxNumber { get; set; } = string.Empty;

        [Required]
        public string OsbRegion { get; set; } = string.Empty;

        public UserRole PrimaryRole { get; set; } = UserRole.WasteGenerator;

        // Tesisin Çıktıları ve Atık/Hammadde Profili
        public string OutputMaterialTypes { get; set; } = string.Empty; // Örn: Polipropilen Çuval Firesi, Streç Film
        public double MonthlyOutputCapacityTons { get; set; } = 0;      // Aylık Çıktı / Fire Kapasitesi (Ton)
        public string FacilityAddress { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Şirketin Tanımlı Personelleri
        public List<CompanyEmployee> Employees { get; set; } = new();
    }

    // 2. Şirkete Bağlı Personel / Kullanıcı Hesabı
    public class CompanyEmployee
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];

        [Required]
        public string CompanyId { get; set; } = string.Empty;
        public Company? Company { get; set; }

        [Required]
        public string Email { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        public string FullName { get; set; } = string.Empty;

        public string Title { get; set; } = "Çevre Mühendisi / Vardiya Sorumlusu";
        public string PhoneNumber { get; set; } = string.Empty;

        public UserRole Role { get; set; } = UserRole.WasteGenerator;
        public bool IsActive { get; set; } = true;

        // Firma yöneticisi kendi firmasının personelini ve çıktı bilgilerini yönetebilir
        public bool IsCompanyManager { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
