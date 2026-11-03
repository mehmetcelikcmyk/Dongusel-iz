using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DonguselIz.Web.Data;
using DonguselIz.Web.Infrastructure.Filters;
using DonguselIz.Web.Models.Domain;
using DonguselIz.Web.Models.Enums;
using DonguselIz.Web.Models.ViewModels;

namespace DonguselIz.Web.Controllers
{
    // Giriş yapan her rolün ilk gördüğü, role özel özet ekranı
    [AuthorizeRole(UserRole.WasteGenerator, UserRole.RecyclerBuyer, UserRole.OsbAuditor)]
    public class PanelController : Controller
    {
        private readonly AppDbContext _db;

        public PanelController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var me = (CompanyEmployee)HttpContext.Items["AuthenticatedEmployee"]!;
            var companyName = me.Company?.Name ?? string.Empty;

            var model = new PanelViewModel
            {
                Greeting = $"Hoş geldiniz {me.FullName}",
                Subtitle = $"{companyName} adına oturum açıldı."
            };

            switch (me.Role)
            {
                case UserRole.WasteGenerator:
                {
                    var mine = await _db.Batches.Where(b => b.CompanyId == me.CompanyId).ToListAsync();
                    var revisionCount = mine.Count(b => b.Status == BatchStatus.RevisionRequested);
                    if (revisionCount > 0)
                    {
                        ViewData["RevisionCount"] = revisionCount;
                    }
                    model.Stats.Add(new StatCard("Toplam Parti", mine.Count.ToString(), "Bildirilen tüm partiler"));
                    model.Stats.Add(new StatCard("Denetim Bekleyen", mine.Count(b => b.Status == BatchStatus.AiVerified).ToString(), "OSB onayı beklenen"));
                    if (revisionCount > 0)
                    {
                        model.Stats.Add(new StatCard("Revizyon Bekleyen", revisionCount.ToString(), "Düzeltme yapmanız gereken"));
                    }
                    model.Stats.Add(new StatCard("Pasaportlu Parti", mine.Count(b => b.Status is BatchStatus.PassportGenerated or BatchStatus.Matched).ToString(), "Pazara açık veya rezerve"));
                    model.Stats.Add(new StatCard("Toplam Ağırlık", $"{mine.Sum(b => b.WeightKg) / 1000.0:0.0} ton", "Bildirilen miktar"));
                    model.Actions.Add(new QuickAction("Yeni Parti Bildir", "Batch", "Create"));
                    model.Actions.Add(new QuickAction("Partilerimi Görüntüle", "Batch", "Index"));
                    model.Actions.Add(new QuickAction("Firmam ve Ekibim", "Company", "Index"));
                    model.RecentTitle = "Son Bildirilen Partiler";
                    model.RecentBatches = mine.OrderByDescending(b => b.CreatedAt).Take(5).ToList();
                    break;
                }
                case UserRole.RecyclerBuyer:
                {
                    var companyId = me.CompanyId;
                    var myOrders = await _db.Batches.Include(b => b.Company)
                        .Where(b => b.ReservedByCompanyId == companyId || b.ReservedByCompanyName == companyName)
                        .OrderByDescending(b => b.ReservedAt ?? b.ReviewedAt)
                        .ToListAsync();

                    var completed = myOrders.Where(b => b.Status == BatchStatus.Recycled).ToList();
                    var inTransit = myOrders.Where(b => b.Status != BatchStatus.Recycled).ToList();

                    var totalWeightTons = myOrders.Sum(b => b.WeightKg) / 1000.0;
                    var totalCarbon = myOrders.Sum(b => b.CarbonSavedTons);
                    var avgPurity = myOrders.Any() ? myOrders.Average(b => b.PurityPercent) : 95.0;

                    model.Stats.Add(new StatCard("Aktif Tedarik", inTransit.Count.ToString(), "Sevkiyat/Kabul bekleyen"));
                    model.Stats.Add(new StatCard("Kazanılan Hammadde", $"{totalWeightTons:0.0} ton", "Tesisinize giren ikincil polimer"));
                    model.Stats.Add(new StatCard("Önlenen CO₂", $"{totalCarbon:0.00} tCO2", "Tesisinizin ESG kazanımı"));
                    model.Stats.Add(new StatCard("Ortalama Saflık", $"%{avgPurity:0.0}", "Gelen hammadde kalite indeksi"));

                    model.Actions.Add(new QuickAction("Hammadde Pazarına Git", "Market", "Index"));
                    model.Actions.Add(new QuickAction("Rezervasyon ve İrsaliye Kabulü", "Market", "Reservations"));
                    model.Actions.Add(new QuickAction("Firmam ve Ekibim", "Company", "Index"));

                    model.RecentTitle = "Tesisinizin Tedarik ve Rezervasyon Akışı";
                    model.RecentBatches = myOrders.Take(5).ToList();
                    break;
                }
                default:
                {
                    var all = await _db.Batches.Include(b => b.Company).ToListAsync();
                    var pendingApplications = await _db.Applications.CountAsync(a => !a.IsApproved);
                    var companyCount = await _db.Companies.CountAsync();
                    model.Stats.Add(new StatCard("Denetim Bekleyen Parti", all.Count(b => b.Status == BatchStatus.AiVerified).ToString(), "Karar bekliyor"));
                    model.Stats.Add(new StatCard("Bekleyen Başvuru", pendingApplications.ToString(), "Katılım talepleri"));
                    model.Stats.Add(new StatCard("Kayıtlı Tesis", companyCount.ToString(), "Tüm roller"));
                    model.Stats.Add(new StatCard("Önlenen Karbon", $"{all.Where(b => b.Status is BatchStatus.PassportGenerated or BatchStatus.Matched).Sum(b => b.CarbonSavedTons):0.00} tCO2e", "Onaylı partilerden"));
                    model.Actions.Add(new QuickAction("Parti Denetimine Git", "Batch", "Audit"));
                    model.Actions.Add(new QuickAction("Tesis ve Personel Masası", "Admin", "Companies"));
                    model.RecentTitle = "Son Gelen Partiler";
                    model.RecentBatches = all.OrderByDescending(b => b.CreatedAt).Take(5).ToList();
                    break;
                }
            }

            return View(model);
        }
    }
}
