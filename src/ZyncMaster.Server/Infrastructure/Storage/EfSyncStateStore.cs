using Microsoft.EntityFrameworkCore;
using ZyncMaster.Server.Data;

namespace ZyncMaster.Server;

// EF-backed last-sync-state store keyed per (user, device). The surrogate row id is
// "userId|deviceId" so a device can only carry one state row per user; lookups go
// through the DeviceId index scoped to the current user.
public sealed class EfSyncStateStore : ISyncStateStore
{
    private readonly IDbContextFactory<ZyncMasterDbContext> _factory;
    private readonly ICurrentUserAccessor _currentUser;

    public EfSyncStateStore(IDbContextFactory<ZyncMasterDbContext> factory, ICurrentUserAccessor currentUser)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
    }

    public async Task SetAsync(SyncState state, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        await using var db = await _factory.CreateDbContextAsync(ct);
        var id = RowId(state.DeviceId);
        var row = await db.SyncStates.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (row is null)
        {
            row = new SyncStateRow { Id = id, DeviceId = state.DeviceId };
            db.SyncStates.Add(row);
        }
        row.LastSyncUtc = state.LastSyncUtc;
        row.LastCreated = state.LastCreated;
        row.LastUpdated = state.LastUpdated;
        row.LastDeleted = state.LastDeleted;
        await db.SaveChangesAsync(ct);
    }

    public async Task<SyncState?> GetAsync(string deviceId, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(deviceId);
        await using var db = await _factory.CreateDbContextAsync(ct);
        var id = RowId(deviceId);
        var row = await db.SyncStates.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, ct);
        if (row is null)
            return null;
        return new SyncState
        {
            DeviceId = row.DeviceId,
            LastSyncUtc = row.LastSyncUtc,
            LastCreated = row.LastCreated,
            LastUpdated = row.LastUpdated,
            LastDeleted = row.LastDeleted,
        };
    }

    private string RowId(string deviceId) => $"{_currentUser.UserId}|{deviceId}";
}
