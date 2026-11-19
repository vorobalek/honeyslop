namespace Honeyslop.Worker.Tests.Bounces;

internal static class Notifications
{
    public const string ReportMessageId = "report-22.20261119165600.851bb9@example.test";

    public const string ForwardedExim = """
        Return-Path: <SRS0=BdkO=JI=return.relay.example=bounce.ptdo5t943eslgr5@forwarder.example>
        Delivered-To: all@bounces.example.test
        Received: from s7.hoster.example ([198.51.100.15])
            by relay.example with esmtps (Exim 4.100.1)
            id 1xU5RK-B9zBpi9gsFV-lOaf
            for bounce.0cllxhzlrx3wb57@em1238633.example.test;
            Thu, 19 Nov 2026 16:56:52 +0000
        X-Failed-Recipients: abuse@hoster.example
        Auto-Submitted: auto-replied
        References: <report-22.20261119165600.851bb9@example.test>
        Content-Type: multipart/report; report-type=delivery-status; boundary=1795107410-eximdsn-1582028756
        MIME-Version: 1.0
        Subject: Mail delivery failed: returning message to sender
        Message-Id: <E1xU5RK-0000000An03-1jHL@s7.hoster.example>
        Date: Thu, 19 Nov 2026 17:56:50 +0100
        To: <all@bounces.example.test>
        X-Original-From: Mail Delivery System <Mailer-Daemon@s7.hoster.example>
        From: postmaster@relay.example

        --1795107410-eximdsn-1582028756
        Content-type: text/plain; charset=us-ascii

        This message was created automatically by mail delivery software.

        A message that you sent could not be delivered to one or more of its
        recipients. This is a permanent error. The following address(es) failed:

          abuse@hoster.example
            retry timeout exceeded

        --1795107410-eximdsn-1582028756
        Content-type: message/delivery-status

        Reporting-MTA: dns; s7.hoster.example

        Action: failed
        Final-Recipient: rfc822;abuse@hoster.example
        Status: 5.0.0

        --1795107410-eximdsn-1582028756
        Content-type: message/rfc822

        Return-path: <bounce.0cllxhzlrx3wb57@em1238633.example.test>
        From: "Example Sender" <security@example.test>
        To: abuse@hoster.example
        Subject: HTTP scanning from 203.0.113.7
        Date: Thu, 19 Nov 2026 16:56:00 +0000
        Message-ID: <report-22.20261119165600.851bb9@example.test>
        MIME-Version: 1.0
        Content-Type: multipart/mixed; boundary="=-original"

        --=-original
        Content-Type: text/plain; charset=utf-8

        Hello, we saw HTTP scanning from 203.0.113.7.
        --=-original
        Content-Type: application/zip; name=requests-203.0.113.7.zip
        Content-Disposition: attachment; filename=requests-203.0.113.7.zip
        Content-Transfer-Encoding: base64

        UEsFBgAAAAAAAAAAAAAAAAAAAAAAAA==
        --=-original--

        --1795107410-eximdsn-1582028756--
        """;

    public const string Postfix = """
        From: MAILER-DAEMON@mx.hoster.example (Mail Delivery System)
        To: all@bounces.example.test
        Subject: Undelivered Mail Returned to Sender
        Message-Id: <20261119170000.ABC123@mx.hoster.example>
        Date: Thu, 19 Nov 2026 17:00:00 +0000
        MIME-Version: 1.0
        Content-Type: multipart/report; report-type=delivery-status; boundary="postfix-boundary"

        --postfix-boundary
        Content-Description: Notification
        Content-Type: text/plain; charset=us-ascii

        I'm sorry to have to inform you that your message could not
        be delivered to one or more recipients.

        --postfix-boundary
        Content-Description: Delivery report
        Content-Type: message/delivery-status

        Reporting-MTA: dns; mx.hoster.example
        X-Postfix-Queue-ID: ABC123
        Arrival-Date: Thu, 19 Nov 2026 16:59:00 +0000

        Final-Recipient: rfc822; desk@internal.hoster.example
        Original-Recipient: rfc822;NOC@Hoster.example
        Action: failed
        Status: 5.1.1
        Remote-MTA: dns; inbound.internal.hoster.example
        Diagnostic-Code: smtp; 550 5.1.1 <desk@internal.hoster.example>:
            Recipient address rejected: User unknown

        Final-Recipient: rfc822; abuse@hoster.example
        Action: delayed
        Status: 4.4.1
        Diagnostic-Code: X-Postfix; connect to inbound.hoster.example: Connection timed out

        Final-Recipient: rfc822; ok@hoster.example
        Action: delivered
        Status: 2.0.0

        --postfix-boundary
        Content-Description: Undelivered Message Headers
        Content-Type: text/rfc822-headers

        From: security@example.test
        To: noc@hoster.example, abuse@hoster.example
        Subject: HTTP scanning from 203.0.113.9
        Message-ID: <report-23.20261119165900.aaaaaa@example.test>

        --postfix-boundary--
        """;

    public const string Success = """
        From: Mail Delivery System <Mailer-Daemon@relay.example>
        To: all@bounces.example.test
        Subject: Delivery Status Notification
        Message-Id: <E1xU514-AIkwcC8xYzF-J0gE@relay.example>
        Date: Thu, 19 Nov 2026 16:29:42 +0000
        MIME-Version: 1.0
        Content-Type: multipart/report; report-type=delivery-status; boundary=success

        --success
        Content-type: text/plain; charset=us-ascii

        <all@bounces.example.test> (relayed to non-DSN-aware mailer)

        --success
        Content-type: message/delivery-status

        Reporting-MTA: dns; relay.example

        Action: delivered
        Final-Recipient: rfc822;all@bounces.example.test
        Status: 2.0.0

        --success--
        """;

    public const string Plain = """
        From: someone@example.test
        To: all@bounces.example.test
        Subject: Hello
        Message-Id: <plain@example.test>
        Date: Thu, 19 Nov 2026 16:00:00 +0000
        MIME-Version: 1.0
        Content-Type: text/plain; charset=utf-8

        Not a bounce.
        """;
}