using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DonguselIz.Web.Data;
using DonguselIz.Web.Infrastructure.Filters;
using DonguselIz.Web.Models.Domain;
using DonguselIz.Web.Models.Enums;
using DonguselIz.Web.Models.ViewModels;
using DonguselIz.Web.Services;

namespace DonguselIz.Web.Controllers
{
    public class BatchController : Controller
    {
        private readonly AppDbContext _db;

        private readonly IVisionAnalysisService _visionService;

        public BatchController(AppDbContext db, IVisionAnalysisService visionService)
        {
            _db = db;
            _visionService = visionService;
        }

        private CompanyEmployee Me => (CompanyEmployee)HttpContext.Items["AuthenticatedEmployee"]!;

        // Gerçek Groq Llama 3.2 Vision Görsel Analiz Uç Noktası (Hibrit Taksonomi Korumalı)
        [AuthorizeRole(UserRole.WasteGenerator)]
        [HttpPost]
        public async Task<IActionResult> AnalyzeSample([FromBody] ImageAnalyzeRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.ImageBase64))
            {
                return BadRequest(new { message = "Görsel verisi bulunamadı." });
            }

            // Tesisin tanımlı çıktısı + Gaziantep OSB 6 sert standardı
            var hybridCatalog = OsbTaxonomy.BuildHybridCatalog(Me.Company?.OutputMaterialTypes);

