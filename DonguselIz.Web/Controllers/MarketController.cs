using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DonguselIz.Web.Data;
using DonguselIz.Web.Infrastructure.Filters;
using DonguselIz.Web.Models.Domain;
using DonguselIz.Web.Models.Enums;
using DonguselIz.Web.Services;

namespace DonguselIz.Web.Controllers
{
    // Hammadde Pazarı
    // Listeleme tüm roller ve paydaşlara açıktır; rezervasyon ve kantar kabul işlemleri Alıcı/Geri Dönüşümcü tesis yetkisi gerektirir.
    public class MarketController : Controller
    {
        private readonly AppDbContext _db;
        private readonly ISymbiosisService _symbiosisService;

        public MarketController(AppDbContext db, ISymbiosisService symbiosisService)
        {
            _db = db;
            _symbiosisService = symbiosisService;
        }

        private CompanyEmployee? CurrentUser =>
            HttpContext.Items.TryGetValue("AuthenticatedEmployee", out var val) ? val as CompanyEmployee : null;

        private CompanyEmployee RequiredBuyer =>
            (CompanyEmployee)HttpContext.Items["AuthenticatedEmployee"]!;

        [HttpGet]
        public async Task<IActionResult> Index(string? q = null)
        {
            // 72 saatlik opsiyon süresi dolan partileri otomatik boşa çıkarıp pazara iade et
            var now = DateTime.UtcNow;
            var expired = await _db.Batches
                .Where(b => b.Status == BatchStatus.Matched && b.ReservationExpiresAt != null && b.ReservationExpiresAt < now)
                .ToListAsync();

            if (expired.Any())
            {
                foreach (var exp in expired)
                {
                    exp.Status = BatchStatus.PassportGenerated;
                    exp.ReservedByCompanyId = null;
                    exp.ReservedByCompanyName = null;
                    exp.ReservedAt = null;
                    exp.ReservationExpiresAt = null;
                }
                await _db.SaveChangesAsync();
            }

            var query = _db.Batches.Include(b => b.Company)
                .Where(b => b.Status == BatchStatus.PassportGenerated);

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                query = query.Where(b => b.MaterialType.Contains(term) || b.LotNo.Contains(term));
            }

            ViewData["Query"] = q;
            var list = await query.ToListAsync();

            var user = CurrentUser;
            var isBuyer = user?.Role == UserRole.RecyclerBuyer;
            ViewBag.CanReserve = isBuyer;

            // Oturum açan alıcı firmanın güncel profilini çek ve simbiyoz skorunu hesapla
            if (isBuyer && user != null && !string.IsNullOrEmpty(user.CompanyId))
            {
                var buyerCompany = await _db.Companies.FirstOrDefaultAsync(c => c.Id == user.CompanyId) ?? user.Company;
                if (buyerCompany != null)
                {
                    var ranked = _symbiosisService.RankBatchesForBuyer(buyerCompany, list);
                    ViewBag.BuyerCompany = buyerCompany;
                    return View(ranked);
                }
            }

            // Üretici tesis, OSB denetçisi veya anonim ziyaretçi için genel liste
            var defaultResults = list.Select(b => new SymbiosisMatchResult
            {
                Batch = b,
                MatchScore = 80,
                Reasons = new List<string> { "Onaylanmış pasaporta sahip döngüsel sanayi hammaddesi" }
            }).ToList();

            return View(defaultResults);
        }

