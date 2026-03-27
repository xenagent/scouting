using System.Text;
using System.Text.Json;
using Amazon.BedrockRuntime;
using Amazon.BedrockRuntime.Model;

namespace Scouting.Web.Services;

/// <summary>
/// Production AI evaluation service using AWS Bedrock → Claude Opus.
///
/// Required configuration:
///   Bedrock__ModelId   e.g. "anthropic.claude-opus-4-6-v1"
///   Bedrock__Region    e.g. "us-east-1"
///   AWS credentials via IAM role or env vars (AWS_ACCESS_KEY_ID / AWS_SECRET_ACCESS_KEY)
/// </summary>
public class BedrockAIAnalysisService : IAIAnalysisService
{
    private readonly AmazonBedrockRuntimeClient _client;
    private readonly string _modelId;

    public BedrockAIAnalysisService(IConfiguration config)
    {
        var region = config["Bedrock:Region"] ?? "us-east-1";
        _modelId   = config["Bedrock:ModelId"] ?? "anthropic.claude-opus-4-6-v1";
        _client    = new AmazonBedrockRuntimeClient(Amazon.RegionEndpoint.GetBySystemName(region));
    }

    public async Task<AIEvaluationResult> EvaluateAsync(AIAnalysisInput input, CancellationToken ct = default)
    {
        var prompt  = BuildPrompt(input);
        var payload = JsonSerializer.Serialize(new
        {
            anthropic_version = "bedrock-2023-05-31",
            max_tokens        = 1024,
            temperature       = 0.1,
            messages          = new[] { new { role = "user", content = prompt } }
        });

        var request = new InvokeModelRequest
        {
            ModelId     = _modelId,
            ContentType = "application/json",
            Accept      = "application/json",
            Body        = new MemoryStream(Encoding.UTF8.GetBytes(payload))
        };

        InvokeModelResponse response;
        try
        {
            response = await _client.InvokeModelAsync(request, ct);
        }
        catch (Exception)
        {
            // Bedrock unavailable → fall back to graceful empty result
            return new AIEvaluationResult { IsAvailable = false };
        }

        using var reader = new StreamReader(response.Body);
        var raw  = await reader.ReadToEndAsync(ct);
        var doc  = JsonDocument.Parse(raw);
        var text = doc.RootElement
            .GetProperty("content")[0]
            .GetProperty("text")
            .GetString() ?? "";

        return ParseResponse(text);
    }