            var result = await _visionService.AnalyzeImageAsync(request.ImageBase64, hybridCatalog);
            return Json(result);
        }

        public class ImageAnalyzeRequest
        {
            public string ImageBase64 { get; set; } = string.Empty;
        }

        // Üretici: kendi tesisinin partileri
        [AuthorizeRole(UserRole.WasteGenerator)]
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var list = await _db.Batches.Include(b => b.Company)
                .Where(b => b.CompanyId == Me.CompanyId)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();
            return View(list);
        }

        [AuthorizeRole(UserRole.WasteGenerator)]
        [HttpGet]
        public IActionResult Create()
        {
            return View(BuildCreateModel(new BatchCreateViewModel()));
        }

        [AuthorizeRole(UserRole.WasteGenerator)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BatchCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(BuildCreateModel(model));
            }

            var lotNo = string.IsNullOrWhiteSpace(model.LotNo)
                ? $"LOT-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..3].ToUpperInvariant()}"
                : model.LotNo.Trim();

            // Yapay zeka makul bir sonuç döndürdüyse onu kullan,
            // aksi hâlde BatchAnalyzer devreye girer (jüri demosu veya bağlantısız ortam)
            double purity, contamination;
            if (model.AiPurityPercent is >= 50 and <= 100)
            {
                purity = Math.Round(model.AiPurityPercent.Value, 1);
                contamination = Math.Round(100.0 - purity, 1);
            }
            else
            {
                var analysis = BatchAnalyzer.Analyze(model.MaterialType, lotNo, model.WeightKg);
                purity = analysis.PurityPercent;
                contamination = analysis.ContaminationPercent;
            }

            // Karbon kazanımı her iki yolda da malzeme ağırlığına göre hesaplanır
            var lower = model.MaterialType.ToLowerInvariant();
            var carbonFactor = lower.Contains("polipropilen") || lower.Contains("pp") ? 1.9
                : lower.Contains("pamuk") || lower.Contains("elyaf") ? 1.2
                : lower.Contains("ldpe") || lower.Contains("streç") ? 1.8
                : lower.Contains("pet") ? 1.5
                : 1.4;
            var carbonSaved = Math.Round(model.WeightKg * carbonFactor / 1000.0, 2);

            var batch = new WasteBatch
            {
                CompanyId = Me.CompanyId,
                CreatedByName = Me.FullName,
                MaterialType = model.MaterialType.Trim(),
                LotNo = lotNo,
                ProductionLine = model.ProductionLine.Trim(),
                Notes = model.Notes?.Trim() ?? string.Empty,
                WeightKg = model.WeightKg,
                ImageBase64 = model.ImageBase64,
                AdditionalImagesJson = model.AdditionalImagesJson,
                PurityPercent = purity,
                ContaminationPercent = contamination,
                CarbonSavedTons = carbonSaved,
                Status = BatchStatus.AiVerified
            };

            _db.Batches.Add(batch);
            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = "Parti kaydedildi ve OSB denetimine gönderildi.";
            return RedirectToAction(nameof(Details), new { id = batch.Id });
        }

        // Denetçi: bekleyen veya tüm partiler
        [AuthorizeRole(UserRole.OsbAuditor)]
        [HttpGet]
        public async Task<IActionResult> Audit(string filter = "pending")
        {
            var pendingCount = await _db.Batches.CountAsync(b => b.Status == BatchStatus.AiVerified);
            var revisionCount = await _db.Batches.CountAsync(b => b.Status == BatchStatus.RevisionRequested);
            var totalCount = await _db.Batches.CountAsync();

            ViewData["PendingCount"] = pendingCount;
            ViewData["RevisionCount"] = revisionCount;
            ViewData["TotalCount"] = totalCount;
            ViewData["Filter"] = filter;

            var query = _db.Batches.Include(b => b.Company).AsQueryable();
            if (filter == "pending")
            {
                query = query.Where(b => b.Status == BatchStatus.AiVerified);
            }
            else if (filter == "revision")
            {
                query = query.Where(b => b.Status == BatchStatus.RevisionRequested);
            }

            var list = await query.OrderByDescending(b => b.CreatedAt).ToListAsync();
            return View(list);
        }

        [AuthorizeRole(UserRole.OsbAuditor)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(string id)
        {
            var batch = await _db.Batches.FindAsync(id);
            if (batch == null || (batch.Status != BatchStatus.AiVerified && batch.Status != BatchStatus.RevisionRequested)) return NotFound();

            batch.Status = BatchStatus.PassportGenerated;
            batch.PassportCode = BatchAnalyzer.NewPassportCode();
            batch.ReviewedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = $"{batch.LotNo} onaylandı ve dijital pasaport oluşturuldu.";
            return RedirectToAction(nameof(Audit));
        }

        [AuthorizeRole(UserRole.OsbAuditor)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestRevision(string id, string? reviewNote)
        {
            var batch = await _db.Batches.FindAsync(id);
            if (batch == null || (batch.Status != BatchStatus.AiVerified && batch.Status != BatchStatus.RevisionRequested)) return NotFound();

            batch.Status = BatchStatus.RevisionRequested;
            batch.ReviewNote = string.IsNullOrWhiteSpace(reviewNote) ? "Lütfen parti verilerini ve numune fotoğrafını gözden geçiriniz." : reviewNote.Trim();
            batch.ReviewedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = $"{batch.LotNo} için revizyon talebi üreticiye iletildi.";
            return RedirectToAction(nameof(Audit));
        }

        [AuthorizeRole(UserRole.OsbAuditor)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(string id, string? reviewNote)
        {
            var batch = await _db.Batches.FindAsync(id);
            if (batch == null || (batch.Status != BatchStatus.AiVerified && batch.Status != BatchStatus.RevisionRequested)) return NotFound();

            batch.Status = BatchStatus.Rejected;
            batch.ReviewNote = string.IsNullOrWhiteSpace(reviewNote) ? "Gerekçe belirtilmedi." : reviewNote.Trim();
            batch.ReviewedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = $"{batch.LotNo} reddedildi.";
            return RedirectToAction(nameof(Audit));
        }

        // Üretici Tesis: Parti düzenleme / revizyon tamamlama
        [AuthorizeRole(UserRole.WasteGenerator)]
        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var batch = await _db.Batches.FirstOrDefaultAsync(b => b.Id == id && b.CompanyId == Me.CompanyId);
            if (batch == null) return NotFound();

            if (batch.Status != BatchStatus.AiVerified && batch.Status != BatchStatus.RevisionRequested && batch.Status != BatchStatus.Draft)
            {
                TempData["ErrorMessage"] = "Onaylanmış veya pazarda işlem gören partiler değiştirilemez.";
                return RedirectToAction(nameof(Details), new { id = batch.Id });
            }

            var model = new BatchEditViewModel
            {
                Id = batch.Id,
                LotNo = batch.LotNo,
                MaterialType = batch.MaterialType,
                ProductionLine = batch.ProductionLine,
                WeightKg = batch.WeightKg,
                Notes = batch.Notes,
                ImageBase64 = batch.ImageBase64,
                AdditionalImagesJson = batch.AdditionalImagesJson,
                ReviewNote = batch.ReviewNote,
                Status = batch.Status,
                MaterialOptions = OsbTaxonomy.BuildHybridCatalog(Me.Company?.OutputMaterialTypes)
            };

            return View(model);
        }

        [AuthorizeRole(UserRole.WasteGenerator)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(BatchEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.MaterialOptions = OsbTaxonomy.BuildHybridCatalog(Me.Company?.OutputMaterialTypes);
                return View(model);
            }

            var batch = await _db.Batches.FirstOrDefaultAsync(b => b.Id == model.Id && b.CompanyId == Me.CompanyId);
            if (batch == null) return NotFound();

            if (batch.Status != BatchStatus.AiVerified && batch.Status != BatchStatus.RevisionRequested && batch.Status != BatchStatus.Draft)
            {
                TempData["ErrorMessage"] = "Onaylanmış veya pazarda işlem gören partiler değiştirilemez.";
                return RedirectToAction(nameof(Details), new { id = batch.Id });
            }

            batch.MaterialType = model.MaterialType.Trim();
            batch.ProductionLine = model.ProductionLine.Trim();
            batch.WeightKg = model.WeightKg;
            batch.Notes = model.Notes?.Trim() ?? string.Empty;

            if (!string.IsNullOrEmpty(model.ImageBase64))
            {
                batch.ImageBase64 = model.ImageBase64;
            }
            if (model.AdditionalImagesJson != null)
            {
                batch.AdditionalImagesJson = model.AdditionalImagesJson;
            }

            if (model.AiPurityPercent is >= 50 and <= 100)
            {
                batch.PurityPercent = Math.Round(model.AiPurityPercent.Value, 1);
                batch.ContaminationPercent = Math.Round(100.0 - batch.PurityPercent, 1);
            }
            else
            {
                var analysis = BatchAnalyzer.Analyze(batch.MaterialType, batch.LotNo, batch.WeightKg);
                batch.PurityPercent = analysis.PurityPercent;
                batch.ContaminationPercent = analysis.ContaminationPercent;
            }

            var lower = batch.MaterialType.ToLowerInvariant();
            var carbonFactor = lower.Contains("polipropilen") || lower.Contains("pp") ? 1.9
                : lower.Contains("pamuk") || lower.Contains("elyaf") ? 1.2
                : lower.Contains("ldpe") || lower.Contains("streç") ? 1.8
                : lower.Contains("pet") ? 1.5
                : 1.4;
            batch.CarbonSavedTons = Math.Round(batch.WeightKg * carbonFactor / 1000.0, 2);

            // Revize edilen parti tekrar denetim kuyruğuna aktarılır
            batch.Status = BatchStatus.AiVerified;
            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = $"{batch.LotNo} başarıyla güncellendi ve OSB denetimine yeniden gönderildi.";
            return RedirectToAction(nameof(Details), new { id = batch.Id });
        }

        // Tüm roller: parti ve pasaport detayı, erişim kuralları içeride uygulanır
        [AuthorizeRole(UserRole.WasteGenerator, UserRole.RecyclerBuyer, UserRole.OsbAuditor)]
        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            var batch = await _db.Batches.Include(b => b.Company).FirstOrDefaultAsync(b => b.Id == id);
            if (batch == null) return NotFound();

            var allowed = Me.Role switch
            {
                UserRole.OsbAuditor => true,
                UserRole.WasteGenerator => batch.CompanyId == Me.CompanyId || batch.Status is BatchStatus.PassportGenerated or BatchStatus.Matched or BatchStatus.Recycled,
                UserRole.RecyclerBuyer => batch.Status is BatchStatus.PassportGenerated or BatchStatus.Matched or BatchStatus.Recycled,
                _ => false
            };

            if (!allowed)
            {
                return View("~/Views/Shared/AccessDenied.cshtml");
            }

            return View(batch);
        }

        // Resmi Döngüsel Karbon Sertifikası (ISO 14067 & AB ESPR Uyumlu A4 Resmi Belge)
        [AuthorizeRole(UserRole.WasteGenerator, UserRole.RecyclerBuyer, UserRole.OsbAuditor)]
        [HttpGet]
        public async Task<IActionResult> Certificate(string id)
        {
            var batch = await _db.Batches.Include(b => b.Company).FirstOrDefaultAsync(b => b.Id == id);
            if (batch == null) return NotFound();

            var allowed = Me.Role switch
            {
                UserRole.OsbAuditor => true,
                UserRole.WasteGenerator => batch.CompanyId == Me.CompanyId,
                UserRole.RecyclerBuyer => batch.Status is BatchStatus.PassportGenerated or BatchStatus.Matched or BatchStatus.Recycled,
                _ => false
            };

            if (!allowed)
            {
                return View("~/Views/Shared/AccessDenied.cshtml");
            }

            return View(batch);
        }

        // Tesisin kendi çıktı türleri ve Gaziantep OSB 6 sert standardı ile hibrit seçenek listesi üretir
        private BatchCreateViewModel BuildCreateModel(BatchCreateViewModel model)
        {
            model.MaterialOptions = OsbTaxonomy.BuildHybridCatalog(Me.Company?.OutputMaterialTypes);
            return model;
        }
    }
}
