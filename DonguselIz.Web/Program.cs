using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.HttpOverrides;
using DonguselIz.Web.Data;
using DonguselIz.Web.Models.Domain;
using DonguselIz.Web.Models.Enums;
using DonguselIz.Web.Services;

using DonguselIz.Web.Infrastructure.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Gerçek SQLite Veri Tabanı
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=dongusel_iz.db"));

// Oturum ve Güvenlik Altyapısı
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(24);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Veri Tabanı Destekli Yetkilendirme Servisi (Scoped)
builder.Services.AddScoped<IAuthService, EfAuthService>();

// Kriptografik Güvenli Token Servisi (Beni Hatırla)
builder.Services.AddSingleton<ITokenService, TokenService>();

// Gerçek Görsel Yapay Zeka Analiz Servisi (Groq Llama 3.2 Vision)
builder.Services.AddHttpClient();
builder.Services.AddScoped<IVisionAnalysisService, GroqVisionService>();

// Akıllı Endüstriyel Simbiyoz ve Eşleştirme Motoru
builder.Services.AddScoped<ISymbiosisService, SymbiosisService>();

// E-Posta Bildirim Servisi (Simülasyon / Log)
builder.Services.AddScoped<IEmailSender, SimulatedEmailSender>();

var app = builder.Build();

