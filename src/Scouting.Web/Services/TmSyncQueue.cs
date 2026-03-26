using System.Threading.Channels;

namespace Scouting.Web.Services;

/// <summary>
/// Anlık TM sync isteklerini tutan singleton kuyruk.
/// Analiz girildiğinde player'ın TM ID'si buraya eklenir,
/// TransfermarktSyncJob arka planda tüketir.
/// </summary>
public sealed class TmSyncQueue
{
    private readonly Channel<string> _channel =
        Channel.CreateBounded<string>(new BoundedChannelOptions(500)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false
        });

    public ChannelReader<string> Reader => _channel.Reader;

    /// <summary>
    /// Belirtilen TM ID'yi sync kuyruğuna ekler.
    /// Kuyruk doluysa en eski istek düşürülür.
    /// </summary>
    public void Enqueue(string tmId)
        => _channel.Writer.TryWrite(tmId);
}