        [AuthorizeRole(UserRole.RecyclerBuyer)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reserve(string id)
        {
            var batch = await _db.Batches.FindAsync(id);
            if (batch == null || batch.Status != BatchStatus.PassportGenerated)
            {
                TempData["ErrorMessage"] = "Bu parti artık rezerve edilemiyor veya başka bir tesis tarafından rezerve edildi.";
                return RedirectToAction(nameof(Index));
            }

            var buyer = RequiredBuyer;
            batch.Status = BatchStatus.Matched;
            batch.ReservedByCompanyId = buyer.CompanyId;
            batch.ReservedByCompanyName = buyer.Company?.Name ?? buyer.Email;
            batch.ReservedAt = DateTime.UtcNow;
            batch.ReservationExpiresAt = DateTime.UtcNow.AddHours(72);
            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = $"{batch.LotNo} firmanız adına 72 saat süreyle rezerve edildi. Lütfen bu süre içinde sevk ve kantar kabulünü tamamlayınız.";
            return RedirectToAction(nameof(Reservations));
        }

        [AuthorizeRole(UserRole.RecyclerBuyer)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelReservation(string id)
        {
            var buyer = RequiredBuyer;
            var companyId = buyer.CompanyId;
            var companyName = buyer.Company?.Name ?? buyer.Email;
            var batch = await _db.Batches.FindAsync(id);

            if (batch == null || (batch.ReservedByCompanyId != companyId && batch.ReservedByCompanyName != companyName))
            {
                TempData["ErrorMessage"] = "Parti bulunamadı veya rezervasyonu iptal etme yetkiniz yok.";
                return RedirectToAction(nameof(Reservations));
            }

            if (batch.Status == BatchStatus.Recycled)
            {
                TempData["ErrorMessage"] = "Kabulü tamamlanmış bir partinin rezervasyonu iptal edilemez.";
                return RedirectToAction(nameof(Reservations));
            }

            batch.Status = BatchStatus.PassportGenerated;
            batch.ReservedByCompanyId = null;
            batch.ReservedByCompanyName = null;
            batch.ReservedAt = null;
            batch.ReservationExpiresAt = null;
            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = $"{batch.LotNo} rezervasyonu iptal edildi ve parti hammadde pazarına geri açıldı.";
            return RedirectToAction(nameof(Reservations));
        }

        [AuthorizeRole(UserRole.RecyclerBuyer)]
        [HttpGet]
        public async Task<IActionResult> Reservations()
        {
            var buyer = RequiredBuyer;
            var companyId = buyer.CompanyId;
            var companyName = buyer.Company?.Name ?? buyer.Email;
            var list = await _db.Batches.Include(b => b.Company)
                .Where(b => b.ReservedByCompanyId == companyId || b.ReservedByCompanyName == companyName)
                .OrderByDescending(b => b.ReservedAt ?? b.ReviewedAt)
                .ToListAsync();
            return View(list);
        }

        // 3. Malzeme Kantar Tartımı, Sevk İrsaliyesi ve Kabul Onayı (Döngü Tamamlama)
        [AuthorizeRole(UserRole.RecyclerBuyer)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmDelivery(string id, double? scaleWeightKg, string? waybillNo, string? scaleReceiptNo)
        {
            var buyer = RequiredBuyer;
            var companyId = buyer.CompanyId;
            var companyName = buyer.Company?.Name ?? buyer.Email;
            var batch = await _db.Batches.FindAsync(id);
            if (batch == null || (batch.ReservedByCompanyId != companyId && batch.ReservedByCompanyName != companyName))
            {
                TempData["ErrorMessage"] = "Parti bulunamadı veya bu parti için işlem yetkiniz yok.";
                return RedirectToAction(nameof(Reservations));
            }

            batch.Status = BatchStatus.Recycled;
            batch.DeliveredAt = DateTime.UtcNow;

            if (scaleWeightKg.HasValue && scaleWeightKg.Value > 0)
            {
                batch.ScaleWeightKg = scaleWeightKg.Value;
            }
            if (!string.IsNullOrWhiteSpace(waybillNo))
            {
                batch.WaybillNo = waybillNo.Trim();
            }
            if (!string.IsNullOrWhiteSpace(scaleReceiptNo))
            {
                batch.ScaleReceiptNo = scaleReceiptNo.Trim();
            }

            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = $"{batch.LotNo} tesisinize kantar ve sevk doğrulamasıyla başarıyla kabul edildi ve döngüsel süreç tamamlandı.";
            return RedirectToAction(nameof(Reservations));
        }
    }
}
