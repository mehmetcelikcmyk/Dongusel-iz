namespace DonguselIz.Web.Models.Domain
{
    // Gaziantep OSB Resmi Malzeme ve Atık Taksonomisi (Sert Standart Sözlük)
    public static class OsbTaxonomy
    {
        public static readonly List<string> StandardCategories = new()
        {
            "Homopolimer Polipropilen (PP) Dokuma / Halı Firesi",
            "LDPE Palet Ambalaj ve Streç Filmi",
            "Rejenere Pamuk Elyafı / Dokuma Telefi",
            "Polyester (PES) Halı & Kumaş Kırpıntısı",
            "Oluklu Mukavva & Karton Ambalaj Atığı",
            "PET Şişe & Levha Çapağı"
        };

        // Fabrikanın özel çıktısıyla OSB genel taksonomisini harmanlayarak tekil bir geçerli liste üretir
        public static List<string> BuildHybridCatalog(string? companyOutputs)
        {
            var list = new List<string>();

            if (!string.IsNullOrWhiteSpace(companyOutputs))
            {
                var customItems = companyOutputs
                    .Split(new[] { '|', ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Distinct();
                list.AddRange(customItems);
            }

            foreach (var std in StandardCategories)
            {
                if (!list.Any(x => x.Equals(std, StringComparison.OrdinalIgnoreCase)))
                {
                    list.Add(std);
                }
            }

            return list;
        }

        // Yapay zekanın cevabını kesinlikle izin verilen listeden birine eşitleyen koruma filtresi
        public static string SanitizeToAllowed(string detectedName, List<string> allowedList)
        {
            if (string.IsNullOrWhiteSpace(detectedName))
                return allowedList.FirstOrDefault() ?? StandardCategories[0];

            // 1. Birebir eşleşme
            var exact = allowedList.FirstOrDefault(x => x.Equals(detectedName.Trim(), StringComparison.OrdinalIgnoreCase));
            if (exact != null) return exact;

            // 2. Kısmi kelime benzerliği eşleşmesi
            var lower = detectedName.ToLowerInvariant();
            if (lower.Contains("pp") || lower.Contains("polipropilen") || lower.Contains("dokuma") || lower.Contains("halı"))
            {
                var match = allowedList.FirstOrDefault(x => x.Contains("PP", StringComparison.OrdinalIgnoreCase) || x.Contains("Dokuma", StringComparison.OrdinalIgnoreCase));
                if (match != null) return match;
            }
            if (lower.Contains("streç") || lower.Contains("film") || lower.Contains("ldpe") || lower.Contains("ambalaj"))
            {
                var match = allowedList.FirstOrDefault(x => x.Contains("LDPE", StringComparison.OrdinalIgnoreCase) || x.Contains("Streç", StringComparison.OrdinalIgnoreCase));
                if (match != null) return match;
            }
            if (lower.Contains("pamuk") || lower.Contains("elyaf") || lower.Contains("iplik") || lower.Contains("telef"))
            {
                var match = allowedList.FirstOrDefault(x => x.Contains("Pamuk", StringComparison.OrdinalIgnoreCase) || x.Contains("Elyaf", StringComparison.OrdinalIgnoreCase));
                if (match != null) return match;
            }
            if (lower.Contains("polyester") || lower.Contains("pes") || lower.Contains("kumaş"))
            {
                var match = allowedList.FirstOrDefault(x => x.Contains("Polyester", StringComparison.OrdinalIgnoreCase) || x.Contains("PES", StringComparison.OrdinalIgnoreCase));
                if (match != null) return match;
            }
            if (lower.Contains("karton") || lower.Contains("koli") || lower.Contains("mukavva"))
            {
                var match = allowedList.FirstOrDefault(x => x.Contains("Mukavva", StringComparison.OrdinalIgnoreCase) || x.Contains("Karton", StringComparison.OrdinalIgnoreCase));
                if (match != null) return match;
            }
            if (lower.Contains("pet") || lower.Contains("çapak") || lower.Contains("şişe"))
            {
                var match = allowedList.FirstOrDefault(x => x.Contains("PET", StringComparison.OrdinalIgnoreCase));
                if (match != null) return match;
            }

            // Fallback: İlk geçerli madde
            return allowedList.FirstOrDefault() ?? StandardCategories[0];
        }
    }
}
