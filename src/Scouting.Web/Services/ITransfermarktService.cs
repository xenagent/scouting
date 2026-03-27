namespace Scouting.Web.Services;

public class PlayerSeasonStats
{
    public string Season { get; init; } = "";
    public string? Competition { get; init; }   // Lig / kupa adı
    public int Matches { get; init; }
    public int MinutesPlayed { get; init; }     // Toplam oynanan dakika
    public int Goals { get; init; }
    public int Assists { get; init; }

    // Türetilmiş — saklanmaz, hesaplanır
    public decimal? GoalsPer90      => MinutesPlayed > 0 ? Math.Round((decimal)Goals   * 90 / MinutesPlayed, 2) : null;
    public decimal? AssistsPer90    => MinutesPlayed > 0 ? Math.Round((decimal)Assists * 90 / MinutesPlayed, 2) : null;
    public int?     MinutesPerGoal  => Goals > 0 ? MinutesPlayed / Goals : null;
}

/// <summary>
/// Maç bazında oyuncu notu (10 üzerinden).
/// Kaynak: SofaScore / WhoScored / FotMob — henüz entegre edilmedi,
/// alan yapısı hazır, servis katmanı sonradan bağlanacak.
/// </summary>
public class PlayerMatchRating
{
    public string Season { get; init; } = "";
    public string? Competition { get; init; }
    public DateOnly MatchDate { get; init; }
    public string? Opponent { get; init; }
    public bool IsHome { get; init; }
    public int MinutesPlayed { get; init; }
    public decimal Rating { get; init; }        // örn. 7.4 (10 üzerinden)
    public string Source { get; init; } = "";   // "sofascore" | "whoscored" | "fotmob"
}

public class TransfermarktPlayerData
{
    public string TmId { get; init; } = "";
    public string? Name { get; init; }
    public int? Age { get; init; }
    public string? Team { get; init; }
    public string? League { get; init; }
    public decimal? MarketValueMillions { get; init; }   // EUR milyon cinsinden
    public List<PlayerSeasonStats> SeasonStats { get; init; } = new();
    public decimal? MarketValue { get; set; }
    public string? ImageUrl { get; init; }
}

public interface ITransfermarktService
{
    /// <summary>
    /// Transfermarkt URL'inden oyuncu ID'sini çıkarır.
    /// Geçersiz URL ise null döner.
    /// Örnek: https://www.transfermarkt.com.tr/.../spieler/990148 → "990148"
    /// </summary>
    string? ExtractTmId(string url);

    /// <summary>
    /// scrape.do üzerinden TM leistungsdaten sayfasını çeker ve parse eder.
    /// Başarısız olursa null döner.
    /// </summary>
    Task<TransfermarktPlayerData?> ScrapePlayerAsync(string tmId, CancellationToken ct = default);
}
