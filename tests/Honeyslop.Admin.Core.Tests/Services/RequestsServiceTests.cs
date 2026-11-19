using Honeyslop.Admin.Core.Clients;
using Honeyslop.Admin.Core.Contracts;
using Honeyslop.Admin.Core.Enums;
using Honeyslop.Admin.Core.Services.Internals;
using Honeyslop.Data.Contracts;
using Honeyslop.Data.Enums;
using Honeyslop.Data.Repositories;
using Moq;

namespace Honeyslop.Admin.Core.Tests.Services;

[TestClass]
public sealed class RequestsServiceTests
{
    private static readonly DateTimeOffset Start = new(2026, 11, 18, 0, 0, 0, TimeSpan.Zero);

    [TestMethod]
    public async Task GetBreakdownAsync_ByCountry_AddsUpTheAddressesOfEachCountry()
    {
        var repository = new Mock<IRequestRepository>();
        repository
            .Setup(x => x.GetBreakdownAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<RequestOutcome?>(), It.IsAny<long?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<DateTimeOffset?>(),
                It.IsAny<DateTimeOffset?>(), RequestDimension.Client, int.MaxValue, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                Bucket("192.0.2.1", 5, Start, Start.AddHours(1)),
                Bucket("192.0.2.2", 3, Start.AddHours(-1), Start),
                Bucket("198.51.100.1", 4, Start, Start),
                Bucket("203.0.113.1", 1, Start, Start)
            ]);
        repository.Setup(x => x.CountAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<RequestOutcome?>(), It.IsAny<long?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<DateTimeOffset?>(),
            It.IsAny<DateTimeOffset?>(), It.IsAny<long?>(), It.IsAny<CancellationToken>())).ReturnsAsync(13);
        var countries = new Mock<ICountryLookup>();
        countries.Setup(x => x.Find(It.Is<string>(ip => ip.StartsWith("192.", StringComparison.Ordinal)))).Returns(new CountryDto("DE", "Germany"));
        countries.Setup(x => x.Find("198.51.100.1")).Returns(new CountryDto("NL", "The Netherlands"));
        var service = new RequestsService(repository.Object, countries.Object, Viewers.Guard(Viewers.Operator), Viewers.ServerTime(DateTimeOffset.UnixEpoch));

        var breakdown = await service.GetBreakdownAsync(new RequestsBreakdownQuery(new RequestFilter(), BreakdownDimension.Country, 2), CancellationToken.None);

        Assert.AreEqual(13, breakdown.Requests);
        CollectionAssert.AreEqual(
            new[]
            {
                new RequestGroupDto("DE", new CountryDto("DE", "Germany"), 8, 2, Start.AddHours(-1), Start.AddHours(1)), new RequestGroupDto("NL", new CountryDto("NL", "The Netherlands"), 4, 1, Start, Start)
            },
            breakdown.Groups);
    }

    private static RequestBucket Bucket(string key, int requests, DateTimeOffset first, DateTimeOffset last)
    {
        return new RequestBucket
        {
            Key = key,
            Requests = requests,
            Clients = 1,
            FirstSeenAt = first,
            LastSeenAt = last
        };
    }
}