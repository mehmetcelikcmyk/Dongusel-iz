namespace DonguselIz.Web.Models.Enums
{
    public enum UserRole
    {
        WasteGenerator = 1,     // Üretici Fabrika Atık Sorumlusu
        RecyclerBuyer = 2,      // Geri Dönüşümcü / İkincil Hammadde Alıcısı
        OsbAuditor = 3          // Gaziantep OSB / ESG Karbon Denetçisi
    }

    public enum MaterialCategory
    {
        Polypropylene = 1,      // Polipropilen (PP Dokuma / Halı İpliği)
        CottonTextile = 2,      // Pamuk Elyaf / Kırpıntı Telefi
        LdpeFilm = 3,           // LDPE Palet Ambalaj Streç Filmi
        PetFlake = 4,           // PET Şişe / Çapak
        CorrugatedCardboard = 5 // Oluklu Mukavva Koli Atığı
    }

    public enum BatchStatus
    {
        Draft = 1,
        AiVerified = 2,
        PassportGenerated = 3,
        Matched = 4,
        InTransit = 5,
        Recycled = 6,
        Rejected = 7,
        RevisionRequested = 8
    }
}
