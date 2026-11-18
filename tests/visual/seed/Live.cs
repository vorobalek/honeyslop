#:project ../../../lib/Honeyslop.Data/Honeyslop.Data.csproj
#:property PublishAot=false

using System.Globalization;
using Honeyslop.Data;
using Honeyslop.Data.Entities;
using Honeyslop.Data.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

var connectionString = args.FirstOrDefault() ??
                       throw new ArgumentException("Pass the connection string of the seeded database.");
var seconds = args.Length > 1 ? int.Parse(args[1], CultureInfo.InvariantCulture) : 20;
var interval = TimeSpan.FromMilliseconds(args.Length > 2 ? int.Parse(args[2], CultureInfo.InvariantCulture) : 700);

await using var services = new ServiceCollection()
    .AddDbContext<ApplicationDbContext>(x => x.UseNpgsql(connectionString))
    .BuildServiceProvider();

string[] clients = ["176.9.38.39", "34.155.127.35", "45.95.147.236"];
string[] targets = ["/wp-login.php", "/.env", "/xmlrpc.php", "/.git/HEAD", "/admin/config.php"];
var origin = new DateTimeOffset(2026, 11, 10, 12, 0, 0, TimeSpan.Zero);
var until = DateTimeOffset.UtcNow.AddSeconds(seconds);
var index = 0;
while (DateTimeOffset.UtcNow < until)
{
    await using var scope = services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Requests.Add(new RequestRecord
    {
        ReceivedAt = origin.AddSeconds(index + 1),
        ClientIp = clients[index % clients.Length],
        Method = "GET",
        Host = "example.test",
        Target = targets[index % targets.Length],
        UserAgent = "python-requests/2.31.0",
        Outcome = RequestOutcome.Fallback,
        ResponseStatus = 404,
        DurationMs = 5,
        Instance = "honeyslop-trap-0"
    });
    await db.SaveChangesAsync();
    index++;
    await Task.Delay(interval);
}

Console.WriteLine($"Inserted {index} requests.");
