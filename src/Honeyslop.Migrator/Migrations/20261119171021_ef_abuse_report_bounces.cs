using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Honeyslop.Migrator.Migrations
{
    /// <inheritdoc />
    public partial class ef_abuse_report_bounces : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "abuse_report_bounce",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    report_id = table.Column<long>(type: "bigint", nullable: false),
                    recipient = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    action = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    diagnostic = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    mta = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    notification_id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    bounced_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_abuse_report_bounce", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "mailbox_cursor",
                schema: "public",
                columns: table => new
                {
                    folder = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    uid_validity = table.Column<long>(type: "bigint", nullable: false),
                    last_uid = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mailbox_cursor", x => x.folder);
                });

            migrationBuilder.CreateIndex(
                name: "ix_abuse_report_bounce_notification_id_recipient",
                schema: "public",
                table: "abuse_report_bounce",
                columns: new[] { "notification_id", "recipient" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_abuse_report_bounce_recipient_bounced_at",
                schema: "public",
                table: "abuse_report_bounce",
                columns: new[] { "recipient", "bounced_at" });

            migrationBuilder.CreateIndex(
                name: "ix_abuse_report_bounce_report_id",
                schema: "public",
                table: "abuse_report_bounce",
                column: "report_id");

            migrationBuilder.Sql(
                """
                create or replace function public.notify_report_bounce_change() returns trigger language plpgsql as $$
                begin
                    perform pg_notify('honeyslop_reports', '');
                    return null;
                end
                $$;

                create trigger abuse_report_bounce_changed
                    after insert or update or delete on public.abuse_report_bounce
                    for each statement execute function public.notify_report_bounce_change();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                drop trigger if exists abuse_report_bounce_changed on public.abuse_report_bounce;
                drop function if exists public.notify_report_bounce_change();
                """);

            migrationBuilder.DropTable(
                name: "abuse_report_bounce",
                schema: "public");

            migrationBuilder.DropTable(
                name: "mailbox_cursor",
                schema: "public");
        }
    }
}
