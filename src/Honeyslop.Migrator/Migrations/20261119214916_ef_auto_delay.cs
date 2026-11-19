using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Honeyslop.Migrator.Migrations
{
    /// <inheritdoc />
    public partial class ef_auto_delay : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "client_probe",
                schema: "public",
                columns: table => new
                {
                    client_ip = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    delay_milliseconds = table.Column<int>(type: "integer", nullable: false),
                    timeout_milliseconds = table.Column<int>(type: "integer", nullable: true),
                    probe_every = table.Column<int>(type: "integer", nullable: false),
                    answers_since_probe = table.Column<int>(type: "integer", nullable: false),
                    probing = table.Column<bool>(type: "boolean", nullable: false),
                    early_drop_milliseconds = table.Column<int>(type: "integer", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_client_probe", x => x.client_ip);
                });

            migrationBuilder.CreateTable(
                name: "delay_job",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    client_ip = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    answered = table.Column<bool>(type: "boolean", nullable: false),
                    duration_ms = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    claimed_by = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    claimed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_delay_job", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_delay_job_claimed_at_id",
                schema: "public",
                table: "delay_job",
                columns: new[] { "claimed_at", "id" });

            migrationBuilder.Sql(
                """
                create or replace function public.notify_client_probe_change() returns trigger language plpgsql as $$
                begin
                    perform pg_notify('honeyslop_clients', '');
                    return null;
                end
                $$;

                create trigger client_probe_changed
                    after insert or update or delete on public.client_probe
                    for each statement execute function public.notify_client_probe_change();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                drop trigger if exists client_probe_changed on public.client_probe;
                drop function if exists public.notify_client_probe_change();
                """);

            migrationBuilder.DropTable(
                name: "client_probe",
                schema: "public");

            migrationBuilder.DropTable(
                name: "delay_job",
                schema: "public");
        }
    }
}
