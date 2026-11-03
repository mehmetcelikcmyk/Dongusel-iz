using System.ComponentModel.DataAnnotations;
using DonguselIz.Web.Models.Domain;
using DonguselIz.Web.Models.Enums;

namespace DonguselIz.Web.Models.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Kurumsal e-posta adresi zorunludur.")]
        [EmailAddress(ErrorMessage = "Geçerli bir kurumsal e-posta adresi giriniz.")]
        [Display(Name = "Kurumsal E-Posta")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Parola zorunludur.")]
        [DataType(DataType.Password)]
        [Display(Name = "Parola")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Beni Hatırla")]
        public bool RememberMe { get; set; }
    }

    public class CompanyApplicationViewModel
    {
        [Required(ErrorMessage = "Firma resmi ünvanı zorunludur.")]
        [Display(Name = "Firma Resmi Ünvanı")]
        public string CompanyName { get; set; } = string.Empty;

        [Required(ErrorMessage = "10 haneli VKN zorunludur.")]
        [StringLength(10, MinimumLength = 10, ErrorMessage = "Vergi Kimlik Numarası 10 haneli olmalıdır.")]
        [Display(Name = "Vergi Kimlik Numarası (VKN)")]
        public string TaxNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "OSB lokasyonu seçilmelidir.")]
        [Display(Name = "Gaziantep OSB Bölgesi")]
        public string OsbRegion { get; set; } = "Gaziantep OSB 1. Bölge";

        [Required(ErrorMessage = "Yetkili ad soyad zorunludur.")]
        [Display(Name = "Tesis Yetkilisi Adı Soyadı")]
        public string ContactFullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Kurumsal e-posta adresi zorunludur.")]
        [EmailAddress(ErrorMessage = "Geçerli bir kurumsal e-posta adresi giriniz.")]
        [Display(Name = "Yetkili Kurumsal E-Posta")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "İletişim telefonu zorunludur.")]
        [Display(Name = "Yetkili Telefonu")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Faaliyet rolü seçilmelidir.")]
        [Display(Name = "Ekosistemdeki Faaliyet Türü")]
        public UserRole RequestedRole { get; set; } = UserRole.WasteGenerator;

        [Required(ErrorMessage = "Üretilen veya talep edilen malzeme çıktısı zorunludur.")]
        [Display(Name = "Tesis Malzeme Çıktıları ve Fire Türleri")]
        public string WasteOrMaterialDescription { get; set; } = string.Empty;

        [Display(Name = "Aylık Tahmini Çıktı / Fire Kapasitesi (Ton)")]
        public double MonthlyOutputCapacityTons { get; set; } = 50.0;

        [Required(ErrorMessage = "Giriş şifresi zorunludur.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Şifreniz en az 6 karakter uzunluğunda olmalıdır.")]
        [DataType(DataType.Password)]
        [Display(Name = "Sisteme Giriş Şifresi")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Şifre Tekrarı")]
        [Compare("Password", ErrorMessage = "Girdiğiniz şifreler birbiriyle uyuşmuyor.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Range(typeof(bool), "true", "true", ErrorMessage = "Başvuru taahhütnamesini onaylamanız gerekmektedir.")]
        public bool TermsAccepted { get; set; }
    }

    // Tesis ve Personel Yönetim Masası Modeli
    public class CompanyManagementViewModel
    {
        public IEnumerable<Company> Companies { get; set; } = new List<Company>();
        public IEnumerable<CompanyApplication> PendingApplications { get; set; } = new List<CompanyApplication>();

        // Yeni Tesis Kayıt Alanları
        public Company NewCompany { get; set; } = new();

        // İlk Yetkili Personel Bilgisi
        [Required(ErrorMessage = "Yetkili ad soyad zorunludur.")]
        public string InitialAdminFullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Yetkili kurumsal e-posta zorunludur.")]
        [EmailAddress(ErrorMessage = "Geçerli e-posta giriniz.")]
        public string InitialAdminEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "İlk giriş şifresi zorunludur.")]
        public string InitialPassword { get; set; } = "Sifre2026!";
    }

    // Tesis Düzenleme Modeli
    public class CompanyEditViewModel
    {
        [Required]
        public string Id { get; set; } = string.Empty;

        [Required(ErrorMessage = "Firma adı zorunludur.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "VKN zorunludur.")]
        [StringLength(10, MinimumLength = 10, ErrorMessage = "VKN 10 haneli olmalıdır.")]
        public string TaxNumber { get; set; } = string.Empty;

        [Required]
        public string OsbRegion { get; set; } = string.Empty;

        public UserRole PrimaryRole { get; set; }

        public string OutputMaterialTypes { get; set; } = string.Empty;
        public double MonthlyOutputCapacityTons { get; set; }
        public string FacilityAddress { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;

        public List<CompanyEmployee> Employees { get; set; } = new();
    }

    // Persone Ekleme / Düzenleme Modeli
    public class EmployeeCreateOrEditViewModel
    {
        public string? EmployeeId { get; set; }

        [Required]
        public string CompanyId { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ad soyad zorunludur.")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Kurumsal e-posta zorunludur.")]
        [EmailAddress(ErrorMessage = "Geçerli e-posta giriniz.")]
        public string Email { get; set; } = string.Empty;

        public string Title { get; set; } = "Vardiya ve Atık Sorumlusu";
        public string PhoneNumber { get; set; } = string.Empty;

        public string? Password { get; set; }
        public UserRole Role { get; set; } = UserRole.WasteGenerator;
    }
}
