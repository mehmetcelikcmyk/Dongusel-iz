using DonguselIz.Web.Models.Domain;

namespace DonguselIz.Web.Services
{
    public class SymbiosisMatchResult
    {
        public WasteBatch Batch { get; set; } = null!;
        public int MatchScore { get; set; }
        public string MatchBadgeClass { get; set; } = "symbiosis-high"; // high, mid, low
        public List<string> Reasons { get; set; } = new();
        public double EstimatedLogisticsDistanceKm { get; set; } = 3.5;
        public string AdvantageSummary { get; set; } = string.Empty;
    }

    public interface ISymbiosisService
    {
        SymbiosisMatchResult EvaluateMatch(Company buyerCompany, WasteBatch batch);
        List<SymbiosisMatchResult> RankBatchesForBuyer(Company buyerCompany, IEnumerable<WasteBatch> batches);
    }

    public class SymbiosisService : ISymbiosisService
    {
        public SymbiosisMatchResult EvaluateMatch(Company buyerCompany, WasteBatch batch)
        {
            var result = new SymbiosisMatchResult
            {
                Batch = batch,
                MatchScore = 50 // Başlangıç taban puanı
            };

            var buyerNeeds = (buyerCompany.OutputMaterialTypes ?? "").ToLowerInvariant();
            var batchMaterial = (batch.MaterialType ?? "").ToLowerInvariant();

            // 1. Malzeme Türü Uyumu (En kritik kriter)
            bool isDirectMatch = false;
            if (buyerNeeds.Contains("polimer") || buyerNeeds.Contains("granül") || buyerNeeds.Contains("plastik"))
            {
                if (batchMaterial.Contains("pp") || batchMaterial.Contains("polipropilen") || batchMaterial.Contains("film") || batchMaterial.Contains("ldpe") || batchMaterial.Contains("dokuma"))
                {
                    result.MatchScore += 30;
                    result.Reasons.Add("Polimer/Plastik geri dönüşüm hattınız ile doğrudan hammadde uyumu.");
                    isDirectMatch = true;
                }
            }
            
            if (buyerNeeds.Contains("pamuk") || buyerNeeds.Contains("elyaf") || buyerNeeds.Contains("iplik") || buyerNeeds.Contains("tekstil"))
            {
                if (batchMaterial.Contains("iplik") || batchMaterial.Contains("pamuk") || batchMaterial.Contains("çözgü") || batchMaterial.Contains("atkı") || batchMaterial.Contains("kumaş"))
                {
                    result.MatchScore += 30;
                    result.Reasons.Add("Tekstil rejenere elyaf kırma üniteniz için doğrudan uygun lif yapısı.");
                    isDirectMatch = true;
                }
            }

            if (!isDirectMatch)
            {
                var words = batchMaterial.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                foreach (var w in words)
                {
                    if (w.Length > 2 && buyerNeeds.Contains(w))
                    {
                        result.MatchScore += 15;
                        result.Reasons.Add($"'{w}' malzeme profili tesis gereksinimlerinizle kısmi eşleşiyor.");
                        break;
                    }
                }
            }

            // 2. Kimyasal Saflık ve Kirlilik Toleransı
            if (batch.PurityPercent >= 93.0)
            {
                result.MatchScore += 15;
                result.Reasons.Add($"Yüksek saflık (%{batch.PurityPercent:0.0}): Ek yıkama maliyeti olmadan doğrudan ekstrüdere beslenebilir.");
            }
            else if (batch.PurityPercent >= 85.0)
            {
                result.MatchScore += 5;
                result.Reasons.Add($"Standart sanayi saflığı (%{batch.PurityPercent:0.0}): Normal eriyik filtreleme ile işlenebilir.");
            }
            else
            {
                result.MatchScore -= 10;
                result.Reasons.Add($"Düşük saflık (%{batch.PurityPercent:0.0}): Ön ayıklama veya yoğun yıkama havuzu gerekebilir.");
            }

            // Kirlilik toleransı
            if (batch.ContaminationPercent <= 4.0)
            {
                result.MatchScore += 10;
                result.Reasons.Add($"Minimal kirlilik (%{batch.ContaminationPercent:0.0}): Düşük fire ve yüksek geri kazanım verimi.");
            }
            else if (batch.ContaminationPercent > 10.0)
            {
                result.MatchScore -= 10;
            }

            // 3. Gaziantep OSB Coğrafi Yakınlık & Karbon Avantajı
            var buyerRegion = buyerCompany.OsbRegion ?? "";
            var sellerRegion = batch.Company?.OsbRegion ?? "";

            if (!string.IsNullOrEmpty(buyerRegion) && !string.IsNullOrEmpty(sellerRegion))
            {
                if (buyerRegion.Equals(sellerRegion, StringComparison.OrdinalIgnoreCase))
                {
                    result.MatchScore += 12;
                    result.EstimatedLogisticsDistanceKm = 1.2;
                    result.Reasons.Add($"Aynı OSB Bölgesi ({buyerRegion}): Fabrikalar arası sıfıra yakın nakliye emisyonu ve hızlı sevkiyat.");
                }
                else
                {
                    result.MatchScore += 5;
                    result.EstimatedLogisticsDistanceKm = 4.8;
                    result.Reasons.Add($"Gaziantep OSB İçi ({sellerRegion} ➔ {buyerRegion}): Şehirlerarası lojistik masrafı yok.");
                }
            }

            // Skoru 10 - 99 aralığında sınırla
            result.MatchScore = Math.Clamp(result.MatchScore, 10, 99);

            // Rozet sınıfı ve özet
            if (result.MatchScore >= 80)
            {
                result.MatchBadgeClass = "symbiosis-high";
                result.AdvantageSummary = "Yüksek Simbiyoz Uyumu · Öncelikli Tavsiye";
            }
            else if (result.MatchScore >= 60)
            {
                result.MatchBadgeClass = "symbiosis-mid";
                result.AdvantageSummary = "İyi Seviye Simbiyoz · İşlenebilir Hammadde";
            }
            else
            {
                result.MatchBadgeClass = "symbiosis-low";
                result.AdvantageSummary = "Düşük Uyum · İlave İşleme Gerekebilir";
            }

            return result;
        }

        public List<SymbiosisMatchResult> RankBatchesForBuyer(Company buyerCompany, IEnumerable<WasteBatch> batches)
        {
            var list = new List<SymbiosisMatchResult>();
            foreach (var b in batches)
            {
                list.Add(EvaluateMatch(buyerCompany, b));
            }

            return list.OrderByDescending(r => r.MatchScore).ToList();
        }
    }
}
