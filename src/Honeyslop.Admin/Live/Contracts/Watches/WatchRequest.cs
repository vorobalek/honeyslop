using System.Text.Json.Serialization;

namespace Honeyslop.Admin.Live.Contracts.Watches;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "view")]
[JsonDerivedType(typeof(EntriesWatch), "entries")]
[JsonDerivedType(typeof(EntryWatch), "entry")]
[JsonDerivedType(typeof(RequestsWatch), "requests")]
[JsonDerivedType(typeof(RequestWatch), "request")]
[JsonDerivedType(typeof(ClientsWatch), "clients")]
[JsonDerivedType(typeof(ClientWatch), "client")]
[JsonDerivedType(typeof(ClientAbuseWatch), "clientAbuse")]
[JsonDerivedType(typeof(ReportsWatch), "reports")]
[JsonDerivedType(typeof(ReportWatch), "report")]
[JsonDerivedType(typeof(GeneratorsWatch), "generators")]
[JsonDerivedType(typeof(SettingsWatch), "settings")]
public abstract record WatchRequest;