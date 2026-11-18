using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Honeyslop.Migrator.Migrations
{
    /// <inheritdoc />
    public partial class ef_abuse_reports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "abuse_report",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    client_ip = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    recipients = table.Column<string[]>(type: "text[]", nullable: false),
                    subject = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    body = table.Column<string>(type: "text", nullable: false),
                    requested_by = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    sender_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    attempts = table.Column<int>(type: "integer", nullable: false),
                    claimed_by = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    claimed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    retry_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    attached_requests = table.Column<int>(type: "integer", nullable: true),
                    last_request_id = table.Column<long>(type: "bigint", nullable: true),
                    message_id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    sent_from = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    error = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    sent_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_abuse_report", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "client_whois",
                schema: "public",
                columns: table => new
                {
                    client_ip = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    info = table.Column<string>(type: "jsonb", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_client_whois", x => x.client_ip);
                });

            migrationBuilder.CreateTable(
                name: "user_setting",
                schema: "public",
                columns: table => new
                {
                    user_id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    value = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_setting", x => new { x.user_id, x.key });
                });

            migrationBuilder.CreateIndex(
                name: "ix_abuse_report_client_ip",
                schema: "public",
                table: "abuse_report",
                column: "client_ip");

            migrationBuilder.CreateIndex(
                name: "ix_abuse_report_client_ip_pending",
                schema: "public",
                table: "abuse_report",
                column: "client_ip",
                unique: true,
                filter: "status = 0");

            migrationBuilder.CreateIndex(
                name: "ix_abuse_report_status",
                schema: "public",
                table: "abuse_report",
                column: "status");

            migrationBuilder.Sql(
                """
                create or replace function public.notify_abuse_report_change() returns trigger language plpgsql as $$
                begin
                    perform pg_notify('honeyslop_reports', '');
                    return null;
                end
                $$;

                create trigger abuse_report_changed
                    after insert or update on public.abuse_report
                    for each statement execute function public.notify_abuse_report_change();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                drop trigger if exists abuse_report_changed on public.abuse_report;
                drop function if exists public.notify_abuse_report_change();
                """);

            migrationBuilder.DropTable(
                name: "abuse_report",
                schema: "public");

            migrationBuilder.DropTable(
                name: "client_whois",
                schema: "public");

            migrationBuilder.DropTable(
                name: "user_setting",
                schema: "public");
        }
    }
}
