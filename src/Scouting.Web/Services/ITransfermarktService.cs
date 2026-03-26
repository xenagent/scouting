namespace Scouting.Web.Services;

public class PlayerSeasonStats
{
    public string Season { get; init; } = "";
    public int Matches { get; init; }
    public int Goals { get; init; }
    public int Assists { get; init; }
}

public class TransfermarktPlayerData
{
    public string TmId { get; init; } = "";
    public string? Name { get; init; }
    public int? Age { get; init; }
    public string? Team { get; init; }
    public string? League { get; init; }
    public decimal? MarketValueMillions { get; init; }   // EUR milyon cinsinden
    public List<PlayerSeasonStats> SeasonStats { get; init; } = [];
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
