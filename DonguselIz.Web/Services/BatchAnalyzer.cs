using DonguselIz.Web.Models.Enums;

namespace DonguselIz.Web.Services
{
    public record BatchAnalysisResult(double PurityPercent, double ContaminationPercent, double CarbonSavedTons);

    // Prototip analiz servisi. Gerçek görsel modelin yerine geçer ve aynı lot için hep aynı sonucu üretir.
    public static class BatchAnalyzer
    {
        public static BatchAnalysisResult Analyze(string materialType, string lotNo, double weightKg)
        {
            // Lot numarasından deterministik bir sayı türet
            var seed = 17;
            foreach (var ch in lotNo) seed = unchecked(seed * 31 + ch);
            var fraction = Math.Abs(seed % 100) / 100.0;

            var purity = Math.Round(88.0 + fraction * 9.0, 1);
            var contamination = Math.Round(100.0 - purity, 1);

            // Malzeme başına tahmini kilogram başına karbon kazanımı
            var lower = materialType.ToLowerInvariant();
            var factor = lower.Contains("polipropilen") || lower.Contains("pp") ? 1.9
                : lower.Contains("pamuk") || lower.Contains("elyaf") ? 1.2
                : lower.Contains("ldpe") || lower.Contains("streç") ? 1.8
                : lower.Contains("pet") ? 1.5
                : 1.4;

            var carbon = Math.Round(weightKg * factor / 1000.0, 2);
            return new BatchAnalysisResult(purity, contamination, carbon);
        }

        public static string NewPassportCode() =>
            $"DPP-GAZ-{DateTime.UtcNow.Year}-{Guid.NewGuid().ToString("N")[..5].ToUpperInvariant()}";
    }

    // Parti durumları için ekranda görünecek metin ve renk sınıfı
    public static class BatchStatusDisplay
    {
        public static string Label(BatchStatus status) => status switch
        {
            BatchStatus.AiVerified => "Denetim Bekliyor",
            BatchStatus.PassportGenerated => "Pasaportlu Yayında",
            BatchStatus.Matched => "Rezerve Edildi",
            BatchStatus.InTransit => "Sevkiyatta",
            BatchStatus.Recycled => "Teslim Alındı (Döngü Kapandı)",
            BatchStatus.Rejected => "Reddedildi",
            BatchStatus.RevisionRequested => "Revizyon İstendi",
            _ => "Taslak"
        };

        public static string CssClass(BatchStatus status) => status switch
        {
            BatchStatus.AiVerified => "badge-warn",
            BatchStatus.PassportGenerated => "badge-ok",
            BatchStatus.Matched => "badge-info",
            BatchStatus.InTransit => "badge-warn",
            BatchStatus.Recycled => "badge-ok",
            BatchStatus.Rejected => "badge-danger",
            BatchStatus.RevisionRequested => "badge-warn",
            _ => "badge-muted"
        };
    }
}
