namespace DonguselIz.Web.Models.ViewModels
{
    public class ClimateDashboardViewModel
    {
        public double TotalCarbonSavedTons { get; set; }
        public double ClosedLoopCarbonSavedTons { get; set; }
        public double TotalMaterialWeightTons { get; set; }
        public int TotalBatchesCount { get; set; }
        public int RecycledBatchesCount { get; set; }
        public int ActivePassportsCount { get; set; }

        // Ekolojik ve Ekonomik Dönüşüm Metrikleri
        public int EquivalentTreesPlanted { get; set; }
        public double ConservedCrudeOilBarrels { get; set; }
        public double DivertedLandfillCubicMeters { get; set; }
        public double EstimatedCbamTaxSavedEuros { get; set; }
        public double EstimatedCbamTaxSavedTry { get; set; }

        // Malzeme Türü Bazlı Karbon ve Ağırlık Dağılımı
        public List<MaterialClimateMetric> MaterialMetrics { get; set; } = new();

        // Tesisler Arası Karbon ve Döngüsellik Liderlik Tablosu
        public List<CompanyClimateLeader> TopCompanies { get; set; } = new();
    }

    public class MaterialClimateMetric
    {
        public string MaterialName { get; set; } = string.Empty;
        public double WeightTons { get; set; }
        public double CarbonSavedTons { get; set; }
        public int BatchCount { get; set; }
        public double PercentageShare { get; set; }
    }

    public class CompanyClimateLeader
    {
        public string CompanyName { get; set; } = string.Empty;
        public string OsbRegion { get; set; } = string.Empty;
        public string PrimaryRole { get; set; } = string.Empty;
        public double TotalCarbonSavedTons { get; set; }
        public double TotalWeightTons { get; set; }
        public int BatchCount { get; set; }
        public string PerformanceBadge { get; set; } = string.Empty;
    }
}
