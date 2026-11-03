using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DonguselIz.Web.Data;
using DonguselIz.Web.Models.Domain;

namespace DonguselIz.Web.Controllers
{
    // Halka Açık Pasaport Doğrulama Sayfası (Yetkilendirme GEREKTİRMEZ)
    // Gümrük denetçileri, OSB saha müfettişleri ve üçüncü taraflar QR kodu okutunca doğrudan buraya düşer.
    public class PassportController : Controller
    {
        private readonly AppDbContext _db;

        public PassportController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet("/passport/{code}")]
        [HttpGet("/Passport/Verify/{code}")]
        public async Task<IActionResult> Verify(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return NotFound();

            var cleanCode = code.Trim();
            var batch = await _db.Batches
                .Include(b => b.Company)
                .FirstOrDefaultAsync(b => b.PassportCode == cleanCode || b.Id == cleanCode);

            if (batch == null)
            {
                ViewBag.NotFoundCode = cleanCode;
                return View("NotFoundPassport");
            }

            return View(batch);
        }

        // Halka Açık Resmi Döngüsel Karbon Sertifikası (Yetkilendirme gerektirmez)
        [HttpGet("/passport/{code}/certificate")]
        public async Task<IActionResult> Certificate(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return NotFound();

            var cleanCode = code.Trim();
            var batch = await _db.Batches
                .Include(b => b.Company)
                .FirstOrDefaultAsync(b => b.PassportCode == cleanCode || b.Id == cleanCode);

            if (batch == null) return NotFound();

            return View("~/Views/Batch/Certificate.cshtml", batch);
        }
    }
}
