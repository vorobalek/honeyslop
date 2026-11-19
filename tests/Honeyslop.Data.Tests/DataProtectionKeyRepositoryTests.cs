using Honeyslop.Data.Entities;
using Honeyslop.Data.Repositories;
using Honeyslop.Tests;
using Microsoft.Extensions.DependencyInjection;

namespace Honeyslop.Data.Tests;

[TestClass]
[TestCategory("Integration")]
[DoNotParallelize]
public sealed class DataProtectionKeyRepositoryTests
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromDays(90);

    [TestMethod]
    public void TryAdd_StoresAKeyOnlyWhenNoStoredKeyIsValidAtItsActivation()
    {
        var start = AfterEveryStoredKey();
        var first = Key(start);
        var concurrent = Key(start.AddSeconds(1));
        var successor = Key(start + Lifetime);
        var revocation = new DataProtectionKey
        {
            FriendlyName = $"revocation-{Unique.String(8)}",
            Xml = "<revocation />"
        };

        var added = new[]
        {
            TryAdd(first), TryAdd(concurrent), TryAdd(successor), TryAdd(revocation)
        };

        CollectionAssert.AreEqual(new[]
        {
            true, false, true, true
        }, added);
        var stored = GetAll();
        CollectionAssert.AreEqual(
            new[]
            {
                first.FriendlyName, successor.FriendlyName, revocation.FriendlyName
            },
            stored.Where(x => x.Id >= first.Id).Select(x => x.FriendlyName).ToArray());
        CollectionAssert.AreEqual(stored.OrderBy(x => x.Id).ToArray(), stored.ToArray());
    }

    [TestMethod]
    public async Task TryAdd_LetsInOnlyOneOfKeysStoredAtTheSameTime()
    {
        var start = AfterEveryStoredKey();
        var keys = Enumerable.Range(0, 4).Select(i => Key(start.AddMilliseconds(i))).ToArray();
        var go = new TaskCompletionSource();

        var adding = keys.Select(key => Task.Run(async () =>
        {
            await go.Task;
            return TryAdd(key);
        })).ToArray();
        go.SetResult();
        var added = await Task.WhenAll(adding);

        Assert.AreEqual(1, added.Count(x => x));
        Assert.AreEqual(1, GetAll().Count(x => x.ActivationDate >= start));
    }

    private static DataProtectionKey Key(DateTimeOffset activation)
    {
        return new DataProtectionKey
        {
            FriendlyName = $"key-{Unique.String(8)}",
            Xml = "<key />",
            ActivationDate = activation,
            ExpirationDate = activation + Lifetime
        };
    }

    private static DateTimeOffset AfterEveryStoredKey()
    {
        return (GetAll().Max(x => x.ExpirationDate) ?? DateTimeOffset.UtcNow).AddDays(1);
    }

    private static bool TryAdd(DataProtectionKey key)
    {
        using var scope = PostgresDatabase.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IDataProtectionKeyRepository>().TryAdd(key);
    }

    private static IReadOnlyList<DataProtectionKey> GetAll()
    {
        using var scope = PostgresDatabase.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IDataProtectionKeyRepository>().GetAll();
    }
}