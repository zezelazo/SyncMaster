using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ZyncMaster.Server.Data;
using Xunit;

namespace ZyncMaster.Server.Tests.Storage;

// Proves DPAPI-style protected values (e.g. refresh tokens) survive a server restart:
// one DataProtectionProvider instance persists its key ring to the SQLite-backed
// DbContext, and a SECOND, independently-built provider over the SAME database and SAME
// application name can unprotect a value the first one protected.
public class DataProtectionRoundTripTests
{
    private static ServiceProvider BuildProvider(SqliteConnection connection)
    {
        var services = new ServiceCollection();
        services.AddDbContext<ZyncMasterDbContext>(o => o.UseSqlite(connection));
        services.AddDataProtection()
            .PersistKeysToDbContext<ZyncMasterDbContext>()
            .SetApplicationName("ZyncMaster");
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task Second_provider_instance_unprotects_value_protected_by_first()
    {
        // One shared in-memory database for both "instances".
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        await using (var sp = BuildProvider(connection))
        {
            using var scope = sp.CreateScope();
            await scope.ServiceProvider.GetRequiredService<ZyncMasterDbContext>().Database.EnsureCreatedAsync();
        }

        string protectedValue;
        // First "instance": protect a value, persisting the generated key to the DB.
        await using (var sp1 = BuildProvider(connection))
        {
            var protector1 = sp1.GetRequiredService<IDataProtectionProvider>()
                .CreateProtector("ZyncMaster.RefreshToken");
            protectedValue = protector1.Protect("super-secret-refresh-token");
        }

        // Second, independently-built "instance" (simulating a restart) over the same DB.
        await using var sp2 = BuildProvider(connection);
        var protector2 = sp2.GetRequiredService<IDataProtectionProvider>()
            .CreateProtector("ZyncMaster.RefreshToken");

        protector2.Unprotect(protectedValue).Should().Be("super-secret-refresh-token");

        // And the key really lives in the DbContext-backed key ring.
        using var verifyScope = sp2.CreateScope();
        var db = verifyScope.ServiceProvider.GetRequiredService<ZyncMasterDbContext>();
        db.DataProtectionKeys.Should().NotBeEmpty();
    }
}
