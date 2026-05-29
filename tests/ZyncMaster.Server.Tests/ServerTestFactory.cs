using System;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ZyncMaster.Server.Data;

namespace ZyncMaster.Server.Tests;

// Test host that runs the real Program composition but swaps the SQL Server DbContext
// for SQLite kept in memory via a single open connection (so every context created by
// the factory or the scoped registration shares the same database for the life of the
// factory). The schema + the seeded "default" user come from EnsureCreated, which mirrors
// the production migration's HasData seed. Tests that need store doubles still chain
// .WithWebHostBuilder on top of this; their overrides win because they run last.
public sealed class ServerTestFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection;

    public ServerTestFactory()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Drop every EF Core registration that Program.cs made for SQL Server — the
            // context, the factory, the options and all provider-specific internal services
            // (registering a second provider alongside SqlServer throws at runtime).
            RemoveAllEntityFramework(services);

            // Re-register against the shared in-memory SQLite connection: a singleton
            // factory for the stores and a scoped context for the Data Protection key ring.
            services.AddDbContextFactory<ZyncMasterDbContext>(o => o.UseSqlite(_connection));
            services.AddDbContext<ZyncMasterDbContext>(
                o => o.UseSqlite(_connection),
                contextLifetime: ServiceLifetime.Scoped,
                optionsLifetime: ServiceLifetime.Singleton);

            using var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ZyncMasterDbContext>();
            db.Database.EnsureCreated();
        });
    }

    // Strips the DbContext, its factory and every provider-specific EF Core service so a
    // fresh single-provider (SQLite) registration can take over.
    private static void RemoveAllEntityFramework(IServiceCollection services)
    {
        var doomed = services.Where(s =>
            s.ServiceType == typeof(ZyncMasterDbContext) ||
            s.ServiceType == typeof(DbContextOptions<ZyncMasterDbContext>) ||
            s.ServiceType == typeof(DbContextOptions) ||
            s.ServiceType == typeof(IDbContextFactory<ZyncMasterDbContext>) ||
            (s.ServiceType.FullName?.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal) ?? false))
            .ToList();
        foreach (var d in doomed)
            services.Remove(d);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _connection.Dispose();
    }
}
