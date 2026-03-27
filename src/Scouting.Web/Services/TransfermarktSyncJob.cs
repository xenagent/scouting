using Microsoft.EntityFrameworkCore;
using Scouting.Web.Domains.AnalysisEntity;
using Scouting.Web.Infrastructure;

namespace Scouting.Web.Services;

/// <summary>
/// İki sorumluluğu var:
/// 1. Anlık: TmSyncQueue'yu dinler — analiz girildiğinde ilgili oyuncunun TM verisi hemen güncellenir.
/// 2. Periyodik: Her 3 ayda bir TM ID'si olan tüm oyuncuları otomatik günceller.
/// </summary>
public class TransfermarktSyncJob : BackgroundService
{
    private static readonly TimeSpan PeriodicInterval = TimeSpan.FromDays(90);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TmSyncQueue _queue;
    private readonly ILogger<TransfermarktSyncJob> _logger;

    public TransfermarktSyncJob(
        IServiceScopeFactory scopeFactory,
        TmSyncQueue queue,
        ILogger<TransfermarktSyncJob> logger)
    {
        _scopeFactory = scopeFactory;
        _queue = queue;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Uygulama açılışında 30 sn bekle
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        // Anlık kuyruk tüketici + periyodik kontrol paralel çalışır
        var queueTask      = ConsumeQueueAsync(stoppingToken);
        var periodicTask   = RunPeriodicSyncAsync(stoppingToken);

        await Task.WhenAll(queueTask, periodicTask);
    }

    // ── 1. Anlık kuyruk tüketici ─────────────────────────────────────────────

