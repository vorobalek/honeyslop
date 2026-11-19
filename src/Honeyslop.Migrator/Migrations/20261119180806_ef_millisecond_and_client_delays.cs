using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Honeyslop.Migrator.Migrations
{
    /// <inheritdoc />
    public partial class ef_millisecond_and_client_delays : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "response_delay_milliseconds",
                schema: "public",
                table: "entry",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql(
                """
                update public.entry
                set response_delay_milliseconds = response_delay_seconds * 1000
                where response_delay_seconds is not null;

                insert into public.setting (key, value, updated_at)
                select 'response_delay_milliseconds', (value::bigint * 1000)::text, now()
                from public.setting
                where key = 'response_delay_seconds' and value ~ '^[0-9]+$'
                on conflict (key) do nothing;
                """);

            migrationBuilder.CreateTable(
                name: "client_delay",
                schema: "public",
                columns: table => new
                {
                    client_ip = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    response_delay_milliseconds = table.Column<int>(type: "integer", nullable: false),
                    changed_by = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_client_delay", x => x.client_ip);
                });

            migrationBuilder.Sql(
                """
                create or replace function public.notify_client_delay_change() returns trigger language plpgsql as $$
                begin
                    perform pg_notify('honeyslop_clients', '');
                    return null;
                end
                $$;

                create trigger client_delay_changed
                    after insert or update or delete on public.client_delay
                    for each statement execute function public.notify_client_delay_change();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                drop trigger if exists client_delay_changed on public.client_delay;
                drop function if exists public.notify_client_delay_change();
                """);

            migrationBuilder.DropTable(
                name: "client_delay",
                schema: "public");

            migrationBuilder.Sql("delete from public.setting where key = 'response_delay_milliseconds';");

            migrationBuilder.DropColumn(
                name: "response_delay_milliseconds",
                schema: "public",
                table: "entry");
        }
    }
}
