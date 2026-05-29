namespace ZyncMaster.Server;

// Resolves the identity of the caller for the current operation. WS-A ships a fixed
// "default" stub so the suite keeps its single-user behavior; WS-B replaces the stub
// with cookie / api-key resolution and every store query already filters by UserId.
public interface ICurrentUserAccessor
{
    string UserId { get; }
}

// Fixed single-user stub. Returns the seeded "default" user id so EF-backed stores
// behave identically to the previous in-memory single-user stores.
public sealed class DefaultCurrentUserAccessor : ICurrentUserAccessor
{
    public const string DefaultUserId = "default";

    public string UserId => DefaultUserId;
}
