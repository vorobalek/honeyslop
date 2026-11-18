using System.Globalization;
using System.Security.Cryptography;
using Honeyslop.Data.Entities;
using Honeyslop.Data.Enums;
using Honeyslop.Tests;
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
            await command.ExecuteNonQueryAsync();

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
            Assert.IsTrue(await scope.Repository().TryAddAsync(entry, CancellationToken.None));

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        while (!received.Contains(entry.Id.ToString(CultureInfo.InvariantCulture)))
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
            await command.ExecuteNonQueryAsync();

        var output = $"{{\"entryId\":{Random.Shared.NextInt64(1, long.MaxValue)},\"tokens\":2}}";
        await using var scope = PostgresDatabase.CreateScope();
        await scope.Repository().NotifyOutputAsync(output, CancellationToken.None);

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        while (!received.Contains(output))
            await connection.WaitAsync(timeout.Token);
    }
}
