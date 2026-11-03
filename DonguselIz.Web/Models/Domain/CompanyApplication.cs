using System.ComponentModel.DataAnnotations;
using DonguselIz.Web.Models.Enums;

namespace DonguselIz.Web.Models.Domain
{
    public class CompanyApplication
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
        public string CompanyName { get; set; } = string.Empty;
        public string TaxNumber { get; set; } = string.Empty;
        public string OsbRegion { get; set; } = string.Empty;
        public string ContactFullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public UserRole RequestedRole { get; set; }
        public string WasteOrMaterialDescription { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public DateTime ApplicationDate { get; set; } = DateTime.UtcNow;
        public bool IsApproved { get; set; } = false;
    }
}
