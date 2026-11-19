using System.Text;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Honeyslop.Worker.Mail;
using Honeyslop.Worker.Mail.Internals;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Honeyslop.Worker.Tests.Bounces;

[TestClass]
[TestCategory("Integration")]
public sealed class ImapMailboxTests
{
    private const int SmtpPort = 3025;
    private const int ImapPort = 3143;
    private const string Address = "all@bounces.example.test";

    private static IContainer? _greenMail;

    [ClassInitialize]
    public static async Task InitializeAsync(TestContext _)
    {
        _greenMail = new ContainerBuilder("greenmail/standalone:2.1.14")
            .WithEnvironment("GREENMAIL_OPTS", "-Dgreenmail.setup.test.all -Dgreenmail.hostname=0.0.0.0 -Dgreenmail.auth.disabled")
            .WithPortBinding(SmtpPort, true)
            .WithPortBinding(ImapPort, true)
            .WithPortBinding(8080, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(x => x.ForPort(8080).ForPath("/api/service/readiness")))
            .Build();
        await _greenMail.StartAsync();
    }

    [ClassCleanup]
    public static async Task CleanupAsync()
    {
        if (_greenMail is not null)
            await _greenMail.DisposeAsync();
    }

    [TestMethod]
    public async Task ReadAsync_ReturnsOnlyDeliveryReports_InBatches_WithoutMarkingAnythingRead()
    {
        await DeliverAsync(Notifications.ForwardedExim);
        await DeliverAsync(Notifications.Plain);
        await DeliverAsync(Notifications.Postfix);
        var mailbox = Mailbox();

        var first = await mailbox.ReadAsync("INBOX", null, 0, 2, CancellationToken.None);
        var second = await mailbox.ReadAsync("INBOX", first.UidValidity, first.LastUid, 2, CancellationToken.None);
        var idle = await mailbox.ReadAsync("INBOX", second.UidValidity, second.LastUid, 2, CancellationToken.None);
        var recreated = await mailbox.ReadAsync("INBOX", first.UidValidity + 1, second.LastUid, 10, CancellationToken.None);

        Assert.IsTrue(first.HasMore);
        Assert.AreEqual("E1xU5RK-0000000An03-1jHL@s7.hoster.example", first.Messages.Single().Message.MessageId);
        Assert.IsFalse(second.HasMore);
        Assert.AreEqual("20261119170000.ABC123@mx.hoster.example", second.Messages.Single().Message.MessageId);
        Assert.IsEmpty(idle.Messages);
        Assert.AreEqual(second.LastUid, idle.LastUid);
        Assert.HasCount(2, recreated.Messages);
        Assert.AreEqual(0, await CountSeenAsync());
    }

    private static ImapMailbox Mailbox()
    {
        return new ImapMailbox(Options.Create(new ImapOptions
        {
            Host = _greenMail!.Hostname,
            Port = _greenMail.GetMappedPublicPort(ImapPort),
            Username = Address,
            Password = "secret"
        }));
    }

    private static async Task DeliverAsync(string text)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));
        var message = await MimeMessage.LoadAsync(stream);
        using var client = new SmtpClient();
        await client.ConnectAsync(_greenMail!.Hostname, _greenMail.GetMappedPublicPort(SmtpPort), SecureSocketOptions.None);
        await client.SendAsync(message, MailboxAddress.Parse("mailer-daemon@hoster.example"), [MailboxAddress.Parse(Address)]);
        await client.DisconnectAsync(true);
    }

    private static async Task<int> CountSeenAsync()
    {
        using var client = new ImapClient();
        await client.ConnectAsync(_greenMail!.Hostname, _greenMail.GetMappedPublicPort(ImapPort), SecureSocketOptions.None);
        await client.AuthenticateAsync(Address, "secret");
        await client.Inbox.OpenAsync(FolderAccess.ReadOnly);
        var summaries = await client.Inbox.FetchAsync(0, -1, MessageSummaryItems.Flags);
        await client.DisconnectAsync(true);
        return summaries.Count(x => x.Flags?.HasFlag(MessageFlags.Seen) == true);
    }
}