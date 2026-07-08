using Sales.Application.DTOs;

namespace Sales.Application.Services.Interfaces;

/// <summary>
/// Ingests sales a POS device completed while offline. Each order is created + settled through
/// the normal checkout pipeline and deduped on <c>OfflineOrderNumber</c> so replays are idempotent.
/// </summary>
public interface IPosOfflineSyncService
{
    Task<OfflineSyncResultDto> SyncAsync(OfflineSyncRequestDto request);
}
