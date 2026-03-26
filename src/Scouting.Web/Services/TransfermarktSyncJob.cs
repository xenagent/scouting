using Microsoft.EntityFrameworkCore;
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

        var player = await db.Players
            .FirstOrDefaultAsync(p => p.TransfermarktId == tmId, ct);

        if (player is null) return;

        var data = await tm.ScrapePlayerAsync(tmId, ct);
        if (data is null) return;

        player.UpdateFromTransfermarkt(data);
        await db.SaveChangesAsync(ct);

        _logger.LogInformation("Anlık TM sync tamamlandı — {Name} (tmId={TmId})", player.Name, tmId);
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
