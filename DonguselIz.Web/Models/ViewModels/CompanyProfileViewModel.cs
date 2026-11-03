using DonguselIz.Web.Models.Domain;

namespace DonguselIz.Web.Models.ViewModels
{
    public class CompanyProfileViewModel
    {
        public Company Company { get; set; } = new();
        public string CurrentEmployeeId { get; set; } = string.Empty;
        public bool IsManager { get; set; }
        public int TotalBatches { get; set; }
        public double TotalWeightKg { get; set; }
        public Dictionary<string, int> BatchCountByPerson { get; set; } = new();
    }
}
