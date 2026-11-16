using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Honeyslop.Migrator.Migrations
{
    /// <inheritdoc />
    public partial class ef_init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "public");

            migrationBuilder.CreateTable(
                name: "entry",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    key_hash = table.Column<byte[]>(type: "bytea", nullable: false),
                    method = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    host = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    target = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    attempts = table.Column<int>(type: "integer", nullable: false),
                    claimed_by = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    claimed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    error = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: true),
                    client_ip = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    user_agent = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    request_headers = table.Column<string>(type: "text", nullable: true),
                    request_body = table.Column<string>(type: "text", nullable: true),
                    response_status = table.Column<int>(type: "integer", nullable: true),
                    response_body = table.Column<string>(type: "text", nullable: true),
                    model = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    prompt_tokens = table.Column<int>(type: "integer", nullable: true),
                    completion_tokens = table.Column<int>(type: "integer", nullable: true),
                    generation_ms = table.Column<int>(type: "integer", nullable: true),
                    hits = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    generated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_hit_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    requested_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    response_headers = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_entry", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "request",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    received_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    client_ip = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    method = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    host = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    target = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    user_agent = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    headers = table.Column<string>(type: "text", nullable: true),
                    body = table.Column<string>(type: "text", nullable: true),
                    outcome = table.Column<int>(type: "integer", nullable: false),
                    response_status = table.Column<int>(type: "integer", nullable: false),
                    entry_id = table.Column<long>(type: "bigint", nullable: true),
                    duration_ms = table.Column<int>(type: "integer", nullable: false),
                    instance = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_request", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "setting",
                schema: "public",
                columns: table => new
                {
                    key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    value = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_setting", x => x.key);
                });

            migrationBuilder.CreateIndex(
                name: "ix_entry_created_at",
                schema: "public",
                table: "entry",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_entry_host",
                schema: "public",
                table: "entry",
                column: "host");

            migrationBuilder.CreateIndex(
                name: "ix_entry_key_hash",
                schema: "public",
                table: "entry",
                column: "key_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_entry_last_hit_at",
                schema: "public",
                table: "entry",
                column: "last_hit_at");

            migrationBuilder.CreateIndex(
                name: "ix_entry_status",
                schema: "public",
                table: "entry",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_request_client_ip_received_at",
                schema: "public",
                table: "request",
                columns: new[] { "client_ip", "received_at" });

            migrationBuilder.CreateIndex(
                name: "ix_request_entry_id",
                schema: "public",
                table: "request",
                column: "entry_id");

            migrationBuilder.CreateIndex(
                name: "ix_request_received_at",
                schema: "public",
                table: "request",
                column: "received_at");

            migrationBuilder.Sql(
                """
                create or replace function public.notify_entry_change() returns trigger language plpgsql as $$
                begin
                    if tg_op = 'DELETE' then
                        perform pg_notify('honeyslop_entries', old.id::text);
                    else
                        perform pg_notify('honeyslop_entries', new.id::text);
                    end if;
                    return null;
                end
                $$;

                create trigger entry_changed
                    after insert or update or delete on public.entry
                    for each row execute function public.notify_entry_change();

                create or replace function public.notify_request_change() returns trigger language plpgsql as $$
                begin
                    perform pg_notify('honeyslop_requests', '');
                    return null;
                end
                $$;

                create trigger request_changed
                    after insert or update on public.request
                    for each statement execute function public.notify_request_change();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                drop trigger if exists request_changed on public.request;
                drop function if exists public.notify_request_change();
                drop trigger if exists entry_changed on public.entry;
                drop function if exists public.notify_entry_change();
                """);

            migrationBuilder.DropTable(
                name: "entry",
                schema: "public");

            migrationBuilder.DropTable(
                name: "request",
                schema: "public");

            migrationBuilder.DropTable(
                name: "setting",
                schema: "public");
        }
    }
}
