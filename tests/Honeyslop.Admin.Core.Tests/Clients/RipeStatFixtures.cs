namespace Honeyslop.Admin.Core.Tests.Clients;

internal static class RipeStatFixtures
{
    public const string PrefixOverview =
        """
        {"status": "ok", "data": {"announced": true, "is_less_specific": false, "resource": "8.8.8.0/24",
         "asns": [{"asn": 15169, "holder": "GOOGLE - Google LLC"}],
         "block": {"resource": "8.0.0.0/8", "desc": "Administered by ARIN", "name": "IANA IPv4 Address Space Registry"}}}
        """;

    public const string AbuseContactFinder =
        """
        {"status": "ok", "data": {"abuse_contacts": ["network-abuse@google.com", "not an address"], "authoritative_rir": "arin"}}
        """;

    public const string Whois =
        """
        {"status": "ok", "data": {"authorities": ["arin"], "records": [
          [{"key": "NetRange", "value": "8.8.8.0 - 8.8.8.255"}, {"key": "CIDR", "value": "8.8.8.0/24"},
           {"key": "NetName", "value": "GOGL"}, {"key": "Organization", "value": "Google LLC (GOGL)"}],
          [{"key": "OrgAbuseEmail", "value": "network-abuse@google.com"}],
          [{"key": "OrgName", "value": "Google LLC"}, {"key": "Country", "value": "US"}]
        ]}}
        """;

    public const string NotAnnounced =
        """
        {"status": "ok", "data": {"announced": false, "resource": "192.0.2.1", "asns": []}}
        """;

    public const string NoAbuseContacts =
        """
        {"status": "ok", "data": {"abuse_contacts": [], "authoritative_rir": null}}
        """;

    public const string RipeWhois =
        """
        {"status": "ok", "data": {"authorities": ["ripe"], "records": [
          [{"key": "inetnum", "value": "193.0.0.0/21"}, {"key": "netname", "value": "RIPE-NCC"},
           {"key": "descr", "value": "RIPE Network Coordination Centre"}, {"key": "country", "value": "NL"}]
        ]}}
        """;
}