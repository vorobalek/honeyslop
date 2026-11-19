using System.Globalization;
using System.Xml.Linq;
using Honeyslop.Data.Entities;
using Honeyslop.Data.Repositories;
using Microsoft.AspNetCore.DataProtection.Repositories;

namespace Honeyslop.Admin.Access.Internals;

internal sealed partial class DataProtectionKeyStore(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<DataProtectionKeyStore> logger) : IXmlRepository
{
    public IReadOnlyCollection<XElement> GetAllElements()
    {
        using var scope = serviceScopeFactory.CreateScope();
        return
        [
            ..scope
                .ServiceProvider
                .GetRequiredService<IDataProtectionKeyRepository>()
                .GetAll()
                .Select(x => XElement.Parse(x.Xml))
        ];
    }

    public void StoreElement(XElement element, string friendlyName)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var stored = scope
            .ServiceProvider
            .GetRequiredService<IDataProtectionKeyRepository>()
            .TryAdd(new DataProtectionKey
            {
                FriendlyName = friendlyName,
                Xml = element.ToString(SaveOptions.DisableFormatting),
                ActivationDate = DateOf(element, "activationDate"),
                ExpirationDate = DateOf(element, "expirationDate")
            });
        if (!stored)
            LogKeptStoredKey(logger, friendlyName);
    }

    private static DateTimeOffset? DateOf(XElement element, string name)
    {
        return element.Name.LocalName == "key" && element.Element(name) is { } date
            ? DateTimeOffset.Parse(date.Value, CultureInfo.InvariantCulture)
            : null;
    }

    [LoggerMessage(LogLevel.Information, "Another instance stored a key first, {FriendlyName} is dropped in its favour")]
    static partial void LogKeptStoredKey(ILogger<DataProtectionKeyStore> logger, string friendlyName);
}