using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Honeyslop.Migrator.Migrations
{
    /// <inheritdoc />
    public partial class ef_response_delay_and_blocked_contacts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "response_delay_seconds",
                schema: "public",
                table: "entry",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "blocked_abuse_contact",
                schema: "public",
                columns: table => new
                {
                    contact = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    note = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    created_by = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_blocked_abuse_contact", x => x.contact);
                });

            migrationBuilder.CreateIndex(
                name: "ix_blocked_abuse_contact_created_at",
                schema: "public",
                table: "blocked_abuse_contact",
                column: "created_at");

            migrationBuilder.Sql(
                """
                create or replace function public.notify_blocked_contact_change() returns trigger language plpgsql as $$
                begin
                    perform pg_notify('honeyslop_reports', '');
                    return null;
                end
                $$;

                create trigger blocked_abuse_contact_changed
                    after insert or update or delete on public.blocked_abuse_contact
                    for each statement execute function public.notify_blocked_contact_change();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                drop trigger if exists blocked_abuse_contact_changed on public.blocked_abuse_contact;
                drop function if exists public.notify_blocked_contact_change();
                """);

            migrationBuilder.DropTable(
                name: "blocked_abuse_contact",
                schema: "public");

            migrationBuilder.DropColumn(
                name: "response_delay_seconds",
                schema: "public",
                table: "entry");
        }
    }
}
