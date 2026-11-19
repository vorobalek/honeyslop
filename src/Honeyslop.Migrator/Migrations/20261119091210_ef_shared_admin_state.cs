using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Honeyslop.Migrator.Migrations
{
    /// <inheritdoc />
    public partial class ef_shared_admin_state : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "data_protection_key",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    friendly_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    xml = table.Column<string>(type: "text", nullable: false),
                    activation_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    expiration_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_data_protection_key", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "resource_lock",
                schema: "public",
                columns: table => new
                {
                    name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    attempts = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_resource_lock", x => x.name);
                });

            migrationBuilder.InsertData(
                schema: "public",
                table: "resource_lock",
                columns: new[] { "name", "attempts" },
                values: new object[,]
                {
                    { "data_protection_keys", 0L },
                    { "settings", 0L }
                });

            migrationBuilder.Sql(
                """
                create or replace function public.notify_setting_change() returns trigger language plpgsql as $$
                begin
                    perform pg_notify('honeyslop_settings', '');
                    return null;
                end
                $$;

                create trigger setting_changed
                    after insert or update or delete on public.setting
                    for each statement execute function public.notify_setting_change();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                drop trigger if exists setting_changed on public.setting;
                drop function if exists public.notify_setting_change();
                """);

            migrationBuilder.DropTable(
                name: "data_protection_key",
                schema: "public");

            migrationBuilder.DropTable(
                name: "resource_lock",
                schema: "public");
        }
    }
}