// Veri tabanı tablolarını oluştur ve sabit kurumsal şirketleri/personelleri hazırla
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();
    
    dbContext.Database.EnsureCreated();

    // Eski veri tabanlarında IsCompanyManager sütunu yoksa ekler ve her firmanın ilk personelini yönetici yapar
    var hasManagerColumn = dbContext.Database
        .SqlQueryRaw<int>("SELECT COUNT(*) AS \"Value\" FROM pragma_table_info('Employees') WHERE name = 'IsCompanyManager'")
        .AsEnumerable().First() > 0;
    if (!hasManagerColumn)
    {
        dbContext.Database.ExecuteSqlRaw("ALTER TABLE \"Employees\" ADD COLUMN \"IsCompanyManager\" INTEGER NOT NULL DEFAULT 0");
        dbContext.Database.ExecuteSqlRaw(@"UPDATE ""Employees"" SET ""IsCompanyManager"" = 1
            WHERE ""Id"" IN (
                SELECT e.""Id"" FROM ""Employees"" e
                WHERE e.""CreatedAt"" = (SELECT MIN(x.""CreatedAt"") FROM ""Employees"" x WHERE x.""CompanyId"" = e.""CompanyId"")
                GROUP BY e.""CompanyId"")");
    }

    var hasImageColumn = dbContext.Database
        .SqlQueryRaw<int>("SELECT COUNT(*) AS \"Value\" FROM pragma_table_info('Batches') WHERE name = 'ImageBase64'")
        .AsEnumerable().FirstOrDefault() > 0;
    if (!hasImageColumn)
    {
        dbContext.Database.ExecuteSqlRaw("ALTER TABLE \"Batches\" ADD COLUMN \"ImageBase64\" TEXT NULL");
    }

    void EnsureBatchColumn(string colName, string colType)
    {
        var hasCol = dbContext.Database
            .SqlQueryRaw<int>($"SELECT COUNT(*) AS \"Value\" FROM pragma_table_info('Batches') WHERE name = '{colName}'")
            .AsEnumerable().FirstOrDefault() > 0;
        if (!hasCol)
        {
            dbContext.Database.ExecuteSqlRaw($"ALTER TABLE \"Batches\" ADD COLUMN \"{colName}\" {colType}");
        }
    }

    EnsureBatchColumn("ReservedByCompanyId", "TEXT NULL");
    EnsureBatchColumn("ReservedAt", "TEXT NULL");
    EnsureBatchColumn("ReservationExpiresAt", "TEXT NULL");
    EnsureBatchColumn("WaybillNo", "TEXT NULL");
    EnsureBatchColumn("ScaleReceiptNo", "TEXT NULL");
    EnsureBatchColumn("ScaleWeightKg", "REAL NULL");
    EnsureBatchColumn("DeliveredAt", "TEXT NULL");
    EnsureBatchColumn("AdditionalImagesJson", "TEXT NULL");

    var hasAppPasswordCol = dbContext.Database
        .SqlQueryRaw<int>("SELECT COUNT(*) AS \"Value\" FROM pragma_table_info('Applications') WHERE name = 'PasswordHash'")
        .AsEnumerable().FirstOrDefault() > 0;
    if (!hasAppPasswordCol)
    {
        dbContext.Database.ExecuteSqlRaw("ALTER TABLE \"Applications\" ADD COLUMN \"PasswordHash\" TEXT NOT NULL DEFAULT ''");
    }

    // Var olan veri tabanına yeni Batches tablosunu veri kaybetmeden ekler
    dbContext.Database.ExecuteSqlRaw(@"CREATE TABLE IF NOT EXISTS ""Batches"" (
        ""Id"" TEXT NOT NULL CONSTRAINT ""PK_Batches"" PRIMARY KEY,
        ""CompanyId"" TEXT NOT NULL,
        ""CreatedByName"" TEXT NOT NULL,
        ""MaterialType"" TEXT NOT NULL,
        ""LotNo"" TEXT NOT NULL,
        ""ProductionLine"" TEXT NOT NULL,
        ""Notes"" TEXT NOT NULL,
        ""WeightKg"" REAL NOT NULL,
        ""PurityPercent"" REAL NOT NULL,
        ""ContaminationPercent"" REAL NOT NULL,
        ""CarbonSavedTons"" REAL NOT NULL,
        ""Status"" INTEGER NOT NULL,
        ""PassportCode"" TEXT NOT NULL,
        ""ReservedByCompanyName"" TEXT NULL,
        ""ReviewNote"" TEXT NULL,
        ""ReviewedAt"" TEXT NULL,
        ""CreatedAt"" TEXT NOT NULL,
        CONSTRAINT ""FK_Batches_Companies_CompanyId"" FOREIGN KEY (""CompanyId"") REFERENCES ""Companies"" (""Id"") ON DELETE CASCADE
    );");

    // 1. Gaziantep OSB Bölge Müdürlüğü (Yönetici & Denetçi Kurumu)
    if (!dbContext.Companies.Any(c => c.TaxNumber == "1000000001"))
    {
        var osbCompany = new Company
        {
            Name = "Gaziantep OSB Bölge Müdürlüğü",
            TaxNumber = "1000000001",
            OsbRegion = "OSB Başkanlık Binası",
            PrimaryRole = UserRole.OsbAuditor,
            OutputMaterialTypes = "Bölge Geneli Malzeme ve Karbon Denetim Raporu",
            MonthlyOutputCapacityTons = 0,
            FacilityAddress = "Gaziantep OSB 1. Bölge Mah. 83101 Nolu Cadde No:2",
            IsActive = true
        };

        authService.CreateCompanyWithAdminAsync(
            osbCompany, 
            "Mehmet Çelik", 
            "admin@gazianteposb.org.tr", 
            "Admin2026!").GetAwaiter().GetResult();
    }

    // 2. Merinos Halı Sanayi ve Ticaret A.Ş. (Üretici Tesis - Polipropilen & Halı İpliği Firesi)
    if (!dbContext.Companies.Any(c => c.TaxNumber == "6190283410"))
    {
        var merinos = new Company
        {
            Name = "Merinos Halı Sanayi ve Ticaret A.Ş.",
            TaxNumber = "6190283410",
            OsbRegion = "Gaziantep OSB 4. Bölge",
            PrimaryRole = UserRole.WasteGenerator,
            OutputMaterialTypes = "Homopolimer Polipropilen (PP) Dokuma Firesi | Halı Çözgü Telefi",
            MonthlyOutputCapacityTons = 120.0,
            FacilityAddress = "4. Organize Sanayi Bölgesi 83414 Nolu Cad. No:8",
            IsActive = true
        };

        authService.CreateCompanyWithAdminAsync(
            merinos, 
            "Ahmet Yılmaz", 
            "saha@merinos.com.tr", 
            "Uretici2026!").GetAwaiter().GetResult();

        // 2. Personel Ekleyelim (Örn: Saha Vardiya Amiri)
        authService.AddEmployeeToCompanyAsync(new CompanyEmployee
        {
            CompanyId = merinos.Id,
            FullName = "Mustafa Demir",
            Email = "vardiya@merinos.com.tr",
            Title = "2. Hat Atık ve Saha Sorumlusu",
            PhoneNumber = "03423371002",
            Role = UserRole.WasteGenerator,
            IsActive = true
        }, "Vardiya2026!").GetAwaiter().GetResult();
    }

    // 3. Sanko Geri Dönüşüm & Elyaf Tesisleri (Alıcı Sanayici - Rejenerasyon & Elyaf Çevrimi)
    if (!dbContext.Companies.Any(c => c.TaxNumber == "7420198823"))
    {
        var sanko = new Company
        {
            Name = "Sanko Geri Dönüşüm & Elyaf Tesisleri",
            TaxNumber = "7420198823",
            OsbRegion = "Gaziantep OSB 2. Bölge",
            PrimaryRole = UserRole.RecyclerBuyer,
            OutputMaterialTypes = "Geri Kazanılmış İkincil Polimer Granülü | Pamuk Elyafı",
            MonthlyOutputCapacityTons = 250.0,
            FacilityAddress = "2. Organize Sanayi Bölgesi Sanayi Cad. No:14",
            IsActive = true
        };

        authService.CreateCompanyWithAdminAsync(
            sanko, 
            "Zeynep Kaya", 
            "tedarik@sanko.com.tr", 
            "Alici2026!").GetAwaiter().GetResult();
    }

    // 4. Örnek partiler, ekranların boş görünmemesi için bir kez eklenir
    if (!dbContext.Batches.Any())
    {
        var merinosCompany = dbContext.Companies.FirstOrDefault(c => c.TaxNumber == "6190283410");
        if (merinosCompany != null)
        {
            var seeds = new[]
            {
                (Lot: "LOT-20261001-101", Material: "Homopolimer Polipropilen (PP) Dokuma Firesi", Line: "Dokuma Hattı 2", Kg: 2400.0, Status: BatchStatus.PassportGenerated),
                (Lot: "LOT-20261008-102", Material: "Halı Çözgü Telefi", Line: "Çözgü Hattı 1", Kg: 1250.0, Status: BatchStatus.AiVerified),
                (Lot: "LOT-20261012-103", Material: "Homopolimer Polipropilen (PP) Dokuma Firesi", Line: "Dokuma Hattı 1", Kg: 3100.0, Status: BatchStatus.AiVerified)
            };

            foreach (var s in seeds)
            {
                var analysis = BatchAnalyzer.Analyze(s.Material, s.Lot, s.Kg);
                dbContext.Batches.Add(new WasteBatch
                {
                    CompanyId = merinosCompany.Id,
                    CreatedByName = "Saha Sorumlusu",
                    MaterialType = s.Material,
                    LotNo = s.Lot,
                    ProductionLine = s.Line,
                    WeightKg = s.Kg,
                    PurityPercent = analysis.PurityPercent,
                    ContaminationPercent = analysis.ContaminationPercent,
                    CarbonSavedTons = analysis.CarbonSavedTons,
                    Status = s.Status,
                    PassportCode = s.Status == BatchStatus.PassportGenerated ? BatchAnalyzer.NewPassportCode() : string.Empty,
                    ReviewedAt = s.Status == BatchStatus.PassportGenerated ? DateTime.UtcNow : null
                });
            }
            dbContext.SaveChanges();
        }
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost
});

app.UseStaticFiles();

app.UseRouting();

app.UseSession();
app.UseRememberMe();

app.UseAuthorization();

// Ana karşılama ekranı
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Demo}/{action=Index}/{id?}");

app.Run();