    // ── Prompt ────────────────────────────────────────────────────────────────
    private static string BuildPrompt(AIAnalysisInput input)
    {
        var sb = new StringBuilder();

        sb.AppendLine("Sen bir futbol scouting platformu için analiz kalite değerlendirme asistanısın.");
        sb.AppendLine("Görevin: Verilen scout analizini iki ana kritere göre değerlendirmek.");
        sb.AppendLine();

        sb.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        sb.AppendLine("OYUNCU BİLGİSİ");
        sb.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        sb.AppendLine($"Ad: {input.PlayerName}");
        sb.AppendLine($"Mevki: {input.PlayerPosition}");
        sb.AppendLine();

        sb.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        sb.AppendLine("DEĞERLENDİRİLECEK ANALİZ");
        sb.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

        sb.AppendLine("[Genel Değerlendirme]");
        sb.AppendLine(input.GeneralContent);

        if (!string.IsNullOrWhiteSpace(input.TechnicalContent))
        {
            sb.AppendLine(); sb.AppendLine("[Teknik Beceriler]");
            sb.AppendLine(input.TechnicalContent);
        }
        if (!string.IsNullOrWhiteSpace(input.TacticalContent))
        {
            sb.AppendLine(); sb.AppendLine("[Taktiksel Katkı]");
            sb.AppendLine(input.TacticalContent);
        }
        if (!string.IsNullOrWhiteSpace(input.PhysicalContent))
        {
            sb.AppendLine(); sb.AppendLine("[Fiziksel Özellikler]");
            sb.AppendLine(input.PhysicalContent);
        }
        if (!string.IsNullOrWhiteSpace(input.StrengthsContent))
        {
            sb.AppendLine(); sb.AppendLine("[Güçlü Yönler]");
            sb.AppendLine(input.StrengthsContent);
        }
        if (!string.IsNullOrWhiteSpace(input.WeaknessesContent))
        {
            sb.AppendLine(); sb.AppendLine("[Geliştirmesi Gerekenler]");
            sb.AppendLine(input.WeaknessesContent);
        }
        sb.AppendLine();

        sb.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        sb.AppendLine("MEVCUT ANALİZLER (Özgünlük Karşılaştırması)");
        sb.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        if (input.ExistingAnalysesContent.Count == 0)
        {
            sb.AppendLine("Bu oyuncu için henüz onaylı analiz bulunmamaktadır.");
        }
        else
        {
            for (var i = 0; i < input.ExistingAnalysesContent.Count; i++)
            {
                sb.AppendLine($"--- Analiz {i + 1} ---");
                sb.AppendLine(input.ExistingAnalysesContent[i]);
            }
        }
        sb.AppendLine();

        sb.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        sb.AppendLine("PUANLAMA KRİTERLERİ (toplam 10 puan)");
        sb.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        sb.AppendLine();
        sb.AppendLine("1. ÖZGÜNLÜK (0-5 puan)");
        sb.AppendLine("Mevcut analizlerle karşılaştır. Scout kendi gözlemini mi aktarıyor?");
        sb.AppendLine("  5 → Tamamen özgün; mevcut analizlerde bulunmayan spesifik gözlemler, kişisel değerlendirme");
        sb.AppendLine("  4 → Büyük ölçüde özgün; bakış açısı farklı, küçük örtüşmeler var");
        sb.AppendLine("  3 → Özgün ama bazı kısımlar mevcut analizlerle benzeşiyor");
        sb.AppendLine("  2 → İçeriğin büyük kısmı mevcut analizlerle örtüşüyor");
        sb.AppendLine("  1 → Neredeyse kopya veya tamamen genel klişelerden oluşuyor");
        sb.AppendLine("  0 → Birebir kopya ya da anlamsız/alakasız içerik");
        sb.AppendLine();
        sb.AppendLine("2. DERİNLİK VE DETAY (0-5 puan)");
        sb.AppendLine("Tahmini okuma süresi ve içerik yoğunluğunu değerlendir.");
        sb.AppendLine("  5 → Çok detaylı; spesifik hareketler, pozisyonlar, durum analizleri, somut örnekler. Tahmini okuma: 4+ dakika");
        sb.AppendLine("  4 → Detaylı; somut gözlemler, birden fazla bölüm dolu. Tahmini okuma: 3-4 dakika");
        sb.AppendLine("  3 → Orta detaylı; bazı somut noktalara değiniliyor. Tahmini okuma: 2-3 dakika");
        sb.AppendLine("  2 → Yüzeysel; genel değerlendirme var ama spesifik gözlem yok. Tahmini okuma: 1-2 dakika");
        sb.AppendLine("  1 → Çok kısa ya da klişe ifadeler. Tahmini okuma: < 1 dakika");
        sb.AppendLine("  0 → Değerlendirme yapılamayacak kadar kısa veya anlamsız");
        sb.AppendLine();

        sb.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        sb.AppendLine("YANIT FORMATI (sadece geçerli JSON, markdown blok veya ek açıklama ekleme)");
        sb.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        sb.AppendLine("""
{
  "originality_score": <0-5 tam sayı>,
  "depth_score": <0-5 tam sayı>,
  "estimated_reading_minutes": <tahmini okuma süresi ondalıklı sayı>,
  "summary": "<Analizin 2-3 cümlelik Türkçe özeti. Scout'un öne çıkardığı en önemli gözlemleri vurgula.>",
  "is_duplicate": <true eğer mevcut bir analizle %60+ benzerlik varsa, aksi halde false>,
  "duplicate_warning": "<is_duplicate true ise hangi analiz ve neden benzediğini kısaca açıkla, aksi halde null>"
}
""");

        return sb.ToString();
    }

    // ── Response parser ───────────────────────────────────────────────────────
    private static AIEvaluationResult ParseResponse(string text)
    {
        try
        {
            // Strip potential markdown fences if model added them
            var json = text.Trim();
            if (json.StartsWith("```")) json = json[(json.IndexOf('\n') + 1)..];
            if (json.EndsWith("```"))  json = json[..json.LastIndexOf("```")];
            json = json.Trim();

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var originality = root.TryGetProperty("originality_score", out var o) ? o.GetDecimal() : 0m;
            var depth       = root.TryGetProperty("depth_score",       out var d) ? d.GetDecimal() : 0m;
            var reading     = root.TryGetProperty("estimated_reading_minutes", out var r) ? r.GetDecimal() : 0m;
            var summary     = root.TryGetProperty("summary",           out var s) ? s.GetString() : null;
            var isDup       = root.TryGetProperty("is_duplicate",      out var dup) && dup.GetBoolean();
            var dupWarn     = root.TryGetProperty("duplicate_warning", out var dw) ? dw.GetString() : null;

            return new AIEvaluationResult
            {
                IsAvailable             = true,
                OriginalityScore        = Math.Clamp(Math.Round(originality, 1), 0m, 5m),
                DepthScore              = Math.Clamp(Math.Round(depth,       1), 0m, 5m),
                EstimatedReadingMinutes = Math.Max(0m, Math.Round(reading,   1)),
                Summary                 = summary,
                IsPossibleDuplicate     = isDup,
                DuplicateWarning        = dupWarn
            };
        }
        catch
        {
            return new AIEvaluationResult { IsAvailable = false };
        }
    }
}
