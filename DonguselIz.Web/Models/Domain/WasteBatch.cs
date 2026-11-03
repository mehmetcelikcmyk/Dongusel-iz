using System.ComponentModel.DataAnnotations;
using DonguselIz.Web.Models.Enums;

namespace DonguselIz.Web.Models.Domain
{
    // Üretici tesisin bildirdiği tek bir atık veya ikincil hammadde partisi
    public class WasteBatch
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];

        [Required]
        public string CompanyId { get; set; } = string.Empty;
        public Company? Company { get; set; }

        public string CreatedByName { get; set; } = string.Empty;
        public string MaterialType { get; set; } = string.Empty;
        public string LotNo { get; set; } = string.Empty;
        public string ProductionLine { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;

        public double WeightKg { get; set; }
        public string? ImageBase64 { get; set; }
        public string? AdditionalImagesJson { get; set; }

        public List<string> GetAllImages()
        {
            var list = new List<string>();
            if (!string.IsNullOrWhiteSpace(ImageBase64))
            {
                list.Add(ImageBase64);
            }
            if (!string.IsNullOrWhiteSpace(AdditionalImagesJson))
            {
                try
                {
                    var extras = System.Text.Json.JsonSerializer.Deserialize<List<string>>(AdditionalImagesJson);
                    if (extras != null)
                    {
                        foreach (var img in extras)
                        {
                            if (!string.IsNullOrWhiteSpace(img) && !list.Contains(img))
                            {
                                list.Add(img);
                            }
                        }
                    }
                }
                catch { }
            }
            return list;
        }

        // Analiz çıktıları
        public double PurityPercent { get; set; }
        public double ContaminationPercent { get; set; }
        public double CarbonSavedTons { get; set; }

        public BatchStatus Status { get; set; } = BatchStatus.AiVerified;
        public string PassportCode { get; set; } = string.Empty;

        public string? ReservedByCompanyId { get; set; }
        public Company? ReservedCompany { get; set; }
        public string? ReservedByCompanyName { get; set; }
        public DateTime? ReservedAt { get; set; }
        public DateTime? ReservationExpiresAt { get; set; }

        // Sanayi kantar ve sevk kabul alanları
        public string? WaybillNo { get; set; }
        public string? ScaleReceiptNo { get; set; }
        public double? ScaleWeightKg { get; set; }
        public DateTime? DeliveredAt { get; set; }

        public string? ReviewNote { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
