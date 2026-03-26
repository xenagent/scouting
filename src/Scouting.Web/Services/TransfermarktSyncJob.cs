using Microsoft.EntityFrameworkCore;
using Scouting.Web.Infrastructure;

namespace Scouting.Web.Services;

/// <summary>
/// Her ay 1'inde Transfermarkt ID'si olan tüm oyuncuların
/// istatistik ve piyasa değerini günceller.
/// </summary>
public class TransfermarktSyncJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TransfermarktSyncJob> _logger;

    // Her gün uyanır, ama ayın 1'inde mi kontrol eder
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(24);

    public TransfermarktSyncJob(
        IServiceScopeFactory scopeFactory,
        ILogger<TransfermarktSyncJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Uygulama ilk açıldığında 30 saniye bekle (DB hazır olsun)
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        using var timer = new PeriodicTimer(CheckInterval);

        // İlk çalışmada da kontrol et
        await TrySyncIfDueAsync(stoppingToken);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await TrySyncIfDueAsync(stoppingToken);
        }
    }

    private async Task TrySyncIfDueAsync(CancellationToken ct)
    {
        // Ayın 1'i değilse geç
        if (DateTime.UtcNow.Day != 1) return;

        _logger.LogInformation("Transfermarkt aylık sync başlıyor — {Date:yyyy-MM-dd}", DateTime.UtcNow);

        try
        {
            await RunSyncAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Transfermarkt sync sırasında beklenmeyen hata");
        }
    }

    private async Task RunSyncAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var tm = scope.ServiceProvider.GetRequiredService<ITransfermarktService>();

        // Transfermarkt ID'si olan ve bu ay henüz sync edilmemiş oyuncular
        var today = DateTime.UtcNow;
        var players = await db.Players
            .Where(p => p.TransfermarktId != null &&
                        (p.LastTransfermarktSync == null ||
                         p.LastTransfermarktSync.Value.Year < today.Year ||
                         p.LastTransfermarktSync.Value.Month < today.Month))
            .ToListAsync(ct);

        _logger.LogInformation("Sync edilecek oyuncu sayısı: {Count}", players.Count);

        var successCount = 0;
        foreach (var player in players)
        {
            if (ct.IsCancellationRequested) break;

            var data = await tm.ScrapePlayerAsync(player.TransfermarktId!, ct);
            if (data is null)
            {
                _logger.LogWarning("Oyuncu verisi alınamadı — tmId={TmId} name={Name}",
                    player.TransfermarktId, player.Name);
                continue;
            }

            player.UpdateFromTransfermarkt(data);
            successCount++;

            // TM'ye çok hızlı istek atmamak için kısa bekleme
            await Task.Delay(TimeSpan.FromSeconds(2), ct);
        }

        await db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Transfermarkt sync tamamlandı — {Success}/{Total} oyuncu güncellendi",
            successCount, players.Count);
    }
}
