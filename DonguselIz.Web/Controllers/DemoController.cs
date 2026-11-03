using Microsoft.AspNetCore.Mvc;
using DonguselIz.Web.Models.ViewModels;
using DonguselIz.Web.Models.Enums;

namespace DonguselIz.Web.Controllers
{
    public class DemoController : Controller
    {
        [HttpGet]
        public IActionResult Index(int role = 1)
        {
            var userRole = Enum.IsDefined(typeof(UserRole), role) ? (UserRole)role : UserRole.WasteGenerator;

            var model = new DemoViewModel
            {
                ActiveRole = userRole,
                ActiveRoleTitle = userRole switch
                {
                    UserRole.WasteGenerator => "Üretici Fabrika Yetkilisi",
                    UserRole.RecyclerBuyer => "Geri Dönüşümcü & Alıcı Tesis",
                    UserRole.OsbAuditor => "Gaziantep OSB & Karbon Denetçisi",
                    _ => "Sistem Kullanıcısı"
                }
            };

            return View(model);
        }
    }
}
