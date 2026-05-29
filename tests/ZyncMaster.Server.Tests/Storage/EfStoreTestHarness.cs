using System;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ZyncMaster.Server.Data;

namespace ZyncMaster.Server.Tests.Storage;

// Spins up a SQLite-in-memory DbContext factory (shared open connection) with the schema
// created and the "default" user seeded, plus a fixed current-user accessor — the same
// single-user setup the live server uses through the stub. Disposed by each test.
internal sealed class EfStoreTestHarness : IDisposable
{
    private readonly SqliteConnection _connection;

    public IDbContextFactory<ZyncMasterDbContext> Factory { get; }
    public ICurrentUserAccessor CurrentUser { get; } = new DefaultCurrentUserAccessor();

    public EfStoreTestHarness()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ZyncMasterDbContext>()
            .UseSqlite(_connection)
            .Options;

        Factory = new PooledFactory(options);

        using var db = Factory.CreateDbContext();
        db.Database.EnsureCreated();
    }

    public ZyncMasterDbContext NewContext() => Factory.CreateDbContext();

    public void Dispose() => _connection.Dispose();

    // Minimal IDbContextFactory that hands out contexts over the same options/connection.
    private sealed class PooledFactory : IDbContextFactory<ZyncMasterDbContext>
    {
        private readonly DbContextOptions<ZyncMasterDbContext> _options;
        public PooledFactory(DbContextOptions<ZyncMasterDbContext> options) => _options = options;
        public ZyncMasterDbContext CreateDbContext() => new(_options);
    }
}
