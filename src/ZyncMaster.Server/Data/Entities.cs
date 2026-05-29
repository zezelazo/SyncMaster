namespace ZyncMaster.Server.Data;

// EF row types. Kept as plain mutable POCOs (not the domain records) so EF change
// tracking works cleanly and the mapping to/from the domain types lives in the stores.

public sealed class UserRow
{
    public string Id { get; set; } = "";
    public string Provider { get; set; } = "";
    public string Subject { get; set; } = "";
    public string? Email { get; set; }
    public string? DisplayName { get; set; }
    public DateTimeOffset CreatedUtc { get; set; }
}

public sealed class ConnectedAccountRow
{
    public string Id { get; set; } = "";
    public string UserId { get; set; } = "";
    public string Provider { get; set; } = "";
    public string AccountRef { get; set; } = "";
    public string? DisplayName { get; set; }
    public string EncryptedRefreshToken { get; set; } = "";
    public DateTimeOffset ConnectedUtc { get; set; }
    public bool IsDefault { get; set; }
}

public sealed class DeviceRow
{
    public string Id { get; set; } = "";
    public string UserId { get; set; } = "";
    public string Name { get; set; } = "";
    public string ApiKeyHash { get; set; } = "";
    public string? TargetCalendarId { get; set; }
    public DateTimeOffset CreatedUtc { get; set; }
    public DateTimeOffset? LastSeenUtc { get; set; }
}

public sealed class PendingPairingRow
{
    public string PairingId { get; set; } = "";
    public string? UserId { get; set; }
    public string DeviceName { get; set; } = "";
    public string Code { get; set; } = "";
    public bool Approved { get; set; }
    public string? ApprovedDeviceId { get; set; }
    public string? OneTimeApiKey { get; set; }
    public DateTimeOffset CreatedUtc { get; set; }
}

public sealed class SyncPairRow
{
    public string Id { get; set; } = "";
    public string UserId { get; set; } = "";
    public string Name { get; set; } = "";
    public string SourceJson { get; set; } = "";
    public string DestinationJson { get; set; } = "";
    public int IntervalMin { get; set; }
    public string State { get; set; } = "active";
    public DateTimeOffset? LastRunUtc { get; set; }
    public string? LastResultJson { get; set; }
}

public sealed class SyncStateRow
{
    // Composite-free surrogate key (UserId|DeviceId) keeps the row per-user-per-device
    // unique while DeviceId carries its own unique index for the by-device lookups.
    public string Id { get; set; } = "";
    public string DeviceId { get; set; } = "";
    public DateTimeOffset LastSyncUtc { get; set; }
    public int LastCreated { get; set; }
    public int LastUpdated { get; set; }
    public int LastDeleted { get; set; }
}