    private async Task ConsumeQueueAsync(CancellationToken ct)
    {
        await foreach (var tmId in _queue.Reader.ReadAllAsync(ct))
        {
            try
            {
                await SyncSinglePlayerAsync(tmId, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Anlık TM sync başarısız — tmId={TmId}", tmId);
            }
        }
    }

    private async Task SyncSinglePlayerAsync(string tmId, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var tm = scope.ServiceProvider.GetRequiredService<ITransfermarktService>();
        var fileService = scope.ServiceProvider.GetRequiredService<IFileService>();
        var httpFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();

        var player = await db.Players
            .FirstOrDefaultAsync(p => p.TransfermarktId == tmId, ct);

        if (player is null) return;

        var data = await tm.ScrapePlayerAsync(tmId, ct);
        if (data is null) return;

        // Önceki değeri kaydet — bonus kontrolü için
        var previousMarketValue = player.MarketValue;

        player.UpdateFromTransfermarkt(data);

        // Market value değişimini analiz et ve ilgili scout'lara bonus/ceza ver
        await CheckAndAwardMarketBonusesAsync(db, player, previousMarketValue, ct);

        // Download player image locally if TM returned one
        if (!string.IsNullOrEmpty(data.ImageUrl))
        {
            try
            {
                using var http = httpFactory.CreateClient("TmImage");
                using var response = await http.GetAsync(data.ImageUrl, HttpCompletionOption.ResponseHeadersRead, ct);
                if (response.IsSuccessStatusCode)
                {
                    var ext = Path.GetExtension(new Uri(data.ImageUrl).AbsolutePath);
                    if (string.IsNullOrEmpty(ext)) ext = ".jpg";
                    await using var stream = await response.Content.ReadAsStreamAsync(ct);
                    var saveResult = await fileService.SavePlayerImageFromStreamAsync(stream, tmId, ext, ct);
                    if (saveResult.IsSuccess)
                        player.SetImageUrl(saveResult.Data!);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "TM görsel indirilemedi — tmId={TmId}", tmId);
            }
        }

        await db.SaveChangesAsync(ct);

        _logger.LogInformation("Anlık TM sync tamamlandı — {Name} (tmId={TmId})", player.Name, tmId);
    }

    // ── Market value bonus / ceza kontrolü ───────────────────────────────────

    /// <summary>
    /// Piyasa değeri önceki baz noktasına göre ≥%10 değiştiyse:
    ///   • Artış  → scout'a +25 × keşif katsayısı puan
    ///   • Düşüş  → scout'a −25 puan
    /// Bonus ödendikten sonra baz değer yeni değere taşınır (tekrar ödeme olmaz).
    /// </summary>
    private async Task CheckAndAwardMarketBonusesAsync(
        AppDbContext db, Domains.PlayerEntity.Player player,
        decimal? previousMarketValue, CancellationToken ct)
    {
        var currentMarketValue = player.MarketValue;
        if (!currentMarketValue.HasValue) return;

        // Bu oyuncuya ait, baseline değeri kayıtlı onaylı analizleri getir
        var analyses = await db.Analyses
            .Where(a => a.PlayerId == player.Id &&
                        a.Status == AnalysisStatus.Approved &&
                        a.ApprovalBaselineMarketValue.HasValue)
            .ToListAsync(ct);

        if (analyses.Count == 0) return;

        foreach (var analysis in analyses)
        {
            var baseline = analysis.ApprovalBaselineMarketValue!.Value;
            if (baseline <= 0) continue;

            var changeRatio = (currentMarketValue.Value - baseline) / baseline;

            // < %10 değişim — anlamlı değil, atla
            if (Math.Abs(changeRatio) < 0.10m) continue;

            var scout = await db.Users
                .FirstOrDefaultAsync(u => u.Id == analysis.CreatedUserId, ct);
            if (scout is null) continue;

            if (changeRatio >= 0.10m)
            {
                // Artış: keşif katsayısıyla ölçeklenmiş bonus
                var bonus = (int)Math.Round(25 * analysis.DiscoveryMultiplier);
                scout.AddBonusPoints(bonus);
                _logger.LogInformation(
                    "Piyasa değeri bonusu: +{Bonus}p → @{Scout} | {Player} | baz:{Baseline}m → şimdi:{Current}m | katsayı:{Mult}",
                    bonus, scout.Username, player.Name,
                    baseline.ToString("0.##"), currentMarketValue.Value.ToString("0.##"),
                    analysis.DiscoveryMultiplier);
            }
            else
            {
                // Düşüş: sabit ceza
                scout.AddBonusPoints(-25);
                _logger.LogInformation(
                    "Piyasa değeri cezası: -25p → @{Scout} | {Player} | baz:{Baseline}m → şimdi:{Current}m",
                    scout.Username, player.Name,
                    baseline.ToString("0.##"), currentMarketValue.Value.ToString("0.##"));
            }

            // Baz değeri güncelle — bir sonraki sync bu yeni değeri referans alır
            analysis.UpdateApprovalBaseline(currentMarketValue.Value);
        }
    }

    // ── 2. Periyodik 3-aylık sync ────────────────────────────────────────────

    private async Task RunPeriodicSyncAsync(CancellationToken ct)
    {
        // Her 24 saatte bir kontrol et; 90 günü geçmiş oyuncuları güncelle
        using var timer = new PeriodicTimer(TimeSpan.FromHours(24));

        while (await timer.WaitForNextTickAsync(ct))
        {
            var cutoff = DateTime.UtcNow.Subtract(PeriodicInterval);

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var due = await db.Players
                .AsNoTracking()
                .Where(p => p.TransfermarktId != null &&
                            (p.LastTransfermarktSync == null ||
                             p.LastTransfermarktSync < cutoff))
                .Select(p => p.TransfermarktId!)
                .ToListAsync(ct);

            if (due.Count == 0) continue;

            _logger.LogInformation("3-aylık TM sync — {Count} oyuncu güncelleniyor", due.Count);

            foreach (var tmId in due)
            {
                if (ct.IsCancellationRequested) break;

                try
                {
                    await SyncSinglePlayerAsync(tmId, ct);
                    // TM rate limit için kısa bekleme
                    await Task.Delay(TimeSpan.FromSeconds(2), ct);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Periyodik TM sync başarısız — tmId={TmId}", tmId);
                }
            }

            _logger.LogInformation("3-aylık TM sync tamamlandı");
        }
    }
}
