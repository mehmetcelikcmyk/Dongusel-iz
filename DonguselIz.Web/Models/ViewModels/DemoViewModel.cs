using DonguselIz.Web.Models.Enums;

namespace DonguselIz.Web.Models.ViewModels
{
    public class DemoViewModel
    {
        public UserRole ActiveRole { get; set; } = UserRole.WasteGenerator;
        public string ActiveRoleTitle { get; set; } = "Üretici Fabrika Yetkilisi";

        // Örnek Analiz Metrikleri
        public string BatchCode { get; set; } = "DPP-GAZ-2026-0842";
        public string MaterialName { get; set; } = "Polipropilen (PP) Dokuma Firesi";
        public string MaterialOrigin { get; set; } = "Gaziantep OSB 4. Bölge - Dokuma Tesis B";
        public double QuantityKg { get; set; } = 1450.0;
        public double PurityScore { get; set; } = 94.8;
        public double ContaminationScore { get; set; } = 5.2;
        public double Co2SavedTon { get; set; } = 2.10;
        public string RecommendedBuyer { get; set; } = "Sanko Geri Dönüşüm & Elyaf Tesisleri";
        public double DistanceKm { get; set; } = 3.8;
        public string QrVerificationUrl { get; set; } = "https://dongusel-iz.gaziantep.gov.tr/dpp/verify/DPP-GAZ-2026-0842";
        
        // Şehir ve OSB Kümülatif Metrikleri
        public double TotalDivertedTons { get; set; } = 1845.0;
        public double TotalCo2Avoided { get; set; } = 2580.0;
        public int ActivePassportsCount { get; set; } = 342;
        public int RegisteredFacilitiesCount { get; set; } = 86;
    }
}
