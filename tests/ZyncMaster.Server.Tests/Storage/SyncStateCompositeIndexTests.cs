using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ZyncMaster.Server.Tests.Storage;

// The WS-A review flagged that a unique index on DeviceId ALONE forbade the same physical
// device from holding a sync-state row under more than one user. The index is now the
// composite (UserId, DeviceId); these tests prove both halves of the new contract.
public class SyncStateCompositeIndexTests
{
    private sealed class MutableCurrentUser : ICurrentUserAccessor
    {
        public string UserId { get; set; } = DefaultCurrentUserAccessor.DefaultUserId;
    }

    [Fact]
    public async Task Same_device_can_hold_state_under_two_different_users()
    {
        using var h = new EfStoreTestHarness();
        var currentUser = new MutableCurrentUser();
        var store = new EfSyncStateStore(h.Factory, currentUser);

        currentUser.UserId = "user-a";
        await store.SetAsync(new SyncState { DeviceId = "dev-shared", LastCreated = 1 });

        currentUser.UserId = "user-b";
        await store.SetAsync(new SyncState { DeviceId = "dev-shared", LastCreated = 2 });

        // Two rows for the same DeviceId, one per user — would have thrown a unique-index
        // violation under the old DeviceId-only index.
        await using var db = h.NewContext();
        var rows = await db.SyncStates.Where(s => s.DeviceId == "dev-shared").ToListAsync();
        rows.Should().HaveCount(2);
        rows.Select(r => r.UserId).Should().BeEquivalentTo(new[] { "user-a", "user-b" });

        currentUser.UserId = "user-a";
        (await store.GetAsync("dev-shared"))!.LastCreated.Should().Be(1);
        currentUser.UserId = "user-b";
        (await store.GetAsync("dev-shared"))!.LastCreated.Should().Be(2);
    }

    [Fact]
    public async Task Composite_index_is_unique_per_user_device()
    {
        using var h = new EfStoreTestHarness();
        await using var db = h.NewContext();

        db.SyncStates.Add(new Data.SyncStateRow { Id = "x|dev", UserId = "x", DeviceId = "dev" });
        db.SyncStates.Add(new Data.SyncStateRow { Id = "x|dev-dup", UserId = "x", DeviceId = "dev" });

        var act = async () => await db.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }
}
