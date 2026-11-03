using DonguselIz.Web.Models.Domain;

namespace DonguselIz.Web.Models.ViewModels
{
    public record StatCard(string Label, string Value, string Hint);

    public record QuickAction(string Label, string Controller, string Action);

    public class PanelViewModel
    {
        public string Greeting { get; set; } = string.Empty;
        public string Subtitle { get; set; } = string.Empty;
        public List<StatCard> Stats { get; set; } = new();
        public List<QuickAction> Actions { get; set; } = new();
        public string RecentTitle { get; set; } = string.Empty;
        public List<WasteBatch> RecentBatches { get; set; } = new();
    }

    public class BatchCreateViewModel
    {
        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Malzeme türü seçilmeli veya yazılmalı.")]
        public string MaterialType { get; set; } = string.Empty;

        public string? LotNo { get; set; }

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Üretim hattı boş bırakılamaz.")]
        public string ProductionLine { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Range(1, 100000, ErrorMessage = "Ağırlık 1 ile 100000 kg arasında olmalı.")]
        public double WeightKg { get; set; }

        public string? Notes { get; set; }
        public string? ImageBase64 { get; set; }
        public string? AdditionalImagesJson { get; set; }

        // Yapay zeka analizinden gelen ve forma hidden field olarak gömülen değerler
        // Bunlar BatchController.Create'te BatchAnalyzer'a öncelik kazanır
        public double? AiPurityPercent { get; set; }
        public double? AiContaminationPercent { get; set; }
        public double? AiConfidencePercent { get; set; }

        public List<string> MaterialOptions { get; set; } = new();
    }

    public class BatchEditViewModel
    {
        [System.ComponentModel.DataAnnotations.Required]
        public string Id { get; set; } = string.Empty;

        public string LotNo { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Malzeme türü seçilmeli veya yazılmalı.")]
        public string MaterialType { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Üretim hattı boş bırakılamaz.")]
        public string ProductionLine { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Range(1, 100000, ErrorMessage = "Ağırlık 1 ile 100000 kg arasında olmalı.")]
        public double WeightKg { get; set; }

        public string? Notes { get; set; }
        public string? ImageBase64 { get; set; }
        public string? AdditionalImagesJson { get; set; }

        public string? ReviewNote { get; set; }
        public DonguselIz.Web.Models.Enums.BatchStatus Status { get; set; }

        public double? AiPurityPercent { get; set; }
        public double? AiContaminationPercent { get; set; }
        public double? AiConfidencePercent { get; set; }

        public List<string> MaterialOptions { get; set; } = new();
    }
}
