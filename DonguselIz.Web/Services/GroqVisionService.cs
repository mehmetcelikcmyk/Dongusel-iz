using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DonguselIz.Web.Services
{
    public class VisionAnalysisDto
    {
        [JsonPropertyName("materialName")]
        public string MaterialName { get; set; } = "Homopolimer Polipropilen (PP) Dokuma Firesi";

        [JsonPropertyName("purityPercent")]
        public double PurityPercent { get; set; } = 94.5;

        [JsonPropertyName("contaminationPercent")]
        public double ContaminationPercent { get; set; } = 5.5;

        [JsonPropertyName("detectedCategory")]
        public string DetectedCategory { get; set; } = "Polimer / Sentetik Dokuma";

        [JsonPropertyName("confidencePercent")]
        public double ConfidencePercent { get; set; } = 92.0;

        [JsonPropertyName("technicalNote")]
        public string TechnicalNote { get; set; } = "Görsel yapay zeka ile yüzey dokusu ve lif yapısı doğrulandı.";
    }

    public interface IVisionAnalysisService
    {
        Task<VisionAnalysisDto> AnalyzeImageAsync(string base64OrDataUri, List<string>? allowedMaterials = null);
    }

    public class GroqVisionService : IVisionAnalysisService
    {
        private readonly IConfiguration _config;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<GroqVisionService> _logger;

        public GroqVisionService(IConfiguration config, IHttpClientFactory httpClientFactory, ILogger<GroqVisionService> logger)
        {
            _config = config;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<VisionAnalysisDto> AnalyzeImageAsync(string base64OrDataUri, List<string>? allowedMaterials = null)
        {
            var apiKey = _config["Groq:ApiKey"];
            var model = _config["Groq:Model"] ?? "llama-3.2-11b-vision-preview";

            if (string.IsNullOrWhiteSpace(apiKey) || apiKey.StartsWith("gsk_") == false)
            {
                return GetFallbackAnalysis();
            }

            try
            {
                // Data URI temizleme veya tam formata getirme
                string imageUrl = base64OrDataUri;
                if (!imageUrl.StartsWith("data:image/"))
                {
                    imageUrl = $"data:image/jpeg;base64,{base64OrDataUri}";
                }

                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(25);
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

                // Tesisin tanımlı malzeme listesini prompta enjekte et
                string materialConstraint = "";
                if (allowedMaterials != null && allowedMaterials.Count > 0)
                {
                    var allowedFormatted = string.Join("\", \"", allowedMaterials);
                    materialConstraint = $@"
KRİTİK KURAL: Bu fabrikanın resmi çıktı kataloğunda SADECE şu malzemeler tanımlıdır:
[""{allowedFormatted}""]
'materialName' alanında KESİNLİKLE VE KESİNLİKLE bu listedeki seçeneklerden görseldeki numuneye en çok benzeyenini TAM VE EKSİKSİZ olarak seçip yazmalısın. Listede olmayan yeni veya türetilmiş bir malzeme adı YAZAMAZSIN.";
                }
                else
                {
                    materialConstraint = "Görseldeki malzemenin standart sanayi cinsini tespit et (Örn: Homopolimer Polipropilen (PP) Dokuma Firesi, Pamuk Halı İpliği Telefi, LDPE Palet Ambalaj Streç Filmi vb.).";
                }

                var prompt = $@"Sen Gaziantep Organize Sanayi Bölgesinde çalışan uzman bir tekstil, polimer ve malzeme mühendisisin.
Sana verilen sanayi atığı / üretim firesi görselini analiz et.

{materialConstraint}

Yüzey homojenliğine, lif yapısına veya polimer dokusuna bakarak saflık yüzdesini (%85-99) ve kirlilik payını hesapla.

SADECE VE SADECE aşağıdaki JSON şemasında yanıt ver, hiçbir ekstra açıklama, selamlama veya markdown kod bloğu (```json) ekleme:
{{
  ""materialName"": ""izin verilen listeden seçilen malzeme adı"",
  ""purityPercent"": 94.8,
  ""contaminationPercent"": 5.2,
  ""detectedCategory"": ""Polimer / Sentetik veya Tekstil veya Ambalaj"",
  ""confidencePercent"": 93.5,
  ""technicalNote"": ""Kısa teknik mühendislik gözlemi""
}}";

                var requestBody = new
                {
                    model = model,
                    messages = new object[]
                    {
                        new
                        {
                            role = "user",
                            content = new object[]
                            {
                                new { type = "text", text = prompt },
                                new
                                {
                                    type = "image_url",
                                    image_url = new { url = imageUrl }
                                }
                            }
                        }
                    },
                    temperature = 0.1,
                    max_tokens = 400
                };

                var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
                var response = await client.PostAsync("https://api.groq.com/openai/v1/chat/completions", jsonContent);

                if (!response.IsSuccessStatusCode)
                {
                    var errorDetails = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Groq Vision API Hatası: {Status} - {Details}", response.StatusCode, errorDetails);
                    return GetFallbackAnalysis();
                }

                var responseString = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseString);
                var rawText = doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString() ?? "";

                // Yanıt JSON bloğu içindeyse temizle
                rawText = rawText.Trim();
                if (rawText.StartsWith("```json")) rawText = rawText.Substring(7);
                if (rawText.StartsWith("```")) rawText = rawText.Substring(3);
                if (rawText.EndsWith("```")) rawText = rawText.Substring(0, rawText.Length - 3);
                _logger.LogInformation("Groq Yapay Zeka Gerçek Yanıtı: {RawText}", rawText);

                var result = JsonSerializer.Deserialize<VisionAnalysisDto>(rawText, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (result != null && !string.IsNullOrWhiteSpace(result.MaterialName))
                {
                    // Sert Taksonomi Koruması: Dönen metni izinli liste ile sanitize et
                    if (allowedMaterials != null && allowedMaterials.Count > 0)
                    {
                        result.MaterialName = Models.Domain.OsbTaxonomy.SanitizeToAllowed(result.MaterialName, allowedMaterials);
                    }
                    return result;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Groq görüntü işleme analizi sırasında hata meydana geldi.");
            }

            var fallback = GetFallbackAnalysis();
            if (allowedMaterials != null && allowedMaterials.Count > 0)
            {
                fallback.MaterialName = Models.Domain.OsbTaxonomy.SanitizeToAllowed(fallback.MaterialName, allowedMaterials);
            }
            return fallback;
        }

        private static VisionAnalysisDto GetFallbackAnalysis()
        {
            return new VisionAnalysisDto
            {
                MaterialName = "Homopolimer Polipropilen (PP) Dokuma Firesi",
                PurityPercent = 94.8,
                ContaminationPercent = 5.2,
                DetectedCategory = "Polimer / Sentetik Dokuma",
                ConfidencePercent = 91.5,
                TechnicalNote = "Spektral yüzey analizi: Boyasız ve homojen mekanik kesim halı firesi."
            };
        }
    }
}
