using System.Globalization;
using System.Security.Cryptography;
using Honeyslop.Data.Entities;
using Honeyslop.Data.Enums;
using Honeyslop.Data.Repositories;
using Honeyslop.Tests;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Honeyslop.Data.Tests;

[TestClass]
[TestCategory("Integration")]
public sealed class NotificationTests
{
    [TestMethod]
    public async Task EntryChanges_AreAnnouncedWithTheirId()
    {
        await using var connection = new NpgsqlConnection(PostgresDatabase.RoleConnectionString);
        await connection.OpenAsync();
        var received = new List<string>();
        connection.Notification += (_, notification) => received.Add(notification.Payload);
        await using (var command = new NpgsqlCommand("listen honeyslop_entries", connection))
        {
            await command.ExecuteNonQueryAsync();
        }

        var now = DateTimeOffset.UtcNow;
        var entry = new Entry
        {
            KeyHash = RandomNumberGenerator.GetBytes(32),
            Method = "GET",
            Host = Unique.Host(),
            Target = Unique.Target(),
            Status = EntryStatus.Pending,
            CreatedAt = now,
            UpdatedAt = now
        };
        await using (var scope = PostgresDatabase.CreateScope())
        {
            Assert.IsTrue(await scope.Repository().TryAddAsync(entry, CancellationToken.None));
        }

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        while (!received.Contains(entry.Id.ToString(CultureInfo.InvariantCulture)))
            await connection.WaitAsync(timeout.Token);
    }

    [TestMethod]
    public async Task SettingChanges_AreAnnounced()
    {
        await using var connection = new NpgsqlConnection(PostgresDatabase.RoleConnectionString);
        await connection.OpenAsync();
        var received = new List<string>();
        connection.Notification += (_, notification) => received.Add(notification.Channel);
        await using (var command = new NpgsqlCommand("listen honeyslop_settings", connection))
        {
            await command.ExecuteNonQueryAsync();
        }

        await using (var scope = PostgresDatabase.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            dbContext.Settings.Add(new Setting
            {
                Key = $"test_{Unique.String(8)}",
                Value = "on",
                UpdatedAt = DateTimeOffset.UtcNow
            });
            await dbContext.SaveChangesAsync();
        }

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        while (!received.Contains("honeyslop_settings"))
            await connection.WaitAsync(timeout.Token);
    }

    [TestMethod]
    public async Task BlockedContactChanges_AreAnnouncedAsReportChanges()
    {
        await using var connection = new NpgsqlConnection(PostgresDatabase.RoleConnectionString);
        await connection.OpenAsync();
        var received = new List<string>();
        connection.Notification += (_, notification) => received.Add(notification.Channel);
        await using (var command = new NpgsqlCommand("listen honeyslop_reports", connection))
        {
            await command.ExecuteNonQueryAsync();
        }

        await using (var scope = PostgresDatabase.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<IBlockedAbuseContactRepository>().AddAsync(
                [
                    new BlockedAbuseContact
                    {
                        Contact = $"@{Unique.String(8)}.example",
                        CreatedAt = DateTimeOffset.UtcNow
                    }
                ],
                CancellationToken.None);
        }

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        while (!received.Contains("honeyslop_reports"))
            await connection.WaitAsync(timeout.Token);
    }

    [TestMethod]
    public async Task Bounces_AreAnnouncedAsReportChanges()
    {
        await using var connection = new NpgsqlConnection(PostgresDatabase.RoleConnectionString);
        await connection.OpenAsync();
        var received = new List<string>();
        connection.Notification += (_, notification) => received.Add(notification.Channel);
        await using (var command = new NpgsqlCommand("listen honeyslop_reports", connection))
        {
            await command.ExecuteNonQueryAsync();
        }

        await using (var scope = PostgresDatabase.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<IAbuseReportBounceRepository>().AddAsync(
                [
                    new AbuseReportBounce
                    {
                        ReportId = 1,
                        Recipient = $"abuse@{Unique.String(8)}.example",
                        Action = BounceAction.Failed,
                        NotificationId = Unique.String(12),
                        BouncedAt = DateTimeOffset.UtcNow,
                        CreatedAt = DateTimeOffset.UtcNow
                    }
                ],
                CancellationToken.None);
        }

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        while (!received.Contains("honeyslop_reports"))
            await connection.WaitAsync(timeout.Token);
    }

    [TestMethod]
    public async Task ClientDelayChanges_AreAnnounced()
    {
        await using var connection = new NpgsqlConnection(PostgresDatabase.RoleConnectionString);
        await connection.OpenAsync();
        var received = new List<string>();
        connection.Notification += (_, notification) => received.Add(notification.Channel);
        await using (var command = new NpgsqlCommand("listen honeyslop_clients", connection))
        {
            await command.ExecuteNonQueryAsync();
        }

        await using (var scope = PostgresDatabase.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<IClientDelayRepository>()
                .SetAsync($"203.0.113.{Random.Shared.Next(1, 254)}", 250, "operator", DateTimeOffset.UtcNow, CancellationToken.None);
        }

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        while (!received.Contains("honeyslop_clients"))
            await connection.WaitAsync(timeout.Token);
    }

    [TestMethod]
    public async Task LearnedDelayChanges_AreAnnouncedAsClientChanges()
    {
        await using var connection = new NpgsqlConnection(PostgresDatabase.RoleConnectionString);
        await connection.OpenAsync();
        var received = new List<string>();
        connection.Notification += (_, notification) => received.Add(notification.Channel);
        await using (var command = new NpgsqlCommand("listen honeyslop_clients", connection))
        {
            await command.ExecuteNonQueryAsync();
        }

        await using (var scope = PostgresDatabase.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<IClientProbeRepository>().TryAddAsync(
                new ClientProbe
                {
                    ClientIp = $"203.0.113.{Random.Shared.Next(1, 254)}-{Unique.String(6)}",
                    DelayMilliseconds = 15_000,
                    ProbeEvery = 20,
                    UpdatedAt = DateTimeOffset.UtcNow
                },
                CancellationToken.None);
        }

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        while (!received.Contains("honeyslop_clients"))
            await connection.WaitAsync(timeout.Token);
    }

    [TestMethod]
    public async Task GenerationOutput_IsAnnouncedWithItsContent()
    {
        await using var connection = new NpgsqlConnection(PostgresDatabase.RoleConnectionString);
        await connection.OpenAsync();
        var received = new List<string>();
        connection.Notification += (_, notification) => received.Add(notification.Payload);
        await using (var command = new NpgsqlCommand("listen honeyslop_outputs", connection))
        {
            await command.ExecuteNonQueryAsync();
        }

        var output = $"{{\"entryId\":{Random.Shared.NextInt64(1, long.MaxValue)},\"tokens\":2}}";
        await using var scope = PostgresDatabase.CreateScope();
        await scope.Repository().NotifyOutputAsync(output, CancellationToken.None);

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        while (!received.Contains(output))
            await connection.WaitAsync(timeout.Token);
    }
}