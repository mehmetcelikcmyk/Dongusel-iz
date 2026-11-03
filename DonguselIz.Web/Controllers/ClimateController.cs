using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DonguselIz.Web.Data;
using DonguselIz.Web.Models.Enums;
using DonguselIz.Web.Models.ViewModels;

namespace DonguselIz.Web.Controllers
{
    // Gaziantep OSB İklim, Karbon ve Döngüsel Ekonomi Karnesi
    // Hem giriş yapmış kullanıcılara hem dış paydaşlara açıktır (Yetkilendirme gerektirmez)
    public class ClimateController : Controller
    {
        private readonly AppDbContext _db;

        public ClimateController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var batches = await _db.Batches
                .Include(b => b.Company)
                .ToListAsync();

            var totalCarbon = Math.Round(batches.Sum(b => b.CarbonSavedTons), 2);
            var closedLoopBatches = batches.Where(b => b.Status == BatchStatus.Recycled).ToList();
            var closedLoopCarbon = Math.Round(closedLoopBatches.Sum(b => b.CarbonSavedTons), 2);
            var totalWeightKg = batches.Sum(b => b.WeightKg);
            var totalWeightTons = Math.Round(totalWeightKg / 1000.0, 1);

            // Ekolojik ve AB SKDM (CBAM) Formülleri
            // 1 ton CO2 engelleme = 45 olgun ağacın yıllık karbon tutumu
            var trees = (int)Math.Round(totalCarbon * 45);
            // 1 ton polimer geri kazanımı = ~3.8 varil ham petrol tasarrufu
            var oilBarrels = Math.Round(totalWeightTons * 3.8, 1);
            // Çöp sahası hacmi: yoğunluk ortalaması ~ 350 kg/m3 -> 1 ton atık ~ 2.85 m3 çöp sahası tasarrufu
            var landfillM3 = Math.Round(totalWeightTons * 2.85, 1);
            // AB SKDM (CBAM) Karbon Vergisi Kalkanı: AB ETS ton başına ortalama 85 Euro (~ 3.150 TL)
            var cbamEuros = Math.Round(totalCarbon * 85.0, 0);
            var cbamTry = Math.Round(cbamEuros * 37.0, 0); // Güncel EUR/TRY parite projeksiyonu

            // Malzeme bazlı dağılım
            var matGroups = batches
                .GroupBy(b => NormalizeMaterial(b.MaterialType))
                .Select(g => new MaterialClimateMetric
                {
                    MaterialName = g.Key,
                    WeightTons = Math.Round(g.Sum(b => b.WeightKg) / 1000.0, 1),
                    CarbonSavedTons = Math.Round(g.Sum(b => b.CarbonSavedTons), 2),
                    BatchCount = g.Count(),
                    PercentageShare = totalWeightKg > 0 ? Math.Round((g.Sum(b => b.WeightKg) / totalWeightKg) * 100.0, 1) : 0
                })
                .OrderByDescending(m => m.CarbonSavedTons)
                .ToList();

            // Tesis liderlik tablosu
            var companyLeaders = batches
                .Where(b => b.Company != null)
                .GroupBy(b => b.Company!)
                .Select(g => new CompanyClimateLeader
                {
                    CompanyName = g.Key.Name,
                    OsbRegion = g.Key.OsbRegion,
                    PrimaryRole = g.Key.PrimaryRole == UserRole.WasteGenerator ? "Üretici Tesis" : "Geri Dönüşümcü",
                    TotalCarbonSavedTons = Math.Round(g.Sum(b => b.CarbonSavedTons), 2),
                    TotalWeightTons = Math.Round(g.Sum(b => b.WeightKg) / 1000.0, 1),
                    BatchCount = g.Count(),
                    PerformanceBadge = g.Sum(b => b.CarbonSavedTons) >= 10.0 ? "A+ Yeşil Lider" :
                                       g.Sum(b => b.CarbonSavedTons) >= 5.0 ? "A İklim Öncüsü" : "B+ Döngüsel Katılımcı"
                })
                .OrderByDescending(c => c.TotalCarbonSavedTons)
                .ToList();

            var model = new ClimateDashboardViewModel
            {
                TotalCarbonSavedTons = totalCarbon,
                ClosedLoopCarbonSavedTons = closedLoopCarbon,
                TotalMaterialWeightTons = totalWeightTons,
                TotalBatchesCount = batches.Count,
                RecycledBatchesCount = closedLoopBatches.Count,
                ActivePassportsCount = batches.Count(b => !string.IsNullOrEmpty(b.PassportCode)),
                EquivalentTreesPlanted = trees,
                ConservedCrudeOilBarrels = oilBarrels,
                DivertedLandfillCubicMeters = landfillM3,
                EstimatedCbamTaxSavedEuros = cbamEuros,
                EstimatedCbamTaxSavedTry = cbamTry,
                MaterialMetrics = matGroups,
                TopCompanies = companyLeaders
            };

            return View(model);
        }

        private static string NormalizeMaterial(string raw)
        {
            var lower = raw.ToLowerInvariant();
            if (lower.Contains("polipropilen") || lower.Contains("pp")) return "Polipropilen (PP) Elyaf & Dokuma";
            if (lower.Contains("pamuk") || lower.Contains("telef") || lower.Contains("iplik")) return "Ham Pamuk Telefi & Halı İpliği";
            if (lower.Contains("ldpe") || lower.Contains("streç") || lower.Contains("film")) return "LDPE Ambalaj & Palet Streç Filmi";
            if (lower.Contains("karton") || lower.Contains("mukavva") || lower.Contains("koli")) return "Oluklu Mukavva & Ambalaj Atığı";
            if (lower.Contains("pet") || lower.Contains("polyester")) return "Polyester & PET Çapak";
            return "Diğer İkincil Polimer ve Karışımlar";
        }
    }
}
