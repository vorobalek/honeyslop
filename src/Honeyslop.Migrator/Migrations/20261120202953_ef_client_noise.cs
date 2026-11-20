using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Honeyslop.Migrator.Migrations
{
    /// <inheritdoc />
    public partial class ef_client_noise : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "client_noise",
                schema: "public",
                columns: table => new
                {
                    client_ip = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    tier = table.Column<int>(type: "integer", nullable: false),
                    changed_by = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_client_noise", x => x.client_ip);
                });

            migrationBuilder.Sql(
                """
                create trigger client_noise_changed
                    after insert or update or delete on public.client_noise
                    for each statement execute function public.notify_client_delay_change();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("drop trigger if exists client_noise_changed on public.client_noise;");

            migrationBuilder.DropTable(
                name: "client_noise",
                schema: "public");
        }
    }
}
