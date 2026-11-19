using Honeyslop.Admin.Access;
using Honeyslop.Data.Entities;
using Honeyslop.Data.Repositories;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;

namespace Honeyslop.Admin.Tests.Access;

[TestClass]
public sealed class SharedDataProtectionTests
{
    [TestMethod]
    public void AnotherInstance_UnprotectsWhatTheFirstProtected()
    {
        var table = new KeyTable();
        using var first = Instance(table);
        var cookie = Protect(first, "signed in");
        using var second = Instance(table);

        Assert.AreEqual("signed in", Unprotect(second, cookie));
        Assert.HasCount(1, table.Keys);
    }

    [TestMethod]
    public void InstancesStartingTogether_SettleOnOneKey()
    {
        var table = new KeyTable();
        using var first = Instance(table);
        var fromFirst = Protect(first, "first");
        using var second = Instance(table);
        table.MissNextRead();
        var fromSecond = Protect(second, "second");

        Assert.HasCount(1, table.Keys);
        Assert.AreEqual("second", Unprotect(first, fromSecond));
        Assert.AreEqual("first", Unprotect(second, fromFirst));
    }

    private static ServiceProvider Instance(IDataProtectionKeyRepository repository)
    {
        return new ServiceCollection()
            .AddLogging()
            .AddScoped(_ => repository)
            .AddSharedDataProtection()
            .BuildServiceProvider();
    }

    private static string Protect(ServiceProvider instance, string text)
    {
        return instance.GetRequiredService<IDataProtectionProvider>().CreateProtector("cookie").Protect(text);
    }

    private static string Unprotect(ServiceProvider instance, string text)
    {
        return instance.GetRequiredService<IDataProtectionProvider>().CreateProtector("cookie").Unprotect(text);
    }

    private sealed class KeyTable : IDataProtectionKeyRepository
    {
        private bool _missNextRead;

        public List<DataProtectionKey> Keys { get; } = [];

        public void MissNextRead()
        {
            _missNextRead = true;
        }

        public IReadOnlyList<DataProtectionKey> GetAll()
        {
            if (!_missNextRead)
                return [..Keys];
            _missNextRead = false;
            return [];
        }

        public bool TryAdd(DataProtectionKey key)
        {
            if (key.ActivationDate is { } activation && Keys.Any(x => x.ExpirationDate > activation))
                return false;
            Keys.Add(key);
            return true;
        }
    }
}
