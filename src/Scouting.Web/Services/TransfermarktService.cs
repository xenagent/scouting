using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Microsoft.Extensions.Options;

namespace Scouting.Web.Services;

public class TransfermarktOptions
{
    public string ScrapeDoToken { get; set; } = "";
    public string ScrapeDoBaseUrl { get; set; } = "http://api.scrape.do";
}

// ── XPath selector map ────────────────────────────────────────────────────────

file static class Selectors
{
    // leistungsdaten sayfası header bilgileri
    public const string Name = "//h1[contains(@class,'data-header__headline-wrapper')]";
    public const string Age = "//li[contains(.,'Yaş') or contains(.,'Age')]/span";
    public const string Team = "//span[contains(@class,'data-header__club')]/a";
    public const string MarketValue = "//a[contains(@class,'market-value')]";

    // Sezon istatistik tablosu — her satır bir sezonu temsil eder
    public const string TableRows = "//table[contains(@class,'items')]/tbody/tr";

    // Oyuncu profil fotoğrafı
    public const string PlayerImage = "//img[contains(@class,'data-header__profile-image')]";
}

// ── Service ───────────────────────────────────────────────────────────────────

public partial class TransfermarktService : ITransfermarktService
{
    private readonly HttpClient _http;
    private readonly TransfermarktOptions _opts;
    private readonly ILogger<TransfermarktService> _logger;

    // /spieler/{id} pattern — URL'nin herhangi bir yerinde olabilir
    [GeneratedRegex(@"/spieler/(\d+)(?:[/?#]|$)", RegexOptions.IgnoreCase)]
    private static partial Regex TmIdRegex();

    public TransfermarktService(
        HttpClient http,
        IOptions<TransfermarktOptions> opts,
        ILogger<TransfermarktService> logger)
    {
        _http = http;
        _opts = opts.Value;
        _logger = logger;
    }

    // ── ITransfermarktService ─────────────────────────────────────────────────

    public string? ExtractTmId(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;
        var m = TmIdRegex().Match(url);
        return m.Success ? m.Groups[1].Value : null;
    }

    public async Task<TransfermarktPlayerData?> ScrapePlayerAsync(
        string tmId, CancellationToken ct = default)
    {
        // leistungsdaten sayfası hem profil bilgilerini hem de istatistikleri içeriyor
        var targetUrl = Uri.EscapeDataString(
            $"https://www.transfermarkt.com.tr/x/leistungsdaten/spieler/{tmId}");

        var requestUrl = $"{_opts.ScrapeDoBaseUrl}/?url={targetUrl}&token={_opts.ScrapeDoToken}&output=raw";

        try
        {
            var html = await _http.GetStringAsync(requestUrl, ct);
            return Parse(html, tmId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "TM scrape başarısız — tmId={TmId}", tmId);
            return null;
        }
    }

    // ── Parser ────────────────────────────────────────────────────────────────

    private static TransfermarktPlayerData Parse(string html, string tmId)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        return new TransfermarktPlayerData
        {
            TmId = tmId,
            Name = GetText(doc, Selectors.Name),
            Age = ParseAge(GetText(doc, Selectors.Age)),
            Team = GetText(doc, Selectors.Team),
            MarketValue = ParseMarketValue(GetText(doc, Selectors.MarketValue)),
            SeasonStats = ParseSeasonStats(doc),
            ImageUrl = GetAttr(doc, Selectors.PlayerImage, "src")
        };
    }

    private static string? GetText(HtmlDocument doc, string xpath)
        => doc.DocumentNode.SelectSingleNode(xpath)?.InnerText?.Trim();

    private static string? GetAttr(HtmlDocument doc, string xpath, string attr)
        => doc.DocumentNode.SelectSingleNode(xpath)?.GetAttributeValue(attr, null);

    /// <summary>"26.05.2006 (19)" veya "19" → 19</summary>
    private static int? ParseAge(string? text)
    {
        if (string.IsNullOrEmpty(text)) return null;

        // "(19)" formatı
        var m = Regex.Match(text, @"\((\d+)\)");
        if (m.Success && int.TryParse(m.Groups[1].Value, out var a)) return a;

        // Sadece rakam
        if (int.TryParse(text.Trim(), out var b)) return b;
        return null;
    }

    /// <summary>
    /// "23,5 Mn. €", "€23.50m", "500 Bin €" → milyon cinsinden decimal.
    /// </summary>
    private static decimal? ParseMarketValue(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;

        // Boşlukları ve NBSP'yi temizle
        var t = text.Replace("\u00a0", " ").Trim();

        // Milyon: Mn. / Mio. / m
        var mM = Regex.Match(t, @"([\d.,]+)\s*(?:Mn\.|Mio\.|m)\s*[€$]?", RegexOptions.IgnoreCase);
        if (mM.Success && TryParseDecimal(mM.Groups[1].Value, out var mv)) return mv;

        // Bin / k (binler → milyona çevir)
        var mK = Regex.Match(t, @"([\d.,]+)\s*(?:Bin|k)\s*[€$]?", RegexOptions.IgnoreCase);
        if (mK.Success && TryParseDecimal(mK.Groups[1].Value, out var kv)) return Math.Round(kv / 1000m, 3);

        return null;
    }

    private static bool TryParseDecimal(string s, out decimal result)
        => decimal.TryParse(
            s.Replace(",", "."),
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture,
            out result);

    /// <summary>
    /// items tablosundaki her satırı sezon istatistiğine dönüştürür.
    /// Toplam/Total satırları atlanır.
    /// Hücre sırası: [0]=Sezon [1]=Kulüp [2]=Lig [3]=Mevki [4]=Maç [5]=Gol [6]=Asist ...
    /// </summary>
    private static List<PlayerSeasonStats> ParseSeasonStats(HtmlDocument doc)
    {
        var list = new List<PlayerSeasonStats>();
        var rows = doc.DocumentNode.SelectNodes(Selectors.TableRows);
        if (rows is null) return list;

        foreach (var row in rows)
        {
            var text = row.InnerText;
            if (text.Contains("Toplam", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("Total", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("Gesamt", StringComparison.OrdinalIgnoreCase))
                continue;

            var cells = row.SelectNodes("./td");
            if (cells is null || cells.Count < 7) continue;

            list.Add(new PlayerSeasonStats
            {
                Season = cells[0].InnerText.Trim(),
                Matches = ParseInt(cells[4].InnerText),
                Goals = ParseInt(cells[5].InnerText),
                Assists = ParseInt(cells[6].InnerText)
            });
        }

        return list;
    }

    private static int ParseInt(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return 0;
        var t = text.Trim();
        return t == "-" ? 0 : int.TryParse(t, out var v) ? v : 0;
    }
}
